param(
    [string]$GameRoot = "",
    [string]$OutputDirectory = "",
    [ValidateRange(30, 900)]
    [int]$TimeoutSeconds = 300,
    [string]$PageName = "Blackhole",
    [string[]]$BatchPages = @(),
    [switch]$Batch,
    [switch]$FullScroll,
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"
$projectRoot = (Resolve-Path -LiteralPath (Split-Path -Parent $PSScriptRoot)).Path
. (Join-Path $PSScriptRoot "initialize_tool_environment.ps1") -ProjectRoot $projectRoot
. (Join-Path $PSScriptRoot "resolve_game_root.ps1")
$gameRootPath = Resolve-GameRootPath -GameRoot $GameRoot -ProjectRoot $projectRoot
$gameExe = Join-Path $gameRootPath "The Message From Deep Space.exe"
if (-not (Test-Path -LiteralPath $gameExe -PathType Leaf)) {
    throw "找不到游戏程序：$gameExe"
}

$running = Get-Process | Where-Object {
    try { $_.Path -eq $gameExe } catch { $false }
}
if ($running) {
    throw "游戏正在运行。请先正常退出游戏，再执行自动参考页截图。"
}

if (-not $SkipBuild) {
    & (Join-Path $PSScriptRoot "build_patch.ps1") -Configuration Release -GameRoot $gameRootPath
}
$packageRoot = Join-Path $projectRoot "build\package"
$pluginSource = Join-Path $packageRoot "BepInEx\plugins\DeepSpaceChinese.dll"
if (-not (Test-Path -LiteralPath $pluginSource -PathType Leaf)) {
    throw "缺少已构建插件：$pluginSource"
}

# 自动回归只覆盖代码和运行时内容，保留用户当前的根目录 INI。
$pluginDestination = Join-Path $gameRootPath "BepInEx\plugins\DeepSpaceChinese.dll"
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $pluginDestination) | Out-Null
Copy-Item -LiteralPath $pluginSource -Destination $pluginDestination -Force
Copy-Item -LiteralPath (Join-Path $packageRoot "DeepSpaceChinese") `
    -Destination $gameRootPath -Recurse -Force

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $stamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $OutputDirectory = Join-Path $projectRoot "work\reference-captures\$stamp"
}
elseif (-not [System.IO.Path]::IsPathRooted($OutputDirectory)) {
    $OutputDirectory = Join-Path $projectRoot $OutputDirectory
}
$outputPath = [System.IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $outputPath) {
    if ((Get-ChildItem -LiteralPath $outputPath -Force | Measure-Object).Count -gt 0) {
        throw "输出目录必须不存在或为空：$outputPath"
    }
}
else {
    New-Item -ItemType Directory -Path $outputPath -Force | Out-Null
}

$encodedOutput = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($outputPath))
$encodedPageName = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($PageName))
$arguments = @(
    "--dsc-reference-capture-base64=$encodedOutput",
    "--dsc-reference-page-base64=$encodedPageName",
    "-screen-fullscreen", "0",
    "-screen-width", "1600",
    "-screen-height", "900"
)
if ($Batch) { $arguments += "--dsc-reference-batch" }
if ($Batch -and $BatchPages.Count -gt 0) {
    $batchFilter = $BatchPages -join "|"
    $encodedBatchFilter = [Convert]::ToBase64String(
        [Text.Encoding]::UTF8.GetBytes($batchFilter))
    $arguments += "--dsc-reference-filter-base64=$encodedBatchFilter"
}
if ($FullScroll) { $arguments += "--dsc-reference-full-scroll" }
Write-Host $(if ($Batch) {
    if ($BatchPages.Count -gt 0) {
        "正在启动真实游戏参考页短名单回归（$($BatchPages -join '、')）：$outputPath"
    } else {
        "正在启动真实游戏参考页批量回归：$outputPath"
    }
} else {
    if ($FullScroll) {
        "正在启动单页完整滚动测试（$PageName）：$outputPath"
    } else {
        "正在启动单页烟雾测试（$PageName，仅顶部/底部）：$outputPath"
    }
})
$startInfo = [System.Diagnostics.ProcessStartInfo]::new()
$startInfo.FileName = $gameExe
$startInfo.Arguments = $arguments -join ' '
$startInfo.WorkingDirectory = $gameRootPath
$startInfo.UseShellExecute = $false
$startInfo.CreateNoWindow = $false
$startInfo.WindowStyle = [System.Diagnostics.ProcessWindowStyle]::Normal
# Start-Process may reconstruct the child environment from registry values in the
# restricted desktop host, turning ProgramData back into the literal
# "%SystemDrive%\ProgramData". Pass every repaired folder explicitly to Unity.
foreach ($name in @(
    'SystemDrive', 'ProgramData', 'ALLUSERSPROFILE', 'ProgramFiles',
    'ProgramFiles(x86)', 'ProgramW6432', 'CommonProgramFiles',
    'CommonProgramFiles(x86)', 'CommonProgramW6432', 'USERPROFILE',
    'LOCALAPPDATA', 'APPDATA', 'DOTNET_CLI_HOME'
)) {
    $value = [Environment]::GetEnvironmentVariable($name, 'Process')
    if (-not [string]::IsNullOrWhiteSpace($value)) {
        $startInfo.EnvironmentVariables[$name] = $value
    }
}
$process = [System.Diagnostics.Process]::Start($startInfo)
$effectiveTimeoutSeconds = if ($Batch -and -not $PSBoundParameters.ContainsKey('TimeoutSeconds')) {
    900
} else {
    $TimeoutSeconds
}
$deadline = (Get-Date).AddSeconds($effectiveTimeoutSeconds)
$statusPath = Join-Path $outputPath "status.json"
$complete = $false
while ((Get-Date) -lt $deadline) {
    Start-Sleep -Milliseconds 500
    if (Test-Path -LiteralPath $statusPath -PathType Leaf) {
        try {
            $status = Get-Content -LiteralPath $statusPath -Raw | ConvertFrom-Json
            if ($status.state -eq "complete") { $complete = $true; break }
            if ($status.state -eq "failed") { throw "游戏内截图失败：$($status.error)" }
        }
        catch [System.ArgumentException] {
            # 游戏可能正以原子性不足的方式写入很小的状态文件，下一轮重读。
        }
    }
    # Steam 可能让最初的启动代理立即退出；只以游戏写出的状态文件为准。
}
if (-not $complete) {
    $targets = @(Get-Process -Name "The Message From Deep Space" -ErrorAction SilentlyContinue | Where-Object {
        try { $_.Path -eq $gameExe } catch { $false }
    })
    foreach ($target in $targets) {
        Stop-Process -Id $target.Id -Force -ErrorAction Stop
    }
    @{ state = "failed"; error = "外部测试脚本等待超时（$effectiveTimeoutSeconds 秒），已关闭测试游戏。" } |
        ConvertTo-Json | Set-Content -LiteralPath $statusPath -Encoding UTF8
    throw "参考页截图等待超时（$effectiveTimeoutSeconds 秒）；已关闭测试游戏。"
}

function Get-ReferenceContentPixelCount {
    param([Parameter(Mandatory = $true)][string]$ImagePath)

    Add-Type -AssemblyName System.Drawing
    $bitmap = [System.Drawing.Bitmap]::new($ImagePath)
    try {
        # Ignore the blue title bar and the right-hand scrollbar. Sampling every third
        # pixel is enough to distinguish actual text/diagrams from an empty monitor while
        # keeping the smoke check cheap.
        $count = 0
        $right = [Math]::Max(1, $bitmap.Width - 64)
        for ($y = 82; $y -lt $bitmap.Height; $y += 3) {
            for ($x = 8; $x -lt $right; $x += 3) {
                $pixel = $bitmap.GetPixel($x, $y)
                if ([Math]::Max($pixel.R, [Math]::Max($pixel.G, $pixel.B)) -gt 40) {
                    $count++
                }
            }
        }
        return $count
    }
    finally {
        $bitmap.Dispose()
    }
}

$manifestPath = Join-Path $outputPath "manifest.json"
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
if (-not $Batch -and -not $FullScroll) {
    if ($manifest.pages.Count -ne 1 -or
        $manifest.pages[0].captures.Count -notin @(2, 4)) {
        throw "烟雾测试必须只产生 1 页；无滚动页中英文各 1 张，有滚动页各顶部/底部 2 张。"
    }
    foreach ($language in @("zh", "en")) {
        $frames = @($manifest.pages[0].captures | Where-Object { $_.language -eq $language })
        if ($frames.Count -notin @(1, 2)) {
            throw "烟雾测试失败：语言 $language 必须有 1 张无滚动截图或顶部/底部 2 张截图。"
        }
        $first = $frames[0]
        $firstPng = Join-Path $outputPath $first.screenshot
        $firstContentPixels = Get-ReferenceContentPixelCount -ImagePath $firstPng
        if ($firstContentPixels -lt 20) {
            throw "烟雾测试失败：$language 的正文区域没有有效像素。"
        }
        if ($frames.Count -eq 2) {
            $last = $frames[1]
            $scrollChanged = [Math]::Abs([double]$first.actual_scroll - [double]$last.actual_scroll) -gt 0.02
            $contentMoved = [Math]::Abs([double]$first.moving_part_y - [double]$last.moving_part_y) -gt 0.001
            $lastPng = Join-Path $outputPath $last.screenshot
            $imagesDiffer = (Get-FileHash -LiteralPath $firstPng -Algorithm SHA256).Hash -ne `
                            (Get-FileHash -LiteralPath $lastPng -Algorithm SHA256).Hash
            $lastContentPixels = Get-ReferenceContentPixelCount -ImagePath $lastPng
            if (-not $scrollChanged -or -not $contentMoved -or -not $imagesDiffer -or
                $lastContentPixels -lt 20) {
                throw "烟雾测试失败：$language 的实际滚动值、内容根坐标、截图哈希和顶部/底部正文像素必须全部有效。"
            }
        }
    }
    Write-Host "烟雾测试通过：中文/英文正文均有效；可滚动页面已验证顶部和底部。"
}
$reportPath = Join-Path $outputPath "report.json"
& (Join-Path $PSScriptRoot "run_python.ps1") "tools\analyze_reference_capture.py" `
    $manifestPath "--output" $reportPath
Write-Host "参考页回归完成：$outputPath"
Assert-NoLiteralSystemDriveArtifact
