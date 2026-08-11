# TheMessageFromDeepSpace_CHS

《The Message From Deep Space / 来自深空的讯息》简体中文本地化项目。

仓库包含翻译源数据、运行时补丁源码、配置编辑器、构建脚本和测试。BepInEx、Fusion Pixel Font、编译结果与补丁 ZIP 均不纳入版本控制。

## 游戏目录

项目可以放在任意目录。仓库提交的 `project.config.json` 提供默认配置：

```json
{
  "GameRoot": ".."
}
```

默认值表示项目父目录是游戏根目录。需要为当前机器指定其他位置时，复制为
`local.config.json` 后修改；该文件已被 Git 忽略：

```powershell
Copy-Item .\project.config.json .\local.config.json
```

两个配置中的 `GameRoot` 都支持绝对路径，也支持相对于配置文件所在目录的
相对路径。构建、测试、题目检查和 Python 资源工具都共享这套配置。

如需临时覆盖配置，可以向 PowerShell 工具传入 `-GameRoot`；命令行相对路径以
当前目录为基准。直接调用 Python 工具时可用 `TMFDS_GAME_ROOT` 环境变量覆盖：

```powershell
& .\tools\build_patch.ps1 -Configuration Release -GameRoot "..\Game"
$env:TMFDS_GAME_ROOT = "..\Game"
python .\tools\extract.py
```

解析优先级为命令行 `-GameRoot` 或 `TMFDS_GAME_ROOT`、`local.config.json`、
`project.config.json`、旧布局默认值。配置文件都不存在时，项目目录的父目录
仍会被视为游戏根目录，以兼容原有目录结构。

需要 Windows PowerShell、Python 3、.NET SDK，以及已安装的正版游戏文件。

## 构建

在项目根目录执行：

```powershell
& .\tools\build_patch.ps1 -Configuration Release
```

构建脚本会自动调用 `tools/ensure_dependencies.ps1`：

- 缺少 BepInEx 5.4.23.5 Windows x64 时，从官方 GitHub Release 下载并解压；
- 缺少 Fusion Pixel Font 12px 非等宽 OTF 时，从官方 GitHub Release 下载；
- 所有下载文件均先校验固定 SHA-256，校验失败即停止构建；
- 第三方文件只保存在被 Git 忽略的 `vendor/` 和 `build/` 中。

生成的可分发目录位于 `build/package/`。它以游戏根目录为压缩包根，适合直接复制或打包为 ZIP。

## 题目及答案修正

已知错误题目及答案的修正规则放在 `patch/Fix/`，构建后位于
`DeepSpaceChinese/Fix/`。每道题使用一个以游戏界面显示题号命名的 JSON，
例如 `80.json`；不要使用资源内部的 `uniqueID`。规则保存原始与修正后的整数
信号数组，也可提供原始与替换答案集。题面和答案集两组字段可各自省略；只提供一组
时只校验并替换该组，两组都提供时必须同时匹配才会原子替换。完整格式见
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
& .\tools\test_project.ps1 -Configuration Release
```

该入口会执行完整补丁构建、Python 测试与语法检查、.NET 运行时自测和翻译产物审计。

## 第三方组件

- [BepInEx 5.4.23.5](https://github.com/BepInEx/BepInEx/releases/tag/v5.4.23.5)，LGPL-2.1；
- [Fusion Pixel Font 2026.07.20](https://github.com/TakWolf/fusion-pixel-font/releases/tag/2026.07.20)，SIL Open Font License 1.1。

二进制文件和字体由构建脚本从上游下载，不存放在本仓库中。
