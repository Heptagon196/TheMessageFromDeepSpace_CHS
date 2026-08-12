param(
    [string]$Configuration = "Release",
    [string]$GameRoot = ""
)

$ErrorActionPreference = "Stop"
$projectRoot = (Resolve-Path -LiteralPath (Split-Path -Parent $PSScriptRoot)).Path
. (Join-Path $PSScriptRoot "resolve_game_root.ps1")
$gameRoot = Resolve-GameRootPath -GameRoot $GameRoot -ProjectRoot $projectRoot
$gameManagedDir = Resolve-GameManagedDirectory -GameRoot $gameRoot

# 某些受控终端会清空这些标准环境变量。若 SystemDrive 缺失，Windows 的
# %SystemDrive%\ProgramData 已知文件夹可能被误当成相对路径并写入当前目录。
$driveRoot = [IO.Path]::GetPathRoot([Environment]::SystemDirectory).TrimEnd('\')
if ($driveRoot -notmatch '^[A-Za-z]:$') {
    $driveRoot = [IO.Path]::GetPathRoot($projectRoot).TrimEnd('\')
}
if ($driveRoot -notmatch '^[A-Za-z]:$') {
    throw "无法确定 Windows 系统盘。"
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

function Assert-NoLiteralSystemDriveArtifact {
    $invalidPath = Join-Path $projectRoot "%SystemDrive%"
    if (Test-Path -LiteralPath $invalidPath) {
        throw "检测到未展开的 %SystemDrive% 目录：$invalidPath。请删除该历史构建残留后重试。"
    }
}

Assert-NoLiteralSystemDriveArtifact

python (Join-Path $PSScriptRoot "build_dictionary_trigger_aliases.py")
if ($LASTEXITCODE -ne 0) { throw "词典中文触发规则生成或冲突校验失败。" }

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

$requiredGameAssemblies = @(
    "Assembly-CSharp.dll",
    "UnityEngine.dll",
    "UnityEngine.CoreModule.dll",
    "UnityEngine.InputLegacyModule.dll",
    "UnityEngine.IMGUIModule.dll",
    "UnityEngine.ParticleSystemModule.dll",
    "UnityEngine.UI.dll",
    "UnityEngine.UIModule.dll",
    "Unity.TextMeshPro.dll",
    "UnityEngine.TextCoreFontEngineModule.dll",
    "Newtonsoft.Json.dll"
)
foreach ($assemblyName in $requiredGameAssemblies) {
    $assemblyPath = Join-Path $gameManagedDir $assemblyName
    if (-not (Test-Path -LiteralPath $assemblyPath -PathType Leaf)) {
        throw "缺少游戏程序集：$assemblyPath"
    }
}

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
    (Join-Path $contentRoot "Translations"), (Join-Path $contentRoot "Fix"),
    (Join-Path $packageRoot "BepInEx\plugins"),
    $fontExtract | Out-Null

python (Join-Path $PSScriptRoot "build_runtime.py") --strict
if ($LASTEXITCODE -ne 0) { throw "运行时翻译文件校验失败。" }

& (Join-Path $PSScriptRoot "audit_unicode_inputs.ps1") `
    -GameRoot $gameRoot `
    -CecilPath (Join-Path $bepInExRoot "BepInEx\core\Mono.Cecil.dll")
if ($LASTEXITCODE -ne 0) { throw "中文输入框覆盖审计失败。" }

dotnet build (Join-Path $projectRoot "src\DeepSpaceChinese\DeepSpaceChinese.csproj") `
    -c $Configuration --configfile (Join-Path $projectRoot "NuGet.Config") `
    "-p:GameManagedDir=$gameManagedDir"
if ($LASTEXITCODE -ne 0) { throw "插件编译失败。" }

dotnet build (Join-Path $projectRoot "src\DeepSpaceChinese.ConfigEditor\DeepSpaceChinese.ConfigEditor.csproj") `
    -c $Configuration --configfile (Join-Path $projectRoot "NuGet.Config")
if ($LASTEXITCODE -ne 0) { throw "配置编辑器编译失败。" }

Copy-Item -LiteralPath (Join-Path $bepInExRoot "BepInEx") -Destination $packageRoot -Recurse -Force
Copy-Item -LiteralPath (Join-Path $bepInExRoot "winhttp.dll") -Destination $packageRoot -Force
Copy-Item -LiteralPath (Join-Path $bepInExRoot "doorstop_config.ini") -Destination $packageRoot -Force
Copy-Item -LiteralPath (Join-Path $projectRoot "patch\DeepSpaceChinese.ini") -Destination $packageRoot -Force
Copy-Item -LiteralPath (Join-Path $projectRoot "patch\README_简体中文.txt") -Destination $contentRoot -Force
$fixSource = Join-Path $projectRoot "patch\Fix"
$fixOutput = Join-Path $contentRoot "Fix"
foreach ($fixFile in Get-ChildItem -LiteralPath $fixSource -File -Recurse) {
    $relativePath = $fixFile.FullName.Substring($fixSource.Length).TrimStart('\')
    $destination = Join-Path $fixOutput $relativePath
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $destination) | Out-Null
    Copy-Item -LiteralPath $fixFile.FullName -Destination $destination -Force
}
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
Assert-NoLiteralSystemDriveArtifact
