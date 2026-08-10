《The Message from Deep Space》题面修正规则

此目录中的每个 JSON 文件对应一道题。文件名和 display_id 都必须使用游戏界面上
显示的题号，例如第 80 题使用 80.json。不要使用游戏资源内部的 uniqueID。

文件格式：

{
  "display_id": 80,
  "original_signals": [-11, 1, -2, 6],
  "replacement_signals": [-11, 1, -2, 7],
  "note": "说明这道题修正了什么"
}

original_signals 和 replacement_signals 必须直接填写题面中的数字信号，不能填写
玩家词典中的词名。补丁只会在以下条件全部满足时替换题面：

1. DeepSpaceChinese.ini 的 [PuzzleFixes] Enabled = true；
2. 文件名、display_id 和游戏界面显示题号三者一致；
3. original_signals 与当前游戏数据逐项完全一致。

第 3 条是防误修保险：如果游戏更新后原题面发生变化，旧规则只会在日志中报告不
匹配，不会强行覆盖。修改规则后可在游戏中按 F5 重新读取。
