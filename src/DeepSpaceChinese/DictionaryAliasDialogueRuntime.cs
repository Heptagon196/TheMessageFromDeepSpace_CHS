using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace DeepSpaceChinese;

/// <summary>
/// Creates independently logged dialogue chunks for Chinese aliases whose
/// wording cannot safely reuse the stock English-triggered translation.
/// </summary>
internal sealed class DictionaryAliasDialogueRuntime
{
    private static readonly FieldInfo AllDialoguesField =
        AccessTools.Field(typeof(DialogueBank), "allDialogues");
    private static readonly FieldInfo DialogueDictField =
        AccessTools.Field(typeof(DialogueBank), "dialogueDict");
    private static readonly FieldInfo DialogueReverseDictField =
        AccessTools.Field(typeof(DialogueBank), "dialogueReverseDict");
    private static readonly FieldInfo UniqueIdField =
        AccessTools.Field(typeof(DialogueChunk), "uniqueID");
    private static readonly FieldInfo FramesField =
        AccessTools.Field(typeof(DialogueChunk), "frames");
    private static readonly FieldInfo LogNameField =
        AccessTools.Field(typeof(DialogueChunk), "logName");

    private readonly PatchConfig _config;
    private readonly DialogueLocalizer _dialogue;
    private readonly DialogueFrameCatalog _frameCatalog;
    private readonly ManualLogSource _log;
    private readonly List<DialogueBank> _banks = new();
    private readonly Dictionary<DialogueBank, Dictionary<int, DialogueChunk>>
        _chunksByBank = new();
    private DictionaryTriggerAliasStore _store;
    private bool _renameActive;
    private int _renameTermId;
    private string _renameFrom = string.Empty;
    private string _renameTo = string.Empty;

    internal DictionaryAliasDialogueRuntime(PatchConfig config,
        DialogueLocalizer dialogue, DialogueFrameCatalog frameCatalog,
        DictionaryTriggerAliasStore store, ManualLogSource log)
    {
        _config = config;
        _dialogue = dialogue;
        _frameCatalog = frameCatalog;
        _store = store ?? DictionaryTriggerAliasStore.Empty;
        _log = log;
    }

    internal void ReplaceStore(DictionaryTriggerAliasStore store)
    {
        _store = store ?? DictionaryTriggerAliasStore.Empty;
        foreach (DialogueBank bank in _banks.Where(value => value != null).ToArray())
            PrepareBank(bank);
    }

    internal void PrepareBank(DialogueBank bank)
    {
        if (bank == null || _store.VariantCount == 0)
            return;
        if (!_banks.Contains(bank))
            _banks.Add(bank);
        if (!_chunksByBank.TryGetValue(bank, out Dictionary<int, DialogueChunk> chunks))
        {
            chunks = new Dictionary<int, DialogueChunk>();
            _chunksByBank.Add(bank, chunks);
        }

        DialogueChunk[] all = AllDialoguesField?.GetValue(bank) as DialogueChunk[] ??
                              Array.Empty<DialogueChunk>();
        var expanded = new List<DialogueChunk>(all);
        foreach (DictionaryTriggerAliasStore.DialogueVariant variant in
                 _store.DialogueVariants)
        {
            DialogueChunk existing = expanded.FirstOrDefault(chunk =>
                chunk != null && chunk.UniqueID == variant.SyntheticDialogueId);
            if (existing == null)
            {
                DialogueChunk source = expanded.FirstOrDefault(chunk =>
                    chunk != null && chunk.UniqueID == variant.DialogueId);
                if (source == null)
                {
                    _log?.LogError($"无法创建词典独立对白 {variant.SyntheticDialogueId}：" +
                                   $"找不到源对白 {variant.DialogueId}。");
                    continue;
                }
                existing = UnityEngine.Object.Instantiate(source);
                existing.name = source.name + $" [中文别名 {variant.SyntheticDialogueId}]";
                if (_dialogue.TryGetOriginalChunk(source, out DialogueFrame[] sourceFrames,
                        out string sourceLogName, out string sourceProcessedRaw))
                {
                    FramesField?.SetValue(existing, sourceFrames);
                    LogNameField?.SetValue(existing, sourceLogName);
                    existing.processedRaw = sourceProcessedRaw;
                }
                UniqueIdField?.SetValue(existing, variant.SyntheticDialogueId);
                expanded.Add(existing);
            }
            chunks[variant.SyntheticDialogueId] = existing;
            RegisterWithConfiguredBank(bank, existing);
        }
        if (expanded.Count != all.Length)
            AllDialoguesField?.SetValue(bank, expanded.ToArray());
    }

    private static void RegisterWithConfiguredBank(DialogueBank bank, DialogueChunk chunk)
    {
        var forward = DialogueDictField?.GetValue(bank) as Dictionary<DialogueChunk, int>;
        var reverse = DialogueReverseDictField?.GetValue(bank) as Dictionary<int, DialogueChunk>;
        if (forward == null || reverse == null || chunk == null)
            return;
        if (!forward.ContainsKey(chunk))
            forward.Add(chunk, chunk.UniqueID);
        if (!reverse.ContainsKey(chunk.UniqueID))
            reverse.Add(chunk.UniqueID, chunk);
    }

    internal void ApplyBank(DialogueBank bank)
    {
        if (bank == null || !_chunksByBank.TryGetValue(bank,
                out Dictionary<int, DialogueChunk> chunks))
            return;
        foreach (DictionaryTriggerAliasStore.DialogueVariant variant in
                 _store.DialogueVariants)
        {
            if (!chunks.TryGetValue(variant.SyntheticDialogueId, out DialogueChunk chunk))
                continue;
            ApplyVariantChunk(chunk, variant);
        }
    }

    internal void ReapplyAll()
    {
        _banks.RemoveAll(bank => bank == null);
        foreach (DialogueBank bank in _banks)
            ApplyBank(bank);
    }

    internal void BeginRename(int termId, string fromName, string toName)
    {
        _renameActive = true;
        _renameTermId = termId;
        _renameFrom = fromName ?? string.Empty;
        _renameTo = toName ?? string.Empty;
    }

    internal void CancelRename()
    {
        _renameActive = false;
        _renameFrom = string.Empty;
        _renameTo = string.Empty;
    }

    internal void CompleteRename(DialogueManager manager)
    {
        try
        {
            if (_config?.Enabled != true ||
                !TrySelectIndependentVariant(_store, _renameActive,
                    _renameTermId, _renameFrom, _renameTo,
                    out DictionaryTriggerAliasStore.DialogueVariant variant))
                return;

            DialogueBank bank = _dialogue.CurrentBank;
            if (manager == null || bank == null || !_chunksByBank.TryGetValue(bank,
                    out Dictionary<int, DialogueChunk> chunks) ||
                !chunks.TryGetValue(variant.SyntheticDialogueId,
                    out DialogueChunk replacement))
            {
                _log?.LogError($"词典输入“{_renameTo}”已命中独立对白，" +
                               $"但合成对白 {variant.SyntheticDialogueId} 尚未注册。");
                return;
            }

            // The clone may have been restored to its inherited English
            // frames after bank initialization.  Reapply the localized
            // variant at the last possible moment before independent play.
            if (!ApplyVariantChunk(replacement, variant))
                return;
            manager.PlayDialogueChunk(replacement);
        }
        finally
        {
            CancelRename();
        }
    }

    internal static bool TrySelectIndependentVariant(DictionaryTriggerAliasStore store,
        bool renameActive, int termId, string fromName, string toName,
        out DictionaryTriggerAliasStore.DialogueVariant variant)
    {
        variant = null;
        return renameActive && store != null &&
               store.TryGetDialogueVariant(termId, fromName, toName, out variant);
    }

    private bool ApplyVariantChunk(DialogueChunk chunk,
        DictionaryTriggerAliasStore.DialogueVariant variant)
    {
        DialogueFrame[] originalFrames;
        string originalLogName;
        if (!_dialogue.TryGetOriginalChunk(chunk, out originalFrames,
                out originalLogName, out _))
        {
            originalFrames = CloneFrames(chunk.Frames);
            originalLogName = chunk.LogName;
        }
        if (!TryBuildTranslatedFrames(originalFrames, variant,
                _dialogue.PlayerFullName(DisplayMode.TranslationOnly),
                out DialogueFrame[] translatedFrames, out string error))
        {
            _log?.LogError($"无法应用词典独立对白 {variant.SyntheticDialogueId}：{error}");
            return false;
        }
        for (int index = 0; index < originalFrames.Length; index++)
        {
            _frameCatalog.RegisterTranslationAlias(originalFrames[index],
                translatedFrames[index],
                $"dictionary-dialogue-variant:{variant.SyntheticDialogueId}/frame:{index}");
        }
        FramesField?.SetValue(chunk, CloneFrames(translatedFrames));
        bool translated = _config.DisplayMode == DisplayMode.TranslationOnly;
        LogNameField?.SetValue(chunk, translated
            ? variant.TranslatedTitle
            : originalLogName);
        chunk.processedRaw = DialogueLocalizer.BuildProcessedRaw(
            translated ? translatedFrames : originalFrames,
            translated ? DisplayMode.TranslationOnly : DisplayMode.OriginalOnly);
        return true;
    }

    internal static bool TryBuildTranslatedFrames(DialogueFrame[] originals,
        DictionaryTriggerAliasStore.DialogueVariant variant, string playerFullName,
        out DialogueFrame[] translated, out string error)
    {
        translated = null;
        error = string.Empty;
        if (originals == null || variant?.Frames == null ||
            variant.Frames.Count != originals.Length)
        {
            error = "变体 frame 数量与源对白不一致。";
            return false;
        }
        var byIndex = new Dictionary<int, DictionaryTriggerAliasStore.DialogueVariantFrame>();
        foreach (DictionaryTriggerAliasStore.DialogueVariantFrame frame in variant.Frames)
        {
            if (frame == null || frame.FrameIndex < 0 ||
                frame.FrameIndex >= originals.Length ||
                byIndex.ContainsKey(frame.FrameIndex))
            {
                error = "变体 frame_index 无效或重复。";
                return false;
            }
            byIndex.Add(frame.FrameIndex, frame);
        }

        translated = CloneFrames(originals);
        for (int frameIndex = 0; frameIndex < originals.Length; frameIndex++)
        {
            DialoguePart[] originalParts = originals[frameIndex].dialogueParts ??
                                           Array.Empty<DialoguePart>();
            if (!byIndex.TryGetValue(frameIndex, out var variantFrame) ||
                !TokenCodec.TrySplitFrameTranslation(variantFrame.TranslatedText,
                    originalParts.Length, playerFullName, out string[] translatedParts))
            {
                error = $"frame {frameIndex} 无法按 PART 标记拆分。";
                translated = null;
                return false;
            }
            string[] displayParts = TokenCodec.ApplyTranslatedWhitespace(
                originalParts.Select(part => part.txt).ToArray(), translatedParts);
            for (int partIndex = 0; partIndex < displayParts.Length; partIndex++)
                translated[frameIndex].dialogueParts[partIndex].txt = displayParts[partIndex];
        }
        return true;
    }

    private static DialogueFrame[] CloneFrames(DialogueFrame[] source)
    {
        if (source == null)
            return Array.Empty<DialogueFrame>();
        var result = new DialogueFrame[source.Length];
        for (int index = 0; index < source.Length; index++)
        {
            result[index] = source[index];
            DialoguePart[] parts = source[index].dialogueParts;
            result[index].dialogueParts = parts == null
                ? Array.Empty<DialoguePart>()
                : (DialoguePart[])parts.Clone();
        }
        return result;
    }
}
