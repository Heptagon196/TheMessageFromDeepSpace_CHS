param(
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidateRange(-10000, -1)]
    [int]$TermId
)

$ErrorActionPreference = "Stop"
$projectRoot = (Resolve-Path -LiteralPath (Split-Path -Parent $PSScriptRoot)).Path
. (Join-Path $PSScriptRoot "initialize_tool_environment.ps1") -ProjectRoot $projectRoot
$scriptPath = Join-Path $PSScriptRoot "inspect_dictionary_trigger.py"

& py -3.12 -c "import sys; print(sys.version)" *> $null
if ($LASTEXITCODE -ne 0) {
    throw "需要 Python 3.12。请先安装 Python 3.12，再重新运行。"
}

& py -3.12 $scriptPath $TermId
if ($LASTEXITCODE -ne 0) {
    throw "词典命名触发查询失败。"
}
