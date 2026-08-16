param(
    [string]$GameRoot = '',
    [string]$CecilPath = ''
)

$ErrorActionPreference = 'Stop'

$projectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
. (Join-Path $PSScriptRoot 'initialize_tool_environment.ps1') -ProjectRoot $projectRoot
. (Join-Path $PSScriptRoot 'resolve_game_root.ps1')
$gameRootPath = Resolve-GameRootPath -GameRoot $GameRoot -ProjectRoot $projectRoot
$gameManagedDir = Resolve-GameManagedDirectory -GameRoot $gameRootPath
if ([string]::IsNullOrWhiteSpace($CecilPath)) {
    $CecilPath = Join-Path $projectRoot 'vendor\BepInEx\extracted\BepInEx\core\Mono.Cecil.dll'
}
elseif (-not [IO.Path]::IsPathRooted($CecilPath)) {
    $CecilPath = Join-Path (Get-Location).Path $CecilPath
}
$CecilPath = [IO.Path]::GetFullPath($CecilPath)
$assemblyPath = Join-Path $gameManagedDir 'Assembly-CSharp.dll'

if (-not (Test-Path -LiteralPath $CecilPath -PathType Leaf)) {
    throw "Mono.Cecil 程序集不存在：$CecilPath"
}
if (-not (Test-Path -LiteralPath $assemblyPath -PathType Leaf)) {
    throw "游戏程序集不存在：$assemblyPath"
}

[void][Reflection.Assembly]::LoadFrom($CecilPath)
$assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($assemblyPath)
$actual = @(
    foreach ($type in $assembly.MainModule.Types) {
        foreach ($field in $type.Fields) {
            if ($field.FieldType.FullName -eq 'TMPro.TMP_InputField') {
                "$($type.FullName).$($field.Name)"
            }
        }
    }
) | Sort-Object -Unique

$supported = @(
    'InputTextDummy.inputField'
    'NameTranslator.nameEntryInput'
    'ProgressLog.translatorInput'
) | Sort-Object -Unique

$unsupported = @($actual | Where-Object { $_ -notin $supported })
$stale = @($supported | Where-Object { $_ -notin $actual })
if ($unsupported.Count -gt 0 -or $stale.Count -gt 0) {
    if ($unsupported.Count -gt 0) {
        Write-Error ('未纳入中文输入支持的 TMP 输入框：' + ($unsupported -join ', '))
    }
    if ($stale.Count -gt 0) {
        Write-Error ('覆盖清单中已不存在的 TMP 输入框：' + ($stale -join ', '))
    }
    exit 1
}

Write-Output ('Unicode input audit passed: ' + ($actual -join ', '))
