using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DeepSpaceChinese.ConfigEditor;

internal sealed class IniDocument
{
    private readonly List<string> _lines;

    private IniDocument(IEnumerable<string> lines)
    {
        _lines = new List<string>(lines);
    }

    public static IniDocument LoadOrDefault(string path)
    {
        return File.Exists(path)
            ? new IniDocument(File.ReadAllLines(path, Encoding.UTF8))
            : new IniDocument(DefaultIniText.Replace("\r\n", "\n").Split('\n'));
    }

    public string Get(string section, string key, string fallback)
    {
        string currentSection = string.Empty;
        foreach (string raw in _lines)
        {
            string line = raw.Trim();
            if (line.StartsWith("[") && line.EndsWith("]"))
            {
                currentSection = line.Substring(1, line.Length - 2).Trim();
                continue;
            }
            if (!string.Equals(currentSection, section, StringComparison.OrdinalIgnoreCase))
                continue;
            int equals = line.IndexOf('=');
            if (equals <= 0)
                continue;
            if (string.Equals(line.Substring(0, equals).Trim(), key,
                    StringComparison.OrdinalIgnoreCase))
                return line.Substring(equals + 1).Trim();
        }
        return fallback;
    }

    public void Set(string section, string key, string value)
    {
        int sectionStart = -1;
        int sectionEnd = _lines.Count;
        string currentSection = string.Empty;
        for (int index = 0; index < _lines.Count; index++)
        {
            string line = _lines[index].Trim();
            if (line.StartsWith("[") && line.EndsWith("]"))
            {
                string nextSection = line.Substring(1, line.Length - 2).Trim();
                if (sectionStart >= 0)
                {
                    sectionEnd = index;
                    break;
                }
                currentSection = nextSection;
                if (string.Equals(currentSection, section, StringComparison.OrdinalIgnoreCase))
                    sectionStart = index;
                continue;
            }
            if (sectionStart < 0 || !string.Equals(currentSection, section,
                    StringComparison.OrdinalIgnoreCase))
                continue;
            int equals = line.IndexOf('=');
            if (equals > 0 && string.Equals(line.Substring(0, equals).Trim(), key,
                    StringComparison.OrdinalIgnoreCase))
            {
                _lines[index] = key + " = " + (value ?? string.Empty);
                return;
            }
        }

        if (sectionStart < 0)
        {
            if (_lines.Count > 0 && _lines[_lines.Count - 1].Length != 0)
                _lines.Add(string.Empty);
            _lines.Add("[" + section + "]");
            _lines.Add(key + " = " + (value ?? string.Empty));
            return;
        }
        _lines.Insert(sectionEnd, key + " = " + (value ?? string.Empty));
    }

    public void RemoveSection(string section)
    {
        int start = -1;
        int end = _lines.Count;
        for (int index = 0; index < _lines.Count; index++)
        {
            string line = _lines[index].Trim();
            if (!line.StartsWith("[") || !line.EndsWith("]"))
                continue;
            string name = line.Substring(1, line.Length - 2).Trim();
            if (start >= 0)
            {
                end = index;
                break;
            }
            if (string.Equals(name, section, StringComparison.OrdinalIgnoreCase))
                start = index;
        }
        if (start >= 0)
            _lines.RemoveRange(start, end - start);
    }

    public void SaveAtomic(string path)
    {
        string directory = Path.GetDirectoryName(path);
        if (string.IsNullOrEmpty(directory))
            directory = AppDomain.CurrentDomain.BaseDirectory;
        Directory.CreateDirectory(directory);
        string temp = path + ".tmp";
        string backup = path + ".bak";
        File.WriteAllLines(temp, _lines, new UTF8Encoding(false));
        if (File.Exists(path))
            File.Replace(temp, path, backup, true);
        else
            File.Move(temp, path);
    }

    private const string DefaultIniText = @"# 《The Message from Deep Space》简体中文补丁配置
# 常规设置修改后重新启动游戏生效；字体和对白颜色设置可按 F5 热重载。
# “显示模式”也可在游戏运行时用快捷键切换。

[Localization]
# 汉化补丁总开关。
Enabled = true

# 运行时切换“仅译文 / 仅原文”的快捷键。默认 F8；写 None 可禁用。
ToggleModeHotkey = F8

# 运行时重新读取翻译、字体和对白颜色的快捷键。
ReloadTranslationsHotkey = F5

# 缺少译文或译文校验失败时是否显示英文原文。
FallbackToOriginal = true

TranslateDialogue = true
TranslateLogs = true
TranslateUI = true
TranslateSystem = true

[Compatibility]
# 编译词典词名时是否忽略英文字母大小写。
CompilerCaseInsensitive = true

[DialogueColors]
Enabled = true
Akers = #FFD166
Bautista = #7FDBFF
Collins = #FF9BD2
Doppler = #A7E87B
AutoLog = #E6E6E6
Pilot = #C7B8FF
CoPilot = #FFB07C

[Font]
FontSource = Auto
BundledFont = Fonts\fusion-pixel-12px-proportional-zh_hans.otf
FontFile =
SystemFontCandidates = Microsoft YaHei;Noto Sans CJK SC;SimHei";
}
