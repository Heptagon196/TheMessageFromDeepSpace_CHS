《The Message from Deep Space》词典对白条件修正

此目录中的每个 JSON 文件对应一段游戏对白，文件名和 dialogue_chunk_id 使用
DialogueChunk.UniqueID。此编号是资源内部的对白编号，不是谜题编号。

修正规则会同时用于：

1. 构建中文附加触发表；
2. 游戏运行时修正原版 AdvancedListener 的条件。

运行时只有在对白编号、监听频道、英文条件和原始词条 ID 全部吻合时才会修改；
若游戏更新后任一数据不同，补丁会保留原数据并在日志中报错。
