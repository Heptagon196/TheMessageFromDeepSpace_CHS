using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using BepInEx.Logging;
using HarmonyLib;

namespace DeepSpaceChinese;

internal sealed class DialogueLocalizer
{
    private sealed class OriginalChunk
    {
        public string LogName;
        public string ProcessedRaw;
        public DialogueFrame[] Frames;
    }

    private static readonly FieldInfo AllDialoguesField = AccessTools.Field(typeof(DialogueBank), "allDialogues");
    private static readonly FieldInfo FramesField = AccessTools.Field(typeof(DialogueChunk), "frames");
    private static readonly FieldInfo LogNameField = AccessTools.Field(typeof(DialogueChunk), "logName");

    private TranslationStore _store;
    private readonly PatchConfig _config;
    private readonly DialogueFrameCatalog _frameCatalog;
    private readonly ManualLogSource _log;
    private readonly Dictionary<int, OriginalChunk> _originals = new();
    private readonly List<DialogueBank> _banks = new();
    private readonly HashSet<string> _reportedMismatches = new(StringComparer.Ordinal);

    public DialogueBank CurrentBank => _banks.LastOrDefault(bank => bank != null);

    public DialogueLocalizer(TranslationStore store, PatchConfig config,
        DialogueFrameCatalog frameCatalog, ManualLogSource log)
    {
        _store = store;
        _config = config;
        _frameCatalog = frameCatalog;
        _log = log;
    }

    public void RegisterAndApply(DialogueBank bank)
    {
        if (bank == null)
            return;
        if (!_banks.Contains(bank))
            _banks.Add(bank);
        ApplyBank(bank);
    }

    public void ReapplyAll()
    {
        _banks.RemoveAll(bank => bank == null);
        foreach (DialogueBank bank in _banks)
            ApplyBank(bank);
    }

    public void ReplaceStore(TranslationStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _frameCatalog.Clear();
        _reportedMismatches.Clear();
    }

    public string PlayerFullName()
    {
        return PlayerFullName(_config.DisplayMode);
    }

    public string PlayerFullName(DisplayMode mode)
    {
        DialogueBank bank = CurrentBank;
        return PlayerNameRuntime.FormatFullName(bank?.TranslatorName, bank?.TranslatorFullName,
            mode);
    }

    internal string ResolveLogTitle(DialogueChunk chunk)
    {
        if (chunk == null)
            return string.Empty;
        string originalTitle = _originals.TryGetValue(chunk.UniqueID, out OriginalChunk original)
            ? original.LogName
            : chunk.LogName;
        string titleKey = $"dialogue:{chunk.UniqueID}/title";
        if (!_store.TryGet(titleKey, out RuntimeTranslationEntry entry))
            return originalTitle;
        string source = TokenCodec.ProtectRuntimeTokens(originalTitle);
        if (!SourceMatches(entry, source))
            return originalTitle;
        return ResolveLogTitleForTests(entry, originalTitle, _config, PlayerFullName());
    }

    internal static string ResolveLogTitleForTests(RuntimeTranslationEntry entry,
        string originalTitle, PatchConfig config, string playerFullName)
    {
        if (entry == null || config == null || !config.Enabled || !config.TranslateLogs ||
            config.DisplayMode != DisplayMode.TranslationOnly)
            return originalTitle;
        string source = TokenCodec.ProtectRuntimeTokens(originalTitle ?? string.Empty);
        if (TokenCodec.Sha256(source) != entry.SourceSha256)
            return originalTitle;
        return TokenCodec.FormatDisplay(entry.TranslatedText, originalTitle, config,
            playerFullName);
    }

    private void ApplyBank(DialogueBank bank)
    {
        if (!_config.Enabled)
            return;
        var chunks = (DialogueChunk[])AllDialoguesField?.GetValue(bank);
        if (chunks == null)
        {
            _log.LogError("无法读取 DialogueBank.allDialogues，未应用对白翻译。");
            return;
        }
        int translatedFrames = 0;
        int translatedTitles = 0;
        foreach (DialogueChunk chunk in chunks.Where(chunk => chunk != null))
        {
            if (!_originals.TryGetValue(chunk.UniqueID, out OriginalChunk original))
            {
                original = new OriginalChunk
                {
                    LogName = chunk.LogName,
                    ProcessedRaw = chunk.processedRaw,
                    Frames = CloneFrames(chunk.Frames),
                };
                _originals.Add(chunk.UniqueID, original);
            }
            FramesField.SetValue(chunk, CloneFrames(original.Frames));
            LogNameField.SetValue(chunk, original.LogName);
            chunk.processedRaw = original.ProcessedRaw;

            DialogueFrame[] displayFrames = CloneFrames(original.Frames);
            bool anyFrameTranslated = false;
            string originalPlayerName = PlayerFullName(DisplayMode.OriginalOnly);
            string translatedPlayerName = PlayerFullName(DisplayMode.TranslationOnly);
            for (int frameIndex = 0; frameIndex < original.Frames.Length; frameIndex++)
            {
                string key = $"dialogue:{chunk.UniqueID}/frame:{frameIndex}";
                if (!_store.TryGet(key, out RuntimeTranslationEntry entry))
                {
                    if (!_config.FallbackToOriginal)
                        BlankFrame(displayFrames, frameIndex);
                    continue;
                }
                string source = TokenCodec.BuildFrameSource(original.Frames[frameIndex]);
                if (!SourceMatches(entry, source))
                    continue;
                DialoguePart[] originalParts = original.Frames[frameIndex].dialogueParts ?? Array.Empty<DialoguePart>();
                if (!TokenCodec.TrySplitFrameTranslation(entry.TranslatedText, originalParts.Length,
                        translatedPlayerName, out string[] translatedParts))
                {
                    ReportMismatch(key, "运行时无法按 PART 标记拆分译文");
                    continue;
                }
                DialogueFrame originalDisplayFrame = CloneFrame(original.Frames[frameIndex]);
                DialogueFrame translatedFrame = CloneFrame(original.Frames[frameIndex]);
                string[] translatedDisplayParts = TokenCodec.ApplyTranslatedWhitespace(
                    originalParts.Select(part => part.txt).ToArray(), translatedParts);
                for (int partIndex = 0; partIndex < originalParts.Length; partIndex++)
                {
                    string originalDisplay = TokenCodec.RestoreRuntimeTokens(
                        TokenCodec.ProtectRuntimeTokens(originalParts[partIndex].txt),
                        originalPlayerName);
                    originalDisplayFrame.dialogueParts[partIndex].txt =
                        TokenCodec.ApplyOriginalWhitespace(originalParts[partIndex].txt,
                            originalDisplay);
                    translatedFrame.dialogueParts[partIndex].txt = translatedDisplayParts[partIndex];
                }
                if (_config.TranslateDialogue)
                    _frameCatalog.Register(originalDisplayFrame, translatedFrame);
                displayFrames[frameIndex] = _config.DisplayMode == DisplayMode.TranslationOnly
                    ? translatedFrame
                    : originalDisplayFrame;
                anyFrameTranslated = true;
                translatedFrames++;
            }

            if (_config.TranslateDialogue)
                FramesField.SetValue(chunk, CloneFrames(displayFrames));
            if (_config.TranslateLogs && anyFrameTranslated)
                chunk.processedRaw = BuildProcessedRaw(displayFrames, _config.DisplayMode);

            string titleKey = $"dialogue:{chunk.UniqueID}/title";
            if (_config.TranslateLogs && _store.TryGet(titleKey, out RuntimeTranslationEntry titleEntry))
            {
                string source = TokenCodec.ProtectRuntimeTokens(original.LogName);
                if (SourceMatches(titleEntry, source))
                {
                    string title = ResolveLogTitleForTests(titleEntry, original.LogName, _config,
                        PlayerFullName());
                    LogNameField.SetValue(chunk, title);
                    translatedTitles++;
                }
            }
        }
        _log.LogInfo($"对白本地化已应用：{translatedFrames} 个 frame，{translatedTitles} 个日志标题，模式 {_config.DisplayMode}。");
    }

    private bool SourceMatches(RuntimeTranslationEntry entry, string source)
    {
        if (TokenCodec.Sha256(source) == entry.SourceSha256)
            return true;
        ReportMismatch(entry.StableKey, "源文哈希不匹配，可能是游戏版本发生变化");
        return false;
    }

    private void ReportMismatch(string key, string reason)
    {
        if (_reportedMismatches.Add(key))
            _log.LogError($"跳过译文 {key}：{reason}");
    }

    private static DialogueFrame[] CloneFrames(DialogueFrame[] source)
    {
        if (source == null)
            return Array.Empty<DialogueFrame>();
        var result = new DialogueFrame[source.Length];
        for (int frameIndex = 0; frameIndex < source.Length; frameIndex++)
        {
            result[frameIndex] = source[frameIndex];
            DialoguePart[] parts = source[frameIndex].dialogueParts;
            result[frameIndex].dialogueParts = parts == null ? Array.Empty<DialoguePart>() : (DialoguePart[])parts.Clone();
        }
        return result;
    }

    private static DialogueFrame CloneFrame(DialogueFrame source)
    {
        DialogueFrame result = source;
        DialoguePart[] parts = source.dialogueParts;
        result.dialogueParts = parts == null ? Array.Empty<DialoguePart>() :
            (DialoguePart[])parts.Clone();
        return result;
    }

    private static void BlankFrame(DialogueFrame[] frames, int frameIndex)
    {
        DialogueFrame frame = frames[frameIndex];
        for (int index = 0; index < frame.dialogueParts.Length; index++)
            frame.dialogueParts[index].txt = string.Empty;
        frames[frameIndex] = frame;
    }

    private static string BuildProcessedRaw(DialogueFrame[] frames, DisplayMode mode)
    {
        var output = new StringBuilder();
        for (int frameIndex = 0; frameIndex < frames.Length; frameIndex++)
        {
            if (frameIndex > 0)
                output.Append("\n\n");
            output.Append(LogSpeakerPrefix((int)frames[frameIndex].speaker, mode)).Append(": ");
            DialoguePart[] parts = frames[frameIndex].dialogueParts ?? Array.Empty<DialoguePart>();
            for (int partIndex = 0; partIndex < parts.Length; partIndex++)
            {
                if (partIndex > 0 && parts[partIndex].clearPrev && output.Length > 0 && output[output.Length - 1] != ' ')
                    output.Append(' ');
                output.Append(TokenCodec.RemoveAnimations(parts[partIndex].txt));
            }
        }
        return output.ToString();
    }

    internal static string LogSpeakerPrefix(int speakerValue, DisplayMode mode)
    {
        if (mode == DisplayMode.TranslationOnly)
        {
            return speakerValue switch
            {
                0 => "埃",
                1 => "巴",
                2 => "科",
                3 => "多",
                4 => "日志",
                5 => "驾",
                6 => "副",
                _ => "？",
            };
        }
        return speakerValue switch
        {
            0 => "A",
            1 => "B",
            2 => "C",
            3 => "D",
            4 => "L",
            5 => "P",
            6 => "Q",
            _ => "?",
        };
    }
}
