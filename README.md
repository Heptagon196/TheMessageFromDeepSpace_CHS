# TheMessageFromDeepSpace_CHS

《The Message From Deep Space / 来自深空的讯息》简体中文本地化项目。

仓库包含翻译源数据、运行时补丁源码、配置编辑器、构建脚本和测试。BepInEx、Fusion Pixel Font、编译结果与补丁 ZIP 均不纳入版本控制。

## 目录要求

当前项目的游戏程序集引用采用相对路径。请把本仓库克隆为游戏根目录下的 `TranslationProject`：

```text
The Message from Deep Space/
├─ The Message From Deep Space_Data/
└─ TranslationProject/
```

需要 Windows PowerShell、Python 3、.NET SDK，以及已安装的正版游戏文件。

## 构建

在游戏根目录执行：

```powershell
& .\TranslationProject\tools\build_patch.ps1 -Configuration Release
```

构建脚本会自动调用 `tools/ensure_dependencies.ps1`：

- 缺少 BepInEx 5.4.23.5 Windows x64 时，从官方 GitHub Release 下载并解压；
- 缺少 Fusion Pixel Font 12px 非等宽 OTF 时，从官方 GitHub Release 下载；
- 所有下载文件均先校验固定 SHA-256，校验失败即停止构建；
- 第三方文件只保存在被 Git 忽略的 `vendor/` 和 `build/` 中。

生成的可分发目录位于 `build/package/`。它以游戏根目录为压缩包根，适合直接复制或打包为 ZIP。

## 题面修正

已知错误题面的修正规则放在 `patch/Fix/`，构建后位于
`DeepSpaceChinese/Fix/`。每道题使用一个以游戏界面显示题号命名的 JSON，
例如 `80.json`；不要使用资源内部的 `uniqueID`。规则保存原始与修正后的整数
信号数组，运行时只有在显示题号和原始数组都完全一致时才会替换。完整格式见
`patch/Fix/README_题面修正.txt`。

## 一键查看题目

按游戏界面显示的题号提取题面，并用最近修改的玩家词典解码：

```powershell
& .\TranslationProject\tools\inspect_puzzle.ps1 100
```

首次运行会把固定版本的 UnityPy 和 TypeTreeGeneratorAPI 下载到被 Git 忽略的
`build/` 目录。若要指定另一份词典，可追加
`-Dictionary "完整的 DICTIONARY-*.save 路径"`。

## 测试

```powershell
& .\TranslationProject\tools\test_project.ps1 -Configuration Release
```

该入口会执行完整补丁构建、Python 测试与语法检查、.NET 运行时自测和翻译产物审计。

## 第三方组件

- [BepInEx 5.4.23.5](https://github.com/BepInEx/BepInEx/releases/tag/v5.4.23.5)，LGPL-2.1；
- [Fusion Pixel Font 2026.07.20](https://github.com/TakWolf/fusion-pixel-font/releases/tag/2026.07.20)，SIL Open Font License 1.1。

二进制文件和字体由构建脚本从上游下载，不存放在本仓库中。
