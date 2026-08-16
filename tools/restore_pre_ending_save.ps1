param(
    [string]$SaveDirectory = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$projectRoot = (Resolve-Path -LiteralPath (Split-Path -Parent $PSScriptRoot)).Path
$fixtureDirectory = Join-Path $projectRoot "tests\fixtures\saves\pre-ending-display-976"
$fixtureSave = Join-Path $fixtureDirectory "TMFDS.save"

function Assert-PreEndingSave {
    param([Parameter(Mandatory = $true)][string]$Path)

    $save = Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
    if ([int]$save.currPuzz -ne 975) {
        throw "测试存档 currPuzz 必须为 975（界面题号 976），实际为 $($save.currPuzz)。"
    }
    if ([int]$save.currPuzzListID -ne 144 -or [int]$save.currPuzzLocalID -ne 6) {
        throw "测试存档的题组位置不是结局前的 144/6。"
    }
    if ([bool]$save.translationComplete) {
        throw "测试存档已被标记为完成翻译，不能作为结局前存档。"
    }
    if (@($save.winningResponses.keys) -contains 921) {
        throw "测试存档已经记录了界面题号 976 的通关答案（内部题目 ID 921）。"
    }
    if (@($save.dialogueEntryData | Where-Object { [int]$_.dialogueBankID -eq 1207 }).Count -ne 0) {
        throw "测试存档已经记录了结局前长对话（bank 1207）。"
    }
    if (@($save.achievements) -contains "The Message from Deep Space") {
        throw "测试存档已经取得结局成就。"
    }
}

if (-not (Test-Path -LiteralPath $fixtureSave -PathType Leaf)) {
    throw "缺少测试夹具：$fixtureSave"
}
Assert-PreEndingSave -Path $fixtureSave

if ([string]::IsNullOrWhiteSpace($SaveDirectory)) {
    $localAppData = [Environment]::GetFolderPath("LocalApplicationData")
    $localLow = Join-Path (Split-Path -Parent $localAppData) "LocalLow"
    $SaveDirectory = Join-Path $localLow "Applesinmypants\The Message From Deep Space"
}

$gameExe = (Resolve-Path -LiteralPath (Join-Path (Split-Path -Parent $projectRoot) "The Message From Deep Space.exe")).Path
$runningGame = Get-Process | Where-Object {
    try {
        $_.Path -and [StringComparer]::OrdinalIgnoreCase.Equals($_.Path, $gameExe)
    }
    catch {
        $false
    }
}
if ($runningGame) {
    throw "游戏仍在运行，请先退出游戏再恢复测试存档。"
}

if (-not (Test-Path -LiteralPath $SaveDirectory -PathType Container)) {
    New-Item -ItemType Directory -Path $SaveDirectory | Out-Null
}
$resolvedSaveDirectory = (Resolve-Path -LiteralPath $SaveDirectory).Path
$saveDirectoryItem = Get-Item -LiteralPath $resolvedSaveDirectory -Force
if ($saveDirectoryItem.Attributes -band [IO.FileAttributes]::ReparsePoint) {
    throw "拒绝写入符号链接或联接目录：$resolvedSaveDirectory"
}

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$target = Join-Path $resolvedSaveDirectory "TMFDS.save"
if (Test-Path -LiteralPath $target -PathType Leaf) {
    $backup = Join-Path $resolvedSaveDirectory "TMFDS.pre-test-restore-$timestamp.save"
    Copy-Item -LiteralPath $target -Destination $backup
    Write-Host "已备份：$backup"
}
Copy-Item -LiteralPath $fixtureSave -Destination $target -Force
Write-Host "已恢复：$target"

Assert-PreEndingSave -Path $target
Write-Host "结局前测试存档恢复完成：当前为界面题号 976，答题后将进入结局流程。"
