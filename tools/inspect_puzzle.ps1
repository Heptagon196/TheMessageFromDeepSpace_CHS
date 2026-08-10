param(
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidateRange(1, 9999)]
    [int]$DisplayId,

    [string]$Dictionary
)

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$dependencyDir = Join-Path $projectRoot "build\puzzle-inspector-python"
$scriptPath = Join-Path $PSScriptRoot "inspect_puzzles.py"

& py -3.12 -c "import sys; print(sys.version)" *> $null
if ($LASTEXITCODE -ne 0) {
    throw "需要 Python 3.12。请先安装 Python 3.12，再重新运行。"
}

$unityPyMarker = Join-Path $dependencyDir "UnityPy\__init__.py"
$typeTreeMarker = Join-Path $dependencyDir "TypeTreeGeneratorAPI\__init__.py"
if (
    -not (Test-Path -LiteralPath $unityPyMarker -PathType Leaf) -or
    -not (Test-Path -LiteralPath $typeTreeMarker -PathType Leaf)
) {
    New-Item -ItemType Directory -Path $dependencyDir -Force | Out-Null
    Write-Host "首次使用：正在下载题目提取依赖……"
    & py -3.12 -m pip install --disable-pip-version-check --upgrade --target $dependencyDir `
        "UnityPy==1.25.3" "TypeTreeGeneratorAPI==0.0.10"
    if ($LASTEXITCODE -ne 0) {
        throw "UnityPy 下载失败。"
    }
}

$previousPackages = $env:TMFDS_PYTHON_PACKAGES
try {
    $env:TMFDS_PYTHON_PACKAGES = $dependencyDir
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
    $env:TMFDS_PYTHON_PACKAGES = $previousPackages
}
