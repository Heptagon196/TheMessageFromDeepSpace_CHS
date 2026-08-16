# 结局前测试存档（界面题号 976）

这组夹具保存于回答界面题号 976 之前。答对该题后，游戏会进入传输 977 及结局流程。

关键状态：

- `currPuzz = 975`：存档内部使用从 0 开始的下标，对应界面题号 976。
- 题组/组内位置为 `144 / 6`。
- 内部题目 ID 921 尚无通关答案。
- 结局前长对话 1207、结局成就及 `translationComplete` 均未写入。

夹具只保存剧情进度 `TMFDS.save`，恢复时不会改动玩家当前的词典存档。

从 `TranslationProject` 根目录恢复：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/restore_pre_ending_save.ps1
```

恢复工具要求游戏已经退出，并会先在原存档目录生成带时间戳的备份，再覆盖测试存档。
