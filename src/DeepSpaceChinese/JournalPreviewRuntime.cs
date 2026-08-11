using System;
using System.Linq;
using TMPro;
using UnityEngine;

namespace DeepSpaceChinese;

internal static class JournalPreviewId
{
    public static bool TryNormalize(string input, out string stableKey)
    {
        stableKey = string.Empty;
        string value = (input ?? string.Empty).Trim();
        if (string.Equals(value, "hypotheses", StringComparison.OrdinalIgnoreCase))
        {
            stableKey = "hypotheses";
            return true;
        }
        const string prefix = "dialogue:";
        const string middle = "/frame:";
        if (!value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;
        int separator = value.IndexOf(middle, prefix.Length,
            StringComparison.OrdinalIgnoreCase);
        if (separator < 0 ||
            !int.TryParse(value.Substring(prefix.Length, separator - prefix.Length),
                out int dialogueId) || dialogueId < 0 ||
            !int.TryParse(value.Substring(separator + middle.Length), out int frameId) ||
            frameId < 0)
            return false;
        stableKey = $"dialogue:{dialogueId}/frame:{frameId}";
        return true;
    }
}

internal enum JournalPreviewPromptAction
{
    None,
    Submit,
    Cancel,
}

internal static class JournalPreviewPromptInput
{
    public static JournalPreviewPromptAction Resolve(bool isKeyDown, KeyCode keyCode)
    {
        if (!isKeyDown)
            return JournalPreviewPromptAction.None;
        if (keyCode == KeyCode.Return || keyCode == KeyCode.KeypadEnter)
            return JournalPreviewPromptAction.Submit;
        return keyCode == KeyCode.Escape
            ? JournalPreviewPromptAction.Cancel
            : JournalPreviewPromptAction.None;
    }
}

internal sealed class JournalPreviewRuntime
{
    private readonly DeepSpaceChinesePlugin _plugin;
    private readonly DialogueFrameCatalog _catalog;
    private bool _promptOpen;
    private bool _focusInput;
    private bool _previewActive;
    private bool _canvasWasActive;
    private bool _continueWasActive;
    private bool _hypothesesPreviewActive;
    private string _input = "dialogue:55/frame:3";
    private string _message = string.Empty;
    private ProgressLog _progressLog;
    private Coroutine _hypothesesRoutine;

    public JournalPreviewRuntime(DeepSpaceChinesePlugin plugin,
        DialogueFrameCatalog catalog)
    {
        _plugin = plugin;
        _catalog = catalog;
    }

    public void Toggle()
    {
        if (_previewActive)
        {
            ClosePreview();
            return;
        }
        _promptOpen = !_promptOpen;
        _focusInput = _promptOpen;
        _message = string.Empty;
    }

    public void Dispose()
    {
        _promptOpen = false;
        if (_previewActive)
            ClosePreview();
    }

    public void DrawGui()
    {
        if (!_promptOpen)
            return;
        const float width = 520f;
        const float height = 132f;
        var rect = new Rect((Screen.width - width) * 0.5f,
            (Screen.height - height) * 0.5f, width, height);

        JournalPreviewPromptAction action = JournalPreviewPromptInput.Resolve(
            Event.current.type == EventType.KeyDown, Event.current.keyCode);
        if (action == JournalPreviewPromptAction.Submit)
        {
            Event.current.Use();
            Show(_input);
            return;
        }
        if (action == JournalPreviewPromptAction.Cancel)
        {
            Event.current.Use();
            _promptOpen = false;
            return;
        }

        GUI.Box(rect, "Journal preview (F6)");
        GUI.Label(new Rect(rect.x + 18f, rect.y + 34f, width - 36f, 24f),
            "Stable ID, e.g. dialogue:55/frame:3 or hypotheses");
        GUI.SetNextControlName("JournalPreviewId");
        _input = GUI.TextField(new Rect(rect.x + 18f, rect.y + 60f,
            width - 36f, 28f), _input ?? string.Empty);
        if (_focusInput)
        {
            GUI.FocusControl("JournalPreviewId");
            _focusInput = false;
        }
        GUI.Label(new Rect(rect.x + 18f, rect.y + 96f, 190f, 24f),
            "Enter: show    Esc: cancel");
        if (!string.IsNullOrEmpty(_message))
            GUI.Label(new Rect(rect.x + 220f, rect.y + 96f, width - 238f, 24f), _message);
    }

    private void Show(string input)
    {
        if (!JournalPreviewId.TryNormalize(input, out string stableKey))
        {
            _message = "Invalid ID";
            return;
        }
        if (stableKey == "hypotheses")
        {
            ShowHypotheses(stableKey);
            return;
        }
        if (!_catalog.TryGet(stableKey, out DialogueFramePair pair))
        {
            _message = "ID not found";
            return;
        }
        _progressLog = Resources.FindObjectsOfTypeAll<ProgressLog>()
            .FirstOrDefault(item => item != null && item.gameObject.scene.IsValid());
        if (_progressLog == null)
        {
            _message = "ProgressLog not found";
            return;
        }
        if (!TrySelectLabels(_progressLog, pair.Original.speaker,
                out TMP_Text title, out TMP_Text body, out string titleSource))
        {
            _message = "Not a character journal frame";
            return;
        }

        _canvasWasActive = _progressLog.progressLogCanvas.activeSelf;
        _continueWasActive = _progressLog.continueButton != null &&
                             _progressLog.continueButton.activeSelf;
        _progressLog.OpenLogBG();
        _progressLog.progressParent.SetActive(false);
        _progressLog.translatorLogGroup.SetActive(false);
        _progressLog.actSequence.SetActive(false);
        if (_progressLog.hypothesesLog != null)
            _progressLog.hypothesesLog.gameObject.SetActive(false);
        if (_progressLog.continueButton != null)
            _progressLog.continueButton.SetActive(true);
        ClearJournalLabels(_progressLog);

        DialogueFrame display = _plugin.PrepareCharacterTypedDialogue(body, pair.Original);
        string text = DialogueManager.ReplaceSignalEmbeds(display.GetMergedDialogueString());
        text = DialogueManager.ReplaceTranslator(text);
        body.text = text;
        body.maxVisibleCharacters = int.MaxValue;
        title.text = _plugin.PrepareGenericTypedText(title, titleSource);
        title.maxVisibleCharacters = int.MaxValue;
        _input = stableKey;
        _promptOpen = false;
        _previewActive = true;
        _plugin.PluginLog.LogMessage($"F6 日志预览已打开：{stableKey}；再次按 F6 关闭。");
    }

    private void ShowHypotheses(string stableKey)
    {
        _progressLog = Resources.FindObjectsOfTypeAll<ProgressLog>()
            .FirstOrDefault(item => item != null && item.gameObject.scene.IsValid());
        DictionaryHypothesesLog hypotheses = _progressLog?.hypothesesLog;
        if (_progressLog == null || hypotheses == null || hypotheses.group == null ||
            hypotheses.haveUnlockedLabel == null || hypotheses.viewInDictLabel == null ||
            hypotheses.dictionaryHypotheses == null)
        {
            _message = "Hypotheses Log not found";
            return;
        }

        _canvasWasActive = _progressLog.progressLogCanvas.activeSelf;
        _continueWasActive = _progressLog.continueButton != null &&
                             _progressLog.continueButton.activeSelf;
        _progressLog.OpenLogBG();
        _progressLog.progressParent.SetActive(false);
        _progressLog.translatorLogGroup.SetActive(false);
        _progressLog.actSequence.SetActive(false);
        ClearJournalLabels(_progressLog);
        hypotheses.Close();
        hypotheses.group.SetActive(true);
        hypotheses.haveUnlockedLabel.text = _plugin.PrepareGenericTypedText(
            hypotheses.haveUnlockedLabel, hypotheses.haveUnlocked_s);
        hypotheses.haveUnlockedLabel.maxVisibleCharacters = int.MaxValue;
        hypotheses.viewInDictLabel.text = _plugin.PrepareGenericTypedText(
            hypotheses.viewInDictLabel, hypotheses.viewInDict_s);
        hypotheses.viewInDictLabel.maxVisibleCharacters = int.MaxValue;
        DictionaryHypotheses.TermCluster cluster =
            hypotheses.dictionaryHypotheses.TermClusterExceeded;
        if (cluster.terms != null && cluster.terms.Length != 0)
            _hypothesesRoutine = _plugin.StartCoroutine(hypotheses.SpawnAllWords(cluster));
        if (_progressLog.continueButton != null)
            _progressLog.continueButton.SetActive(true);

        _input = stableKey;
        _promptOpen = false;
        _previewActive = true;
        _hypothesesPreviewActive = true;
        _plugin.PluginLog.LogMessage("F6 假说说明页预览已打开；再次按 F6 关闭。");
    }

    private void ClosePreview()
    {
        if (_progressLog != null)
        {
            if (_hypothesesRoutine != null)
            {
                _plugin.StopCoroutine(_hypothesesRoutine);
                _hypothesesRoutine = null;
            }
            if (_hypothesesPreviewActive && _progressLog.hypothesesLog != null)
                _progressLog.hypothesesLog.Close();
            ClearJournalLabels(_progressLog);
            if (_progressLog.continueButton != null)
                _progressLog.continueButton.SetActive(_continueWasActive);
            if (!_canvasWasActive)
            {
                _progressLog.starSystem.Stop();
                _progressLog.progressLogCanvas.SetActive(false);
            }
        }
        _previewActive = false;
        _hypothesesPreviewActive = false;
        _progressLog = null;
    }

    private static bool TrySelectLabels(ProgressLog log, Speaker speaker,
        out TMP_Text title, out TMP_Text body, out string titleSource)
    {
        title = null;
        body = null;
        titleSource = string.Empty;
        switch (speaker)
        {
            case Speaker.Alan:
                title = log.aLogTitle; body = log.aLog; titleSource = log.aLogTitle_s; break;
            case Speaker.BScientist:
                title = log.bLogTitle; body = log.bLog; titleSource = log.bLogTitle_s; break;
            case Speaker.Carrie:
                title = log.cLogTitle; body = log.cLog; titleSource = log.cLogTitle_s; break;
            case Speaker.Doppler:
                title = log.dLogTitle; body = log.dLog; titleSource = log.dLogTitle_s; break;
            default:
                return false;
        }
        return title != null && body != null;
    }

    private static void ClearJournalLabels(ProgressLog log)
    {
        TMP_Text[] labels =
        {
            log.aLogTitle, log.aLog, log.bLogTitle, log.bLog,
            log.cLogTitle, log.cLog, log.dLogTitle, log.dLog,
            log.tLogTitle, log.tLogArea,
        };
        foreach (TMP_Text label in labels)
        {
            if (label == null)
                continue;
            label.text = string.Empty;
            label.maxVisibleCharacters = int.MaxValue;
        }
    }
}
