param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$Script,

    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$ScriptArguments = @()
)

$ErrorActionPreference = "Stop"
$projectRoot = (Resolve-Path -LiteralPath (Split-Path -Parent $PSScriptRoot)).Path
. (Join-Path $PSScriptRoot "initialize_tool_environment.ps1") -ProjectRoot $projectRoot

$scriptPath = $Script
if (-not [System.IO.Path]::IsPathRooted($scriptPath)) {
    $scriptPath = Join-Path $projectRoot $scriptPath
}
$scriptPath = (Resolve-Path -LiteralPath $scriptPath).Path

& py -3.12 $scriptPath @ScriptArguments
if ($LASTEXITCODE -ne 0) {
    throw "Python 工具执行失败（退出码 $LASTEXITCODE）：$scriptPath"
}

Assert-NoLiteralSystemDriveArtifact
