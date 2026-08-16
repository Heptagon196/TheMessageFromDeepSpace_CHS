param(
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidateRange(1, 9999)]
    [int]$DisplayId,

    [string]$Dictionary,

    [string]$GameRoot = ""
)

$ErrorActionPreference = "Stop"
$projectRoot = (Resolve-Path -LiteralPath (Split-Path -Parent $PSScriptRoot)).Path
. (Join-Path $PSScriptRoot "initialize_tool_environment.ps1") -ProjectRoot $projectRoot
. (Join-Path $PSScriptRoot "resolve_game_root.ps1")
$gameRootPath = Resolve-GameRootPath -GameRoot $GameRoot -ProjectRoot $projectRoot
$null = Resolve-GameManagedDirectory -GameRoot $gameRootPath
$scriptPath = Join-Path $PSScriptRoot "inspect_puzzles.py"

& py -3.12 -c "import sys; print(sys.version)" *> $null
if ($LASTEXITCODE -ne 0) {
    throw "需要 Python 3.12。请先安装 Python 3.12，再重新运行。"
}

$previousGameRoot = $env:TMFDS_GAME_ROOT
try {
    $env:TMFDS_GAME_ROOT = $gameRootPath
    $arguments = @($scriptPath, $DisplayId)
    if (-not [string]::IsNullOrWhiteSpace($Dictionary)) {
        $arguments += @("--dictionary", $Dictionary)
    }
    & py -3.12 @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "题目提取失败。"
    }
}
finally {
    $env:TMFDS_GAME_ROOT = $previousGameRoot
}
