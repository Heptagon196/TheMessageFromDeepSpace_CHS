param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectRoot
)

$ProjectRoot = [IO.Path]::GetFullPath($ProjectRoot)

# The managed desktop host can expose both PATH and Path in the same native
# environment block. Windows itself accepts that, but ProcessStartInfo builds a
# case-insensitive dictionary and fails with "Key PATH/Path already added". Collapse
# the aliases before any tool or game process is launched.
$effectivePath = [Environment]::GetEnvironmentVariable('Path', 'Process')
if (-not [string]::IsNullOrWhiteSpace($effectivePath)) {
    [Environment]::SetEnvironmentVariable('PATH', $null, 'Process')
    [Environment]::SetEnvironmentVariable('Path', $effectivePath, 'Process')
}

function Test-UsableWindowsPath([string]$Path) {
    return -not [string]::IsNullOrWhiteSpace($Path) -and
        -not $Path.Contains('%') -and
        $Path -match '^[A-Za-z]:\\'
}

function Get-WindowsDriveRoot {
    $candidates = @(
        [Environment]::SystemDirectory,
        $ProjectRoot
    )
    foreach ($candidate in $candidates) {
        if ([string]::IsNullOrWhiteSpace($candidate)) {
            continue
        }
        $root = [IO.Path]::GetPathRoot([IO.Path]::GetFullPath($candidate))
        if ($root -match '^[A-Za-z]:\\$') {
            return $root
        }
    }
    throw "无法确定 Windows 系统盘。"
}

function Assert-NoLiteralSystemDriveArtifact {
    $roots = @($ProjectRoot, (Get-Location).Path) | Sort-Object -Unique
    foreach ($root in $roots) {
        $invalidPath = Join-Path $root "%SystemDrive%"
        if (Test-Path -LiteralPath $invalidPath) {
            throw "检测到未展开的 %SystemDrive% 目录：$invalidPath。它是旧版工具在 Windows 环境变量缺失时产生的构建残留。"
        }
    }
}

$windowsDriveRoot = Get-WindowsDriveRoot
$windowsDrive = $windowsDriveRoot.TrimEnd('\')
$env:SystemDrive = $windowsDrive

$knownFolders = [ordered]@{
    ProgramData = [IO.Path]::Combine($windowsDriveRoot, 'ProgramData')
    ALLUSERSPROFILE = [IO.Path]::Combine($windowsDriveRoot, 'ProgramData')
    ProgramFiles = [IO.Path]::Combine($windowsDriveRoot, 'Program Files')
    'ProgramFiles(x86)' = [IO.Path]::Combine($windowsDriveRoot, 'Program Files (x86)')
    ProgramW6432 = [IO.Path]::Combine($windowsDriveRoot, 'Program Files')
    CommonProgramFiles = [IO.Path]::Combine($windowsDriveRoot, 'Program Files', 'Common Files')
    'CommonProgramFiles(x86)' = [IO.Path]::Combine($windowsDriveRoot, 'Program Files (x86)', 'Common Files')
    CommonProgramW6432 = [IO.Path]::Combine($windowsDriveRoot, 'Program Files', 'Common Files')
}
foreach ($entry in $knownFolders.GetEnumerator()) {
    $current = [Environment]::GetEnvironmentVariable($entry.Key, 'Process')
    if (-not (Test-UsableWindowsPath $current)) {
        [Environment]::SetEnvironmentVariable($entry.Key, $entry.Value, 'Process')
    }
}

# 受控终端也可能清空用户数据目录。工具缓存统一放在 build 下，既可写又不会进入 Git。
$toolEnvironment = Join-Path $ProjectRoot 'build\tool-environment'
$userProfile = $env:USERPROFILE
if (-not (Test-UsableWindowsPath $userProfile)) {
    $userProfile = Join-Path $toolEnvironment 'UserProfile'
    $env:USERPROFILE = $userProfile
}
if (-not (Test-UsableWindowsPath $env:LOCALAPPDATA)) {
    $env:LOCALAPPDATA = Join-Path $toolEnvironment 'LocalAppData'
}
if (-not (Test-UsableWindowsPath $env:APPDATA)) {
    $env:APPDATA = Join-Path $toolEnvironment 'AppData'
}
if (-not (Test-UsableWindowsPath $env:DOTNET_CLI_HOME)) {
    $env:DOTNET_CLI_HOME = Join-Path $toolEnvironment 'DotNetHome'
}
New-Item -ItemType Directory -Force -Path `
    $env:USERPROFILE, $env:LOCALAPPDATA, $env:APPDATA, $env:DOTNET_CLI_HOME | Out-Null

Assert-NoLiteralSystemDriveArtifact
