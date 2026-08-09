《The Message from Deep Space》简体中文补丁

安装：
把 ZIP 内的所有文件和文件夹解压到游戏根目录，即游戏 EXE 所在目录。
如果系统询问是否合并文件夹，选择合并。

配置：
游戏根目录的 DeepSpaceChinese.ini 是唯一需要用户编辑的配置文件。
也可以双击游戏根目录的 DeepSpaceChinese.ConfigEditor.exe，通过图形界面修改并校验配置。
配置工具使用 Windows 自带的 .NET Framework WinForms，不附带额外运行时。
默认按 F8 可在“仅译文”和“仅原文”模式之间即时切换；快捷键可在 INI 中修改。
默认按 F5 可重新读取四个翻译 JSON、INI 的 [Font]、[DialogueColors]、[Compatibility] 和中文字体并立即应用，无需重启游戏。
字体热重载失败时会保留当前字体；译文与字体相互独立，任一失败都不会覆盖其旧版本。
默认开启按说话者着色，可在 INI 中关闭或分别修改七种高对比度颜色。
过长对白会按实际文本框宽度自动排版：不超过上限 1.5 倍时缩小字号，更长时自动分页。
起名界面在译文模式下显示为“输入框 + 博士”，纯原文模式恢复“Dr. + 姓名”。
所有输入均使用游戏原本的 TMP 输入框；补丁会修复游戏每帧强制关闭 Windows 输入法的问题。
中文姓名仍沿用游戏输入框的 14 字符上限。
默认开启编译词典词名时忽略英文字母大小写，因此 VAR 可匹配词典中的 var；精确拼写仍优先。

翻译文件：
DeepSpaceChinese\Translations\dialogue.json  全部对白
DeepSpaceChinese\Translations\titles.json    日志标题
DeepSpaceChinese\Translations\ui.json        界面文本
DeepSpaceChinese\Translations\system.json    系统提示和运行时模板

字体：
补丁自带 Fusion Pixel Font（缝合像素字体）12px 非等宽简体中文版，不依赖 Windows 预装字体。
字体许可证位于 DeepSpaceChinese\Licenses。

卸载：
删除 winhttp.dll、doorstop_config.ini、DeepSpaceChinese.ini、DeepSpaceChinese 文件夹，
以及 BepInEx\plugins\DeepSpaceChinese.dll。若没有安装其他 BepInEx 模组，也可删除整个 BepInEx 文件夹。
