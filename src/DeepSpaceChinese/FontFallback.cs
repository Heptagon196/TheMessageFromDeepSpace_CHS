using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using BepInEx.Logging;
using TMPro;
using UnityEngine;

namespace DeepSpaceChinese;

internal sealed class FontFallback
{
    private sealed class DirectBinding
    {
        public TMP_Text Component;
        public TMP_FontAsset OriginalFont;
        public Material OriginalMaterial;
        public TextOverflowModes OriginalOverflowMode;
        public bool UseChineseFont;
    }

    private readonly PatchConfig _config;
    private readonly string _contentRoot;
    private readonly ManualLogSource _log;
    private TMP_FontAsset _font;
    private string _fingerprint;
    private int _fontGeneration;
    private readonly Dictionary<int, DirectBinding> _directBindings = new();

    public FontFallback(PatchConfig config, string contentRoot, ManualLogSource log)
    {
        _config = config;
        _contentRoot = contentRoot;
        _log = log;
    }

    public void EnsureLoaded()
    {
        if (_font == null)
        {
            _font = LoadFont();
            if (_font != null)
                _fingerprint = CalculateFingerprint();
        }
        if (_font == null)
            return;
        List<TMP_FontAsset> fallbacks = TMP_Settings.fallbackFontAssets;
        if (fallbacks == null)
        {
            fallbacks = new List<TMP_FontAsset>();
            TMP_Settings.fallbackFontAssets = fallbacks;
        }
        if (!fallbacks.Contains(_font))
            fallbacks.Add(_font);
    }

    public bool ReloadIfChanged(out bool changed)
    {
        changed = false;
        string fingerprint = CalculateFingerprint();
        if (_font != null && string.Equals(_fingerprint, fingerprint, StringComparison.Ordinal))
        {
            EnsureLoaded();
            _log.LogDebug("中文字体文件和 [Font] 配置未变化，跳过字体重建。");
            return true;
        }

        TMP_FontAsset replacement = LoadFont();
        if (replacement == null)
        {
            _log.LogError("中文字体热重载失败，继续使用当前 fallback 字体。");
            EnsureLoaded();
            return false;
        }

        TMP_FontAsset previous = _font;
        List<TMP_FontAsset> fallbacks = TMP_Settings.fallbackFontAssets;
        if (fallbacks == null)
        {
            fallbacks = new List<TMP_FontAsset>();
            TMP_Settings.fallbackFontAssets = fallbacks;
        }
        int previousIndex = previous == null ? -1 : fallbacks.IndexOf(previous);
        if (previousIndex >= 0)
            fallbacks[previousIndex] = replacement;
        else if (!fallbacks.Contains(replacement))
            fallbacks.Add(replacement);
        if (previous != null)
        {
            while (fallbacks.Remove(previous))
            {
            }
        }
        _font = replacement;
        _fingerprint = CalculateFingerprint();
        changed = true;
        RefreshDirectBindings();
        if (previous != null && previous != replacement)
            UnityEngine.Object.Destroy(previous);
        _log.LogMessage("中文 fallback 字体已热重载。");
        return true;
    }

    public bool ApplyDirect(TMP_Text component, bool useChineseFont)
    {
        if (component == null)
            return false;
        EnsureLoaded();
        int id = component.GetInstanceID();
        if (!_directBindings.TryGetValue(id, out DirectBinding binding) ||
            binding.Component == null || !ReferenceEquals(binding.Component, component))
        {
            binding = new DirectBinding
            {
                Component = component,
                OriginalFont = component.font,
                OriginalMaterial = component.fontSharedMaterial,
                OriginalOverflowMode = component.overflowMode,
            };
            _directBindings[id] = binding;
        }
        binding.UseChineseFont = useChineseFont;
        if (useChineseFont)
        {
            if (_font == null)
                return false;
            component.font = _font;
            component.fontSharedMaterial = _font.material;
            component.overflowMode = TextOverflowModes.Overflow;
        }
        else
        {
            component.font = binding.OriginalFont;
            component.fontSharedMaterial = binding.OriginalMaterial;
            component.overflowMode = binding.OriginalOverflowMode;
        }
        return true;
    }

    public string RichTextFontName
    {
        get
        {
            EnsureLoaded();
            return _font == null ? string.Empty : _font.name;
        }
    }

    public string RichTextColorFor(TMP_Text component)
    {
        EnsureLoaded();
        if (component == null)
            return "FFFFFFFF";
        Color textColor = component.color;
        Color sourceFaceColor = ReadFaceColor(component.fontSharedMaterial);
        Color targetFaceColor = ReadFaceColor(_font == null ? null : _font.material);
        Color compensated = DialoguePunctuationColor.Compensate(textColor,
            sourceFaceColor, targetFaceColor);
        return ColorUtility.ToHtmlStringRGBA(compensated);
    }

    public void RefreshDirectBindings()
    {
        foreach (KeyValuePair<int, DirectBinding> item in
                 new List<KeyValuePair<int, DirectBinding>>(_directBindings))
        {
            DirectBinding binding = item.Value;
            if (binding.Component == null)
            {
                _directBindings.Remove(item.Key);
                continue;
            }
            ApplyDirect(binding.Component, binding.UseChineseFont);
        }
    }

    private string CalculateFingerprint()
    {
        string mode = (_config.FontSource ?? "Auto").Trim().ToLowerInvariant();
        string bundledPath = ResolvePath(Path.Combine(_contentRoot, _config.BundledFont ?? string.Empty));
        string customPath = string.IsNullOrWhiteSpace(_config.FontFile)
            ? string.Empty
            : ResolvePath(_config.FontFile);
        string selected = mode switch
        {
            "bundled" => FileFingerprint(bundledPath),
            "file" => FileFingerprint(customPath),
            "system" => "system:" + string.Join(";", _config.SystemFontCandidates ?? Array.Empty<string>()),
            "auto" when File.Exists(bundledPath) => FileFingerprint(bundledPath),
            "auto" when !string.IsNullOrEmpty(customPath) && File.Exists(customPath) => FileFingerprint(customPath),
            "auto" => "system:" + string.Join(";", _config.SystemFontCandidates ?? Array.Empty<string>()),
            _ => "invalid-mode",
        };
        string configuration = string.Join("\n", new[]
        {
            mode,
            bundledPath,
            customPath,
            string.Join(";", _config.SystemFontCandidates ?? Array.Empty<string>()),
            selected,
        });
        using var sha = SHA256.Create();
        return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(configuration)));
    }

    internal string CurrentFingerprintForTests() => CalculateFingerprint();

    private static string FileFingerprint(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return "missing:" + (path ?? string.Empty);
        try
        {
            using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var sha = SHA256.Create();
            return Path.GetFullPath(path) + ":" + Convert.ToBase64String(sha.ComputeHash(stream));
        }
        catch (Exception ex)
        {
            return Path.GetFullPath(path) + ":unreadable:" + ex.GetType().Name;
        }
    }

    private TMP_FontAsset LoadFont()
    {
        string mode = (_config.FontSource ?? "Auto").Trim().ToLowerInvariant();
        TMP_FontAsset result = null;
        if (mode is "auto" or "bundled")
        {
            string path = ResolvePath(Path.Combine(_contentRoot, _config.BundledFont ?? string.Empty));
            result = CreateFromFile(path);
            if (result != null || mode == "bundled")
                return FinalizeFont(result, path);
        }
        if ((mode is "auto" or "file") && !string.IsNullOrWhiteSpace(_config.FontFile))
        {
            string path = ResolvePath(_config.FontFile);
            result = CreateFromFile(path);
            if (result != null || mode == "file")
                return FinalizeFont(result, path);
        }
        if (mode is "auto" or "system")
        {
            foreach (string candidate in _config.SystemFontCandidates.Select(value => value.Trim()))
            {
                if (candidate.Length == 0)
                    continue;
                result = CreateFromSystem(candidate);
                if (result != null)
                    return FinalizeFont(result, "系统字体 " + candidate);
            }
        }
        _log.LogError("未能加载中文 fallback 字体，中文可能显示为方框。");
        return null;
    }

    private static Color ReadFaceColor(Material material) =>
        material != null && material.HasProperty("_FaceColor")
            ? material.GetColor("_FaceColor")
            : Color.white;

    private TMP_FontAsset CreateFromFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            _log.LogWarning($"字体文件不存在：{path}");
            return null;
        }
        try
        {
            MethodInfo method = typeof(TMP_FontAsset).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(candidate =>
                {
                    ParameterInfo[] parameters = candidate.GetParameters();
                    return candidate.Name == "CreateFontAsset" && parameters.Length == 7 &&
                           parameters[0].ParameterType == typeof(string) &&
                           parameters[1].ParameterType == typeof(int);
                });
            if (method == null)
                throw new MissingMethodException("TMP_FontAsset.CreateFontAsset(string, int, ...) not found");
            ParameterInfo[] p = method.GetParameters();
            object renderMode = Enum.Parse(p[4].ParameterType, "SDFAA");
            return (TMP_FontAsset)method.Invoke(null, new object[] { path, 0, 90, 9, renderMode, 1024, 1024 });
        }
        catch (Exception ex)
        {
            _log.LogError($"从字体文件创建 TMP fallback 失败：{path}\n{ex}");
            return null;
        }
    }

    private TMP_FontAsset CreateFromSystem(string family)
    {
        try
        {
            MethodInfo method = typeof(TMP_FontAsset).GetMethod("CreateFontAsset",
                BindingFlags.Public | BindingFlags.Static, null,
                new[] { typeof(string), typeof(string), typeof(int) }, null);
            return method == null ? null : (TMP_FontAsset)method.Invoke(null, new object[] { family, string.Empty, 90 });
        }
        catch (Exception ex)
        {
            _log.LogWarning($"系统字体不可用：{family} ({ex.GetBaseException().Message})");
            return null;
        }
    }

    private TMP_FontAsset FinalizeFont(TMP_FontAsset font, string source)
    {
        if (font == null)
            return null;
        font.name = "DeepSpaceChinese Fallback " + (++_fontGeneration).ToString();
        font.hashCode = unchecked((int)TMP_TextUtilities.GetHashCodeCaseInSensitive(font.name));
        MaterialReferenceManager.AddFontAsset(font);
        PropertyInfo multiAtlas = typeof(TMP_FontAsset).GetProperty("isMultiAtlasTexturesEnabled");
        if (multiAtlas?.CanWrite == true)
            multiAtlas.SetValue(font, true, null);
        UnityEngine.Object.DontDestroyOnLoad(font);
        _log.LogInfo($"已加载中文 fallback 字体：{source}");
        return font;
    }

    private static string ResolvePath(string path) =>
        Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(BepInEx.Paths.GameRootPath, path));
}
