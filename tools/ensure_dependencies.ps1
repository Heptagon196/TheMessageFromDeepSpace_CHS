param(
    [string]$VendorRoot = ""
)

$ErrorActionPreference = "Stop"
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$projectRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($VendorRoot)) {
    $VendorRoot = Join-Path $projectRoot "vendor"
}
$VendorRoot = [IO.Path]::GetFullPath($VendorRoot)
if ($VendorRoot -eq [IO.Path]::GetPathRoot($VendorRoot)) {
    throw "依赖目录不能是磁盘根目录：$VendorRoot"
}

function Get-FileSha256([string]$Path) {
    return (Get-FileHash -Algorithm SHA256 -LiteralPath $Path).Hash.ToUpperInvariant()
}

function Ensure-VerifiedDownload(
    [string]$Name,
    [string]$Uri,
    [string]$Destination,
    [string]$ExpectedSha256
) {
    $ExpectedSha256 = $ExpectedSha256.ToUpperInvariant()
    if (Test-Path -LiteralPath $Destination -PathType Leaf) {
        $actual = Get-FileSha256 $Destination
        if ($actual -ne $ExpectedSha256) {
            throw "$Name 已存在但 SHA-256 不匹配。请删除后重试：$Destination`n预期：$ExpectedSha256`n实际：$actual"
        }
        Write-Host "$Name 已存在且校验通过。"
        return
    }

    $parent = Split-Path -Parent $Destination
    New-Item -ItemType Directory -Force -Path $parent | Out-Null
    $temporary = "$Destination.download"
    if (Test-Path -LiteralPath $temporary) {
        Remove-Item -LiteralPath $temporary -Force
    }

    try {
        Write-Host "正在下载 $Name ..."
        try {
            Invoke-WebRequest -UseBasicParsing -Headers @{ "User-Agent" = "TheMessageFromDeepSpace-CHS-build" } `
                -Uri $Uri -OutFile $temporary
        }
        catch {
            Write-Warning "PowerShell 下载失败，改用 Windows curl：$($_.Exception.Message)"
            if (Test-Path -LiteralPath $temporary) {
                Remove-Item -LiteralPath $temporary -Force
            }
            $curl = Get-Command "curl.exe" -ErrorAction SilentlyContinue
            if ($null -eq $curl) {
                throw "下载失败，且系统中找不到 curl.exe。"
            }
            & $curl.Source --fail --location --retry 3 --retry-delay 1 `
                --user-agent "TheMessageFromDeepSpace-CHS-build" --output $temporary $Uri
            if ($LASTEXITCODE -ne 0) {
                throw "curl 下载失败，退出码：$LASTEXITCODE"
            }
        }
        $actual = Get-FileSha256 $temporary
        if ($actual -ne $ExpectedSha256) {
            throw "$Name 下载后的 SHA-256 不匹配。`n预期：$ExpectedSha256`n实际：$actual"
        }
        Move-Item -LiteralPath $temporary -Destination $Destination
    }
    finally {
        if (Test-Path -LiteralPath $temporary) {
            Remove-Item -LiteralPath $temporary -Force
        }
    }
}

$bepInExDirectory = Join-Path $VendorRoot "BepInEx"
$bepInExArchive = Join-Path $bepInExDirectory "BepInEx_win_x64_5.4.23.5.zip"
$bepInExLicense = Join-Path $bepInExDirectory "LICENSE"
$bepInExExtracted = Join-Path $bepInExDirectory "extracted"
$bepInExMarker = Join-Path $bepInExDirectory ".extracted-sha256"
$fontArchive = Join-Path $VendorRoot "fusion-pixel-font-12px-proportional-otf-v2026.07.20.zip"

Ensure-VerifiedDownload `
    "BepInEx 5.4.23.5 Windows x64" `
    "https://github.com/BepInEx/BepInEx/releases/download/v5.4.23.5/BepInEx_win_x64_5.4.23.5.zip" `
    $bepInExArchive `
    "82F9878551030F54657792C0740D9D51A09500EEAE1FBA21106B0C441E6732C4"

Ensure-VerifiedDownload `
    "BepInEx LGPL 许可证" `
    "https://raw.githubusercontent.com/BepInEx/BepInEx/v5.4.23.5/LICENSE" `
    $bepInExLicense `
    "E6E534EF6F4347B6449407EE046A3D09CB0174C6F688C996AD0BED94B74B3933"

Ensure-VerifiedDownload `
    "Fusion Pixel Font 12px 非等宽 OTF" `
    "https://github.com/TakWolf/fusion-pixel-font/releases/download/2026.07.20/fusion-pixel-font-12px-proportional-otf-v2026.07.20.zip" `
    $fontArchive `
    "C4D9953664CB6EDC8C550DA5ECD1C0CF74F95450B2E220790212F08937924D63"

$expectedBepInExSha256 = "82F9878551030F54657792C0740D9D51A09500EEAE1FBA21106B0C441E6732C4"
$requiredExtractedFiles = @(
    (Join-Path $bepInExExtracted "BepInEx\core\BepInEx.dll"),
    (Join-Path $bepInExExtracted "BepInEx\core\0Harmony.dll"),
    (Join-Path $bepInExExtracted "doorstop_config.ini"),
    (Join-Path $bepInExExtracted "winhttp.dll")
)
$markerMatches = (Test-Path -LiteralPath $bepInExMarker -PathType Leaf) -and
    ((Get-Content -LiteralPath $bepInExMarker -Raw).Trim().ToUpperInvariant() -eq $expectedBepInExSha256)
$missingExtractedFile = @($requiredExtractedFiles | Where-Object {
    -not (Test-Path -LiteralPath $_ -PathType Leaf)
}).Count -gt 0

if (-not $markerMatches -or $missingExtractedFile) {
    $extractFullPath = [IO.Path]::GetFullPath($bepInExExtracted)
    $bepInExFullPath = [IO.Path]::GetFullPath($bepInExDirectory).TrimEnd('\')
    if (-not $extractFullPath.StartsWith($bepInExFullPath + '\', [StringComparison]::OrdinalIgnoreCase)) {
        throw "BepInEx 解压目录越界：$extractFullPath"
    }
    if (Test-Path -LiteralPath $extractFullPath) {
        $extractItem = Get-Item -LiteralPath $extractFullPath -Force
        if (-not $extractItem.PSIsContainer -or
            (($extractItem.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0)) {
            throw "BepInEx 解压目标类型异常：$extractFullPath"
        }
        Remove-Item -LiteralPath $extractFullPath -Recurse -Force
    }
    New-Item -ItemType Directory -Force -Path $extractFullPath | Out-Null
    Expand-Archive -LiteralPath $bepInExArchive -DestinationPath $extractFullPath -Force
    foreach ($requiredFile in $requiredExtractedFiles) {
        if (-not (Test-Path -LiteralPath $requiredFile -PathType Leaf)) {
            throw "BepInEx 解压结果缺少文件：$requiredFile"
        }
    }
    Set-Content -LiteralPath $bepInExMarker -Value $expectedBepInExSha256 -Encoding ASCII
    Write-Host "BepInEx 已解压并校验。"
}
else {
    Write-Host "BepInEx 解压目录已就绪。"
}

Write-Host "第三方依赖已全部就绪：$VendorRoot"
