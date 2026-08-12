param(
    [string]$Configuration = "Release",
    [string]$GameRoot = ""
)

$ErrorActionPreference = "Stop"
$projectRoot = (Resolve-Path -LiteralPath (Split-Path -Parent $PSScriptRoot)).Path
. (Join-Path $PSScriptRoot "resolve_game_root.ps1")
$gameRootPath = Resolve-GameRootPath -GameRoot $GameRoot -ProjectRoot $projectRoot
$gameManagedDir = Resolve-GameManagedDirectory -GameRoot $gameRootPath
$projectDrive = Split-Path -Qualifier $projectRoot
$env:SystemDrive = $projectDrive.TrimEnd('\')
if ([string]::IsNullOrWhiteSpace($env:ProgramData) -or $env:ProgramData.Contains('%') -or
    -not [IO.Path]::IsPathRooted($env:ProgramData)) {
    $env:ProgramData = Join-Path $env:SystemDrive "ProgramData"
}
if ([string]::IsNullOrWhiteSpace($env:ALLUSERSPROFILE) -or
    $env:ALLUSERSPROFILE.Contains('%') -or
    -not [IO.Path]::IsPathRooted($env:ALLUSERSPROFILE)) {
    $env:ALLUSERSPROFILE = $env:ProgramData
}

& (Join-Path $PSScriptRoot "build_patch.ps1") `
    -Configuration $Configuration `
    -GameRoot $gameRootPath

python (Join-Path $projectRoot "tests\test_build_runtime.py")
if ($LASTEXITCODE -ne 0) { throw "build_runtime 测试失败。" }

python (Join-Path $projectRoot "tests\test_extracted_cache.py")
if ($LASTEXITCODE -ne 0) { throw "提取缓存测试失败。" }

python (Join-Path $projectRoot "tests\test_extraction_rules.py")
if ($LASTEXITCODE -ne 0) { throw "提取规则测试失败。" }

python (Join-Path $projectRoot "tests\test_inspect_puzzles.py")
if ($LASTEXITCODE -ne 0) { throw "题目提取工具测试失败。" }

python (Join-Path $projectRoot "tests\test_translation_text_checks.py")
if ($LASTEXITCODE -ne 0) { throw "译文标点规范测试失败。" }

python (Join-Path $projectRoot "tests\test_dictionary_trigger_conflicts.py")
if ($LASTEXITCODE -ne 0) { throw "词典中文触发冲突校验测试失败。" }

$pythonSources = @(
    Get-ChildItem -LiteralPath (Join-Path $projectRoot "tools") -Filter "*.py" -File
    Get-ChildItem -LiteralPath (Join-Path $projectRoot "work\formal_batches") -Filter "*.py" -File
) | Select-Object -ExpandProperty FullName
python -m py_compile $pythonSources
if ($LASTEXITCODE -ne 0) { throw "Python 语法检查失败。" }

dotnet build (Join-Path $projectRoot "tests\DeepSpaceChinese.RuntimeTests\DeepSpaceChinese.RuntimeTests.csproj") `
    -c $Configuration --configfile (Join-Path $projectRoot "NuGet.Config") `
    "-p:GameManagedDir=$gameManagedDir"
if ($LASTEXITCODE -ne 0) { throw "运行时测试项目编译失败。" }

& (Join-Path $projectRoot "tests\DeepSpaceChinese.RuntimeTests\bin\$Configuration\net472\DeepSpaceChinese.RuntimeTests.exe")
if ($LASTEXITCODE -ne 0) { throw "运行时自测失败。" }

python (Join-Path $PSScriptRoot "audit_translation_outputs.py") `
    (Join-Path $projectRoot "work\formal_batches\manifest.json") `
    (Join-Path $projectRoot "work\glossary.locked.json")
if ($LASTEXITCODE -ne 0) { throw "翻译产物审计失败。" }

Write-Host "项目完整测试通过。"
