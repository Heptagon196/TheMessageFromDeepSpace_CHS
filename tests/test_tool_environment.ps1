$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$names = @(
    'SystemDrive', 'ProgramData', 'ALLUSERSPROFILE', 'ProgramFiles', 'ProgramFiles(x86)',
    'ProgramW6432', 'CommonProgramFiles', 'CommonProgramFiles(x86)', 'CommonProgramW6432',
    'USERPROFILE', 'LOCALAPPDATA', 'APPDATA', 'DOTNET_CLI_HOME'
)
$saved = @{}
foreach ($name in $names) {
    $saved[$name] = [Environment]::GetEnvironmentVariable($name, 'Process')
}

try {
    $env:SystemDrive = ''
    $env:ProgramData = '%SystemDrive%\ProgramData'
    $env:ALLUSERSPROFILE = '%SystemDrive%\ProgramData'
    $env:ProgramFiles = ''
    ${env:ProgramFiles(x86)} = ''
    $env:LOCALAPPDATA = ''
    $env:APPDATA = ''
    $env:DOTNET_CLI_HOME = ''

    . (Join-Path $projectRoot 'tools\initialize_tool_environment.ps1') -ProjectRoot $projectRoot

    foreach ($name in $names) {
        $value = [Environment]::GetEnvironmentVariable($name, 'Process')
        if ([string]::IsNullOrWhiteSpace($value) -or $value.Contains('%')) {
            throw "环境变量未正确初始化：$name=$value"
        }
    }
    if ($env:SystemDrive -notmatch '^[A-Za-z]:$') {
        throw "SystemDrive 格式错误：$env:SystemDrive"
    }
    foreach ($name in $names | Where-Object { $_ -ne 'SystemDrive' }) {
        $value = [Environment]::GetEnvironmentVariable($name, 'Process')
        if ($value -notmatch '^[A-Za-z]:\\') {
            throw "环境路径不是绝对路径：$name=$value"
        }
    }
    $pathKeys = @([Environment]::GetEnvironmentVariables('Process').Keys |
        Where-Object { $_ -ieq 'PATH' })
    if ($pathKeys.Count -ne 1) {
        throw "PATH 大小写别名未归一：$($pathKeys -join ', ')"
    }
    $probe = [Diagnostics.ProcessStartInfo]::new()
    $probe.UseShellExecute = $false
    $probe.EnvironmentVariables['SystemDrive'] = $env:SystemDrive
    Assert-NoLiteralSystemDriveArtifact
    Write-Host '工具环境初始化测试通过。'
}
finally {
    foreach ($name in $names) {
        [Environment]::SetEnvironmentVariable($name, $saved[$name], 'Process')
    }
}
