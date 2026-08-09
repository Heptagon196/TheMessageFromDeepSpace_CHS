$ErrorActionPreference = 'Stop'

$projectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$gameRoot = (Resolve-Path -LiteralPath (Join-Path $projectRoot '..')).Path
$cecilPath = Join-Path $gameRoot 'BepInEx\core\Mono.Cecil.dll'
$assemblyPath = Join-Path $gameRoot 'The Message from Deep Space_Data\Managed\Assembly-CSharp.dll'

[void][Reflection.Assembly]::LoadFrom($cecilPath)
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
