param(
    [Parameter(Mandatory = $true)]
    [string[]]$DotNetArguments
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
. (Join-Path $PSScriptRoot 'initialize_tool_environment.ps1') -ProjectRoot $projectRoot

& dotnet @DotNetArguments
$exitCode = $LASTEXITCODE
Assert-NoLiteralSystemDriveArtifact
exit $exitCode
