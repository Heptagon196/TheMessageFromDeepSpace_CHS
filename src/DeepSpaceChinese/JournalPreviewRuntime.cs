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

internal sealed class JournalPreviewRuntime
{
    private readonly DeepSpaceChinesePlugin _plugin;
    private readonly DialogueFrameCatalog _catalog;
    private bool _promptOpen;
    private bool _focusInput;
    private bool _previewActive;
    private bool _canvasWasActive;
    private string _input = "dialogue:55/frame:3";
    private string _message = string.Empty;
    private ProgressLog _progressLog;

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
        const float height = 150f;
        var rect = new Rect((Screen.width - width) * 0.5f,
            (Screen.height - height) * 0.5f, width, height);
        GUI.Box(rect, "Journal preview (F6)");
        GUI.Label(new Rect(rect.x + 18f, rect.y + 34f, width - 36f, 24f),
            "Stable ID, e.g. dialogue:55/frame:3");
        GUI.SetNextControlName("JournalPreviewId");
        _input = GUI.TextField(new Rect(rect.x + 18f, rect.y + 60f,
            width - 36f, 28f), _input ?? string.Empty);
        if (_focusInput)
        {
            GUI.FocusControl("JournalPreviewId");
            _focusInput = false;
        }
        bool submit = GUI.Button(new Rect(rect.x + 18f, rect.y + 100f, 110f, 30f),
                          "Show") ||
                      Event.current.type == EventType.KeyDown &&
                      (Event.current.keyCode == KeyCode.Return ||
                       Event.current.keyCode == KeyCode.KeypadEnter);
        if (GUI.Button(new Rect(rect.x + 138f, rect.y + 100f, 110f, 30f), "Cancel"))
            _promptOpen = false;
        if (!string.IsNullOrEmpty(_message))
            GUI.Label(new Rect(rect.x + 260f, rect.y + 103f, width - 278f, 30f), _message);
        if (submit)
        {
            Event.current.Use();
            Show(_input);
        }
    }

    private void Show(string input)
    {
        if (!JournalPreviewId.TryNormalize(input, out string stableKey))
        {
            _message = "Invalid ID";
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
        _progressLog.OpenLogBG();
        _progressLog.progressParent.SetActive(false);
        _progressLog.translatorLogGroup.SetActive(false);
        _progressLog.actSequence.SetActive(false);
        if (_progressLog.hypothesesLog != null)
            _progressLog.hypothesesLog.gameObject.SetActive(false);
        _progressLog.continueButton.SetActive(false);
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

    private void ClosePreview()
    {
        if (_progressLog != null)
        {
            ClearJournalLabels(_progressLog);
            if (!_canvasWasActive)
            {
                _progressLog.starSystem.Stop();
                _progressLog.progressLogCanvas.SetActive(false);
            }
        }
        _previewActive = false;
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
