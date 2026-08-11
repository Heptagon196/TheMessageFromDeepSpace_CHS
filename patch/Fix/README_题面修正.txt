《The Message from Deep Space》题目及答案修正规则

此目录中的每个 JSON 文件对应一道题。文件名和 display_id 都必须使用游戏界面上
显示的题号，例如第 80 题使用 80.json。不要使用游戏资源内部的 uniqueID。

文件格式：

{
  "display_id": 80,
  "original_signals": [-11, 1, -2, 6],
  "replacement_signals": [-11, 1, -2, 7],
  "original_answers": [
    [-11, 2],
    [-11, 3]
  ],
  "replacement_answers": [
    [-11, 4],
    [-11, 5]
  ],
  "note": "说明这道题修正了什么"
}

题面修正使用 original_signals 和 replacement_signals；答案集修正使用
original_answers 和 replacement_answers。每组字段都必须同时非空或同时省略，且
至少要提供一组：

1. 只提供题面组：只校验并替换题面，不检查答案集；
2. 只提供答案集组：只校验并替换答案集，不检查题面；
3. 两组都提供：原题面和原始答案集必须都匹配，才同时替换二者。任一项不匹配时
   整条规则都不执行，不会只替换其中一部分。

original_signals 和 replacement_signals 必须直接填写题面中的数字信号，不能填写
玩家词典中的词名。只修正答案集时，可以从 JSON 中省略这两个字段。

每个答案也是一个数字信号数组；第一个答案是主答案，后续答案是可接受的备选答案。
只填写一个 replacement_answers 条目时，修正后不启用备选答案。只修正题面时，
可以省略 original_answers 和 replacement_answers。答案集及其中每个答案都不能为空。

补丁只会在以下条件全部满足时应用修正：

1. DeepSpaceChinese.ini 的 [PuzzleFixes] Enabled = true；
2. 文件名、display_id 和游戏界面显示题号三者一致；
3. 若提供题面组，original_signals 与当前游戏题面逐项完全一致；
4. 若提供答案集组，original_answers 与游戏当前的主答案、备选答案逐项完全一致；
5. 两组都提供时，第 3、4 条必须同时满足。

第 3～5 条是防误修保险：如果游戏更新后题面或答案发生变化，旧规则只会在日志中
报告不匹配，不会部分覆盖或强行覆盖。修改规则后可在游戏中按 F5 重新读取题面和
答案集。
