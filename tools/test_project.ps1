param(
    [string]$Configuration = "Release",
    [string]$GameRoot = ""
)

$ErrorActionPreference = "Stop"
$projectRoot = (Resolve-Path -LiteralPath (Split-Path -Parent $PSScriptRoot)).Path
. (Join-Path $PSScriptRoot "initialize_tool_environment.ps1") -ProjectRoot $projectRoot
. (Join-Path $PSScriptRoot "resolve_game_root.ps1")
$gameRootPath = Resolve-GameRootPath -GameRoot $GameRoot -ProjectRoot $projectRoot
$gameManagedDir = Resolve-GameManagedDirectory -GameRoot $gameRootPath
& (Join-Path $PSScriptRoot "build_patch.ps1") `
    -Configuration $Configuration `
    -GameRoot $gameRootPath

& py -3.12 (Join-Path $PSScriptRoot "python_runtime.py")
if ($LASTEXITCODE -ne 0) { throw "Unity 资产分析依赖预检失败。" }

& py -3.12 (Join-Path $projectRoot "tests\test_python_runtime.py")
if ($LASTEXITCODE -ne 0) { throw "Unity 资产分析依赖测试失败。" }

python (Join-Path $projectRoot "tests\test_build_runtime.py")
if ($LASTEXITCODE -ne 0) { throw "build_runtime 测试失败。" }

python (Join-Path $projectRoot "tests\test_extracted_cache.py")
if ($LASTEXITCODE -ne 0) { throw "提取缓存测试失败。" }

python (Join-Path $projectRoot "tests\test_extraction_rules.py")
if ($LASTEXITCODE -ne 0) { throw "提取规则测试失败。" }

python (Join-Path $projectRoot "tests\test_inspect_puzzles.py")
if ($LASTEXITCODE -ne 0) { throw "题目提取工具测试失败。" }

python (Join-Path $projectRoot "tests\test_pre_ending_save_fixture.py")
if ($LASTEXITCODE -ne 0) { throw "结局前测试存档校验失败。" }

python (Join-Path $projectRoot "tests\test_translation_text_checks.py")
if ($LASTEXITCODE -ne 0) { throw "译文标点规范测试失败。" }

python (Join-Path $projectRoot "tools\audit_dialogue_part_boundaries.py")
if ($LASTEXITCODE -ne 0) { throw "对白清屏 PART 边界校验失败。" }

python (Join-Path $projectRoot "tests\test_dictionary_trigger_conflicts.py")
if ($LASTEXITCODE -ne 0) { throw "词典中文触发冲突校验测试失败。" }

python (Join-Path $projectRoot "tests\test_update_translation.py")
if ($LASTEXITCODE -ne 0) { throw "一键译文修改工具测试失败。" }

& (Join-Path $projectRoot "tests\test_tool_environment.ps1")
if ($LASTEXITCODE -ne 0) { throw "工具环境初始化测试失败。" }

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
Assert-NoLiteralSystemDriveArtifact
