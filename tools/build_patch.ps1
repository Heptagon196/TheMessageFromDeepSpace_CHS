param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$gameRoot = Split-Path -Parent $projectRoot

# 某些受控终端会清空这些标准环境变量。若 SystemDrive 缺失，Windows 的
# %SystemDrive%\ProgramData 已知文件夹可能被误当成相对路径并写入当前目录。
$driveRoot = [IO.Path]::GetPathRoot($gameRoot).TrimEnd('\')
if (-not [IO.Path]::IsPathRooted($gameRoot) -or $driveRoot -notmatch '^[A-Za-z]:$') {
    throw "无法从游戏目录确定系统盘：$gameRoot"
}
$env:SystemDrive = $driveRoot
if (-not $env:ProgramData -or $env:ProgramData.Contains('%')) {
    $env:ProgramData = Join-Path $env:SystemDrive "ProgramData"
}
if (-not $env:ALLUSERSPROFILE -or $env:ALLUSERSPROFILE.Contains('%')) {
    $env:ALLUSERSPROFILE = $env:ProgramData
}
if (-not $env:ProgramFiles -or $env:ProgramFiles.Contains('%')) {
    $env:ProgramFiles = Join-Path $env:SystemDrive "Program Files"
}
if (-not ${env:ProgramFiles(x86)} -or ${env:ProgramFiles(x86)}.Contains('%')) {
    ${env:ProgramFiles(x86)} = Join-Path $env:SystemDrive "Program Files (x86)"
}

$toolEnvironment = Join-Path $projectRoot "build\tool-environment"
if (-not $env:USERPROFILE -or $env:USERPROFILE.Contains('%') -or
    -not [IO.Path]::IsPathRooted($env:USERPROFILE)) {
    $env:USERPROFILE = Join-Path $toolEnvironment "UserProfile"
}
if (-not $env:LOCALAPPDATA -or $env:LOCALAPPDATA.Contains('%') -or
    -not [IO.Path]::IsPathRooted($env:LOCALAPPDATA)) {
    $env:LOCALAPPDATA = Join-Path $toolEnvironment "LocalAppData"
}
if (-not $env:APPDATA -or $env:APPDATA.Contains('%') -or
    -not [IO.Path]::IsPathRooted($env:APPDATA)) {
    $env:APPDATA = Join-Path $toolEnvironment "AppData"
}
New-Item -ItemType Directory -Force -Path $env:USERPROFILE, $env:LOCALAPPDATA, $env:APPDATA | Out-Null

$packageRoot = Join-Path $projectRoot "build\package"
$contentRoot = Join-Path $packageRoot "DeepSpaceChinese"
$bepInExRoot = Join-Path $projectRoot "vendor\BepInEx\extracted"
$fontArchive = Join-Path $projectRoot "vendor\fusion-pixel-font-12px-proportional-otf-v2026.07.20.zip"
$fontExtract = Join-Path $projectRoot "build\font-extract"

& (Join-Path $PSScriptRoot "ensure_dependencies.ps1") -VendorRoot (Join-Path $projectRoot "vendor")

$requiredRoots = @($projectRoot, $gameRoot, $bepInExRoot)
foreach ($path in $requiredRoots) {
    if (-not (Test-Path -LiteralPath $path -PathType Container)) {
        throw "缺少构建目录：$path"
    }
}
if (-not (Test-Path -LiteralPath $fontArchive -PathType Leaf)) {
    throw "缺少字体包：$fontArchive"
}

New-Item -ItemType Directory -Force -Path $packageRoot, $contentRoot,
    (Join-Path $contentRoot "Fonts"), (Join-Path $contentRoot "Licenses"),
    (Join-Path $contentRoot "Translations"), (Join-Path $packageRoot "BepInEx\plugins"),
    $fontExtract | Out-Null

python (Join-Path $PSScriptRoot "build_runtime.py") --strict
if ($LASTEXITCODE -ne 0) { throw "运行时翻译文件校验失败。" }

& (Join-Path $PSScriptRoot "audit_unicode_inputs.ps1")
if ($LASTEXITCODE -ne 0) { throw "中文输入框覆盖审计失败。" }

dotnet build (Join-Path $projectRoot "src\DeepSpaceChinese\DeepSpaceChinese.csproj") `
    -c $Configuration --configfile (Join-Path $projectRoot "NuGet.Config")
if ($LASTEXITCODE -ne 0) { throw "插件编译失败。" }

dotnet build (Join-Path $projectRoot "src\DeepSpaceChinese.ConfigEditor\DeepSpaceChinese.ConfigEditor.csproj") `
    -c $Configuration --configfile (Join-Path $projectRoot "NuGet.Config")
if ($LASTEXITCODE -ne 0) { throw "配置编辑器编译失败。" }

Copy-Item -LiteralPath (Join-Path $bepInExRoot "BepInEx") -Destination $packageRoot -Recurse -Force
Copy-Item -LiteralPath (Join-Path $bepInExRoot "winhttp.dll") -Destination $packageRoot -Force
Copy-Item -LiteralPath (Join-Path $bepInExRoot "doorstop_config.ini") -Destination $packageRoot -Force
Copy-Item -LiteralPath (Join-Path $projectRoot "patch\DeepSpaceChinese.ini") -Destination $packageRoot -Force
Copy-Item -LiteralPath (Join-Path $projectRoot "patch\README_简体中文.txt") -Destination $contentRoot -Force
Copy-Item -LiteralPath (Join-Path $projectRoot "src\DeepSpaceChinese\bin\$Configuration\net472\DeepSpaceChinese.dll") `
    -Destination (Join-Path $packageRoot "BepInEx\plugins") -Force
Copy-Item -LiteralPath (Join-Path $projectRoot "src\DeepSpaceChinese.ConfigEditor\bin\$Configuration\net472\DeepSpaceChinese.ConfigEditor.exe") `
    -Destination $packageRoot -Force

$legacyBundledFont = Join-Path $contentRoot "Fonts\fusion-pixel-12px-monospaced-zh_hans.otf"
if (Test-Path -LiteralPath $legacyBundledFont -PathType Leaf) {
    Remove-Item -LiteralPath $legacyBundledFont -Force
}
tar -xf $fontArchive -C $fontExtract "fusion-pixel-12px-proportional-zh_hans.otf" "OFL.txt"
Copy-Item -LiteralPath (Join-Path $fontExtract "fusion-pixel-12px-proportional-zh_hans.otf") `
    -Destination (Join-Path $contentRoot "Fonts\fusion-pixel-12px-proportional-zh_hans.otf") -Force
Copy-Item -LiteralPath (Join-Path $fontExtract "OFL.txt") `
    -Destination (Join-Path $contentRoot "Licenses\FusionPixelFont-OFL-1.1.txt") -Force
Copy-Item -LiteralPath (Join-Path $projectRoot "vendor\BepInEx\LICENSE") `
    -Destination (Join-Path $contentRoot "Licenses\BepInEx-LGPL-2.1.txt") -Force

Write-Host "补丁目录已生成：$packageRoot"
