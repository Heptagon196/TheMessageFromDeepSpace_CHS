using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace DeepSpaceChinese.ConfigEditor;

internal sealed class ConfigEditorForm : Form
{
    private static readonly KeyValuePair<string, string>[] SpeakerNames =
    {
        new("Akers", "埃克斯"), new("Bautista", "巴蒂斯塔"), new("Collins", "柯林斯"),
        new("Doppler", "多普勒"), new("AutoLog", "自动日志"), new("Pilot", "飞行员"),
        new("CoPilot", "副驾驶"),
    };

    private readonly string _iniPath;
    private IniDocument _document;
    private readonly CheckBox _enabled = new() { Text = "启用汉化补丁", AutoSize = true };
    private readonly TextBox _toggleKey = new();
    private readonly TextBox _reloadKey = new();
    private readonly CheckBox _fallback = new() { Text = "译文缺失或校验失败时显示英文原文", AutoSize = true };
    private readonly CheckBox _dialogue = new() { Text = "对白", AutoSize = true };
    private readonly CheckBox _logs = new() { Text = "日志", AutoSize = true };
    private readonly CheckBox _ui = new() { Text = "界面", AutoSize = true };
    private readonly CheckBox _system = new() { Text = "系统文本", AutoSize = true };
    private readonly CheckBox _compilerCaseInsensitive = new()
    {
        Text = "编译词典词名时忽略英文字母大小写（推荐）", AutoSize = true,
    };
    private readonly CheckBox _puzzleFixes = new()
    {
        Text = "应用已知错误题目及答案的修正规则（推荐）", AutoSize = true,
    };
    private readonly CheckBox _colorsEnabled = new() { Text = "按说话者给对白着色", AutoSize = true };
    private readonly Dictionary<string, TextBox> _colorBoxes = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Button> _colorButtons = new(StringComparer.OrdinalIgnoreCase);
    private readonly ComboBox _fontSource = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox _bundledFont = new();
    private readonly TextBox _fontFile = new();
    private readonly TextBox _systemFonts = new() { Multiline = true, Height = 58, ScrollBars = ScrollBars.Vertical };
    private readonly Label _status = new() { AutoEllipsis = true, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };

    public ConfigEditorForm(string iniPath)
    {
        _iniPath = iniPath;
        Text = "《来自深空的讯息》汉化配置工具 v0.1.64";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(650, 540);
        ClientSize = new Size(720, 590);
        Font = SystemFonts.MessageBoxFont;
        AutoScaleMode = AutoScaleMode.Dpi;

        var tabs = new TabControl { Dock = DockStyle.Fill, Padding = new Point(16, 5) };
        tabs.TabPages.Add(BuildGeneralTab());
        tabs.TabPages.Add(BuildColorTab());
        tabs.TabPages.Add(BuildFontTab());

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false, AutoSize = true, Padding = new Padding(0, 5, 0, 0),
        };
        buttons.Controls.Add(MakeButton("保存并关闭", (_, __) => Save(true), 104));
        buttons.Controls.Add(MakeButton("保存", (_, __) => Save(false), 82));
        buttons.Controls.Add(MakeButton("重新读取", (_, __) => LoadSettings(), 92));
        buttons.Controls.Add(MakeButton("恢复默认", (_, __) => ResetDefaults(), 92));

        var bottom = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, AutoSize = true };
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        bottom.Controls.Add(_status, 0, 0);
        bottom.Controls.Add(buttons, 1, 0);

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, Padding = new Padding(10) };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.Controls.Add(tabs, 0, 0);
        root.Controls.Add(bottom, 0, 1);
        Controls.Add(root);
        AcceptButton = buttons.Controls[1] as Button;

        _fontSource.Items.AddRange(new object[] { "Auto", "Bundled", "File", "System" });
        _fontSource.SelectedIndexChanged += (_, __) => UpdateFontControls();
        _colorsEnabled.CheckedChanged += (_, __) => UpdateColorControls();
        FormClosing += (_, __) => { };
        LoadSettings();
    }

    private TabPage BuildGeneralTab()
    {
        var page = NewPage("常规");
        var table = NewTable();
        table.Controls.Add(_enabled, 0, 0);
        table.SetColumnSpan(_enabled, 3);
        AddTextRow(table, 1, "显示模式切换键", _toggleKey, "默认 F8；在仅译文和仅原文间切换。填写 None 可禁用。");
        AddTextRow(table, 2, "热重载键", _reloadKey, "默认 F5；重载翻译、字体和角色颜色。填写 None 可禁用。");
        table.Controls.Add(_fallback, 0, 3);
        table.SetColumnSpan(_fallback, 3);
        table.Controls.Add(new Label { Text = "启用的翻译类型", AutoSize = true, Margin = new Padding(3, 13, 3, 3) }, 0, 4);
        var types = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
        types.Controls.AddRange(new Control[] { _dialogue, _logs, _ui, _system });
        table.Controls.Add(types, 1, 4);
        table.SetColumnSpan(types, 2);
        table.Controls.Add(_compilerCaseInsensitive, 0, 5);
        table.SetColumnSpan(_compilerCaseInsensitive, 3);
        AddFullWidthHint(table, 6,
            "启用后，VAR 可匹配词典中的 var；精确拼写仍优先，存在歧义时不会误选。");
        table.Controls.Add(_puzzleFixes, 0, 7);
        table.SetColumnSpan(_puzzleFixes, 3);
        AddFullWidthHint(table, 8,
            "题面和答案集可单独修正；两者都填写时，原题面和原始答案集必须同时匹配才会替换。");
        AddFullWidthHint(table, 9,
            "以上兼容项和题目及答案修正规则保存后，可在游戏中按 F5 重载。", false);
        page.Controls.Add(table);
        return page;
    }

    private TabPage BuildColorTab()
    {
        var page = NewPage("角色颜色");
        var table = NewTable();
        table.Controls.Add(_colorsEnabled, 0, 0);
        table.SetColumnSpan(_colorsEnabled, 3);
        int row = 1;
        foreach (KeyValuePair<string, string> speaker in SpeakerNames)
        {
            var box = new TextBox { Width = 150, CharacterCasing = CharacterCasing.Upper };
            var button = MakeButton("选择颜色", (_, __) => PickColor(speaker.Key), 100);
            button.UseVisualStyleBackColor = false;
            button.FlatStyle = FlatStyle.Flat;
            box.TextChanged += (_, __) => UpdateColorPreview(speaker.Key);
            _colorBoxes[speaker.Key] = box;
            _colorButtons[speaker.Key] = button;
            table.Controls.Add(new Label { Text = speaker.Value, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
            table.Controls.Add(box, 1, row);
            table.Controls.Add(button, 2, row);
            row++;
        }
        table.Controls.Add(new Label
        {
            Text = "颜色格式为 #RRGGBB。默认配色针对游戏的深色背景选择。保存后在游戏中按 F5 即可应用。",
            AutoSize = true, ForeColor = Color.DimGray, Margin = new Padding(3, 14, 3, 3),
        }, 0, row);
        table.SetColumnSpan(table.GetControlFromPosition(0, row), 3);
        page.Controls.Add(table);
        return page;
    }

    private TabPage BuildFontTab()
    {
        var page = NewPage("字体");
        var table = NewTable();
        table.Controls.Add(new Label { Text = "字体来源", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
        table.Controls.Add(_fontSource, 1, 0);
        table.SetColumnSpan(_fontSource, 2);
        AddBrowseRow(table, 1, "随包字体", _bundledFont, (_, __) => BrowseFont(_bundledFont, true));
        AddBrowseRow(table, 2, "自定义字体", _fontFile, (_, __) => BrowseFont(_fontFile, false));
        table.Controls.Add(new Label { Text = "系统字体候选", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 3);
        table.Controls.Add(_systemFonts, 1, 3);
        table.SetColumnSpan(_systemFonts, 2);
        table.Controls.Add(new Label
        {
            Text = "Auto：随包字体 → 自定义文件 → 系统字体。系统字体名称以分号分隔。\n字体设置保存后可在游戏中按 F5 热重载。",
            AutoSize = true, ForeColor = Color.DimGray, Margin = new Padding(3, 16, 3, 3),
        }, 0, 4);
        table.SetColumnSpan(table.GetControlFromPosition(0, 4), 3);
        page.Controls.Add(table);
        return page;
    }

    private void LoadSettings()
    {
        try
        {
            _document = IniDocument.LoadOrDefault(_iniPath);
            ApplyToControls(EditorSettings.FromIni(_document));
            SetStatus((File.Exists(_iniPath) ? "已读取：" : "尚未找到配置，已载入默认值：") + _iniPath, false);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "读取配置失败：\n" + ex.Message, "配置工具", MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void Save(bool close)
    {
        try
        {
            EditorSettings settings = ReadControls();
            string error = settings.Validate();
            if (error != null)
            {
                MessageBox.Show(this, error, "配置有误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            settings.ApplyTo(_document);
            _document.SaveAtomic(_iniPath);
            SetStatus("已保存。翻译、字体、角色颜色、兼容项和题目及答案修正规则可在游戏中按 F5 应用。", true);
            if (close)
                Close();
        }
        catch (UnauthorizedAccessException)
        {
            MessageBox.Show(this, "没有权限写入游戏目录。请关闭游戏，或以管理员身份运行本工具。",
                "无法保存", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "保存配置失败：\n" + ex.Message, "无法保存", MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void ResetDefaults()
    {
        if (MessageBox.Show(this, "要把界面中的所有选项恢复为默认值吗？\n点击保存后才会写入 INI。",
                "恢复默认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        {
            ApplyToControls(new EditorSettings());
            SetStatus("已在界面中恢复默认值，尚未保存。", false);
        }
    }

    private EditorSettings ReadControls()
    {
        var settings = new EditorSettings
        {
            Enabled = _enabled.Checked,
            ToggleModeHotkey = _toggleKey.Text,
            ReloadTranslationsHotkey = _reloadKey.Text,
            FallbackToOriginal = _fallback.Checked,
            TranslateDialogue = _dialogue.Checked,
            TranslateLogs = _logs.Checked,
            TranslateUI = _ui.Checked,
            TranslateSystem = _system.Checked,
            CompilerCaseInsensitive = _compilerCaseInsensitive.Checked,
            PuzzleFixesEnabled = _puzzleFixes.Checked,
            DialogueColorsEnabled = _colorsEnabled.Checked,
            FontSource = Convert.ToString(_fontSource.SelectedItem) ?? "Auto",
            BundledFont = _bundledFont.Text,
            FontFile = _fontFile.Text,
            SystemFontCandidates = _systemFonts.Text,
        };
        foreach (KeyValuePair<string, TextBox> pair in _colorBoxes)
            settings.Colors[pair.Key] = pair.Value.Text.Trim();
        return settings;
    }

    private void ApplyToControls(EditorSettings settings)
    {
        _enabled.Checked = settings.Enabled;
        _toggleKey.Text = settings.ToggleModeHotkey;
        _reloadKey.Text = settings.ReloadTranslationsHotkey;
        _fallback.Checked = settings.FallbackToOriginal;
        _dialogue.Checked = settings.TranslateDialogue;
        _logs.Checked = settings.TranslateLogs;
        _ui.Checked = settings.TranslateUI;
        _system.Checked = settings.TranslateSystem;
        _compilerCaseInsensitive.Checked = settings.CompilerCaseInsensitive;
        _puzzleFixes.Checked = settings.PuzzleFixesEnabled;
        _colorsEnabled.Checked = settings.DialogueColorsEnabled;
        foreach (KeyValuePair<string, TextBox> pair in _colorBoxes)
            pair.Value.Text = settings.Colors[pair.Key];
        int sourceIndex = _fontSource.FindStringExact(settings.FontSource);
        _fontSource.SelectedIndex = sourceIndex >= 0 ? sourceIndex : 0;
        _bundledFont.Text = settings.BundledFont;
        _fontFile.Text = settings.FontFile;
        _systemFonts.Text = settings.SystemFontCandidates;
        UpdateFontControls();
        UpdateColorControls();
    }

    private void PickColor(string key)
    {
        using var dialog = new ColorDialog { FullOpen = true };
        if (TryParseColor(_colorBoxes[key].Text, out Color current))
            dialog.Color = current;
        if (dialog.ShowDialog(this) == DialogResult.OK)
            _colorBoxes[key].Text = $"#{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}";
    }

    private void UpdateColorPreview(string key)
    {
        Button button = _colorButtons[key];
        if (!TryParseColor(_colorBoxes[key].Text, out Color color))
        {
            button.BackColor = SystemColors.Control;
            button.ForeColor = Color.Firebrick;
            return;
        }
        button.BackColor = color;
        double luminance = 0.299 * color.R + 0.587 * color.G + 0.114 * color.B;
        button.ForeColor = luminance > 150 ? Color.Black : Color.White;
    }

    private void UpdateColorControls()
    {
        foreach (TextBox box in _colorBoxes.Values)
            box.Enabled = _colorsEnabled.Checked;
        foreach (Button button in _colorButtons.Values)
            button.Enabled = _colorsEnabled.Checked;
    }

    private void BrowseFont(TextBox target, bool bundled)
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "字体文件 (*.otf;*.ttf;*.ttc)|*.otf;*.ttf;*.ttc|所有文件 (*.*)|*.*",
            CheckFileExists = true,
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;
        string gameRoot = Path.GetDirectoryName(_iniPath) ?? AppDomain.CurrentDomain.BaseDirectory;
        string basePath = bundled ? Path.Combine(gameRoot, "DeepSpaceChinese") : gameRoot;
        target.Text = MakeRelativeIfInside(basePath, dialog.FileName);
    }

    private void UpdateFontControls()
    {
        string source = Convert.ToString(_fontSource.SelectedItem) ?? "Auto";
        _bundledFont.Enabled = source is "Auto" or "Bundled";
        _fontFile.Enabled = source is "Auto" or "File";
        _systemFonts.Enabled = source is "Auto" or "System";
    }

    private void SetStatus(string text, bool success)
    {
        _status.Text = text;
        _status.ForeColor = success ? Color.DarkGreen : SystemColors.ControlText;
    }

    private static TabPage NewPage(string title) => new(title) { Padding = new Padding(14), AutoScroll = true };

    private static TableLayoutPanel NewTable()
    {
        var table = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 3, Padding = new Padding(8) };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        return table;
    }

    private static Button MakeButton(string text, EventHandler click, int width)
    {
        var button = new Button { Text = text, Width = width, Height = 30, UseVisualStyleBackColor = true };
        button.Click += click;
        return button;
    }

    private static void AddTextRow(TableLayoutPanel table, int row, string label, TextBox box, string hint)
    {
        table.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
        box.Dock = DockStyle.Fill;
        table.Controls.Add(box, 1, row);
        table.Controls.Add(new Label { Text = hint, AutoSize = true, ForeColor = Color.DimGray, MaximumSize = new Size(260, 0) }, 2, row);
    }

    private static void AddFullWidthHint(TableLayoutPanel table, int row, string text,
        bool indent = true)
    {
        var hint = new Label
        {
            Text = text,
            AutoSize = true,
            ForeColor = Color.DimGray,
            MaximumSize = new Size(620, 0),
            Margin = indent ? new Padding(24, 0, 3, 8) : new Padding(3, 4, 3, 3),
        };
        table.Controls.Add(hint, 0, row);
        table.SetColumnSpan(hint, 3);
    }

    private static void AddBrowseRow(TableLayoutPanel table, int row, string label, TextBox box, EventHandler browse)
    {
        table.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
        box.Dock = DockStyle.Fill;
        table.Controls.Add(box, 1, row);
        table.Controls.Add(MakeButton("浏览…", browse, 82), 2, row);
    }

    private static bool TryParseColor(string text, out Color color)
    {
        color = Color.Empty;
        if (string.IsNullOrWhiteSpace(text) || text.Length != 7 || text[0] != '#')
            return false;
        try
        {
            color = Color.FromArgb(Convert.ToInt32(text.Substring(1, 2), 16),
                Convert.ToInt32(text.Substring(3, 2), 16), Convert.ToInt32(text.Substring(5, 2), 16));
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string MakeRelativeIfInside(string basePath, string filePath)
    {
        string root = Path.GetFullPath(basePath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        string full = Path.GetFullPath(filePath);
        if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            return full;
        return full.Substring(root.Length);
    }
}
