using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace DeepSpaceChinese;

internal readonly struct DialogueReplayRequest
{
    public DialogueReplayRequest(int dialogueId, int? frameId)
    {
        DialogueId = dialogueId;
        FrameId = frameId;
    }

    public int DialogueId { get; }
    public int? FrameId { get; }
}

internal static class DialogueReplayId
{
    private const string Prefix = "play:";
    private const string FrameSeparator = "/frame:";

    public static bool TryNormalize(string input, out string stableKey)
    {
        stableKey = string.Empty;
        if (!TryParse(input, out DialogueReplayRequest request))
            return false;
        stableKey = request.FrameId.HasValue
            ? $"play:{request.DialogueId}/frame:{request.FrameId.Value}"
            : $"play:{request.DialogueId}";
        return true;
    }

    public static bool TryParse(string input, out DialogueReplayRequest request)
    {
        request = default;
        string value = (input ?? string.Empty).Trim();
        if (!value.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            return false;
        int separator = value.IndexOf(FrameSeparator, Prefix.Length,
            StringComparison.OrdinalIgnoreCase);
        string dialogueText = separator < 0
            ? value.Substring(Prefix.Length)
            : value.Substring(Prefix.Length, separator - Prefix.Length);
        if (!int.TryParse(dialogueText, out int dialogueId) || dialogueId < 0)
            return false;
        if (separator < 0)
        {
            request = new DialogueReplayRequest(dialogueId, null);
            return true;
        }
        if (!int.TryParse(value.Substring(separator + FrameSeparator.Length),
                out int frameId) || frameId < 0)
            return false;
        request = new DialogueReplayRequest(dialogueId, frameId);
        return true;
    }
}

internal static class JournalPreviewId
{
    public static bool TryNormalize(string input, out string stableKey)
    {
        stableKey = string.Empty;
        string value = (input ?? string.Empty).Trim();
        if (DialogueReplayId.TryNormalize(value, out stableKey))
            return true;
        if (string.Equals(value, "hypotheses", StringComparison.OrdinalIgnoreCase))
        {
            stableKey = "hypotheses";
            return true;
        }
        if (string.Equals(value, "credits", StringComparison.OrdinalIgnoreCase))
        {
            stableKey = "credits";
            return true;
        }
        if (string.Equals(value, "contact", StringComparison.OrdinalIgnoreCase))
        {
            stableKey = "contact";
            return true;
        }
        if (string.Equals(value, "report", StringComparison.OrdinalIgnoreCase))
        {
            stableKey = "report:1";
            return true;
        }
        const string reportPrefix = "report:";
        if (value.StartsWith(reportPrefix, StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(value.Substring(reportPrefix.Length), out int weekId) &&
            weekId >= 0)
        {
            stableKey = $"report:{weekId}";
            return true;
        }
        const string hypothesesPrefix = "hypotheses:";
        if (value.StartsWith(hypothesesPrefix, StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(value.Substring(hypothesesPrefix.Length), out int clusterIndex) &&
            clusterIndex >= 0)
        {
            stableKey = $"hypotheses:{clusterIndex}";
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
    private static readonly FieldInfo FramesField = typeof(DialogueChunk).GetField("frames",
        BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo TestChunkField = typeof(DialogueManager).GetField(
        "testChunk", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo QueueDebugDialogueField = typeof(DialogueManager).GetField(
        "queueDebugDialogue", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly MethodInfo ContactRoutineMethod = typeof(SpaceshipSequence).GetMethod(
        "ContactRoutine", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly MethodInfo TransmissionsEncounteredMethod =
        FindProgressLogMethod("TransmissionsEncountered");
    private static readonly MethodInfo SignalsEncounteredMethod =
        FindProgressLogMethod("SignalsEncountered");
    private static readonly MethodInfo TransmissionsEncounteredTotalMethod =
        FindProgressLogMethod("TransmissionsEncounteredTotal");
    private static readonly MethodInfo SignalsEncounteredTotalMethod =
        FindProgressLogMethod("SignalsEncounteredTotal");
    private static readonly MethodInfo BuildWordsNamedStringMethod =
        FindProgressLogMethod("BuildWordsNamedString");
    private static readonly MethodInfo BuildTransmissionGroupStringMethod =
        FindProgressLogMethod("BuildTransmissionGroupString");

    private readonly DeepSpaceChinesePlugin _plugin;
    private readonly DialogueFrameCatalog _catalog;
    private readonly DialogueLocalizer _dialogue;
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
    private Coroutine _creditsLaunchRoutine;
    private Coroutine _contactLaunchRoutine;
    private Coroutine _contactRoutine;
    private LeaveRoomNoMeeting _creditsTransition;
    private LeaveRoomNoMeeting _contactTransition;
    private DialogueManager _replayManager;
    private DialogueChunk _replayChunk;
    private CreditsSequence _creditsSequence;
    private SpaceshipSequence _contactSequence;

    public JournalPreviewRuntime(DeepSpaceChinesePlugin plugin,
        DialogueFrameCatalog catalog, DialogueLocalizer dialogue)
    {
        _plugin = plugin;
        _catalog = catalog;
        _dialogue = dialogue;
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
            "ID: report:1, play:801/frame:4, dialogue:55/frame:3, hypotheses:6, contact, or credits");
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
        if (DialogueReplayId.TryParse(stableKey, out DialogueReplayRequest replay))
        {
            ShowDialogueReplay(replay, stableKey);
            return;
        }
        if (stableKey.StartsWith("hypotheses", StringComparison.Ordinal))
        {
            ShowHypotheses(stableKey);
            return;
        }
        if (stableKey.StartsWith("report:", StringComparison.Ordinal))
        {
            ShowProgressReport(stableKey);
            return;
        }
        if (stableKey == "credits")
        {
            ShowCredits();
            return;
        }
        if (stableKey == "contact")
        {
            ShowContact();
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
        _plugin.ApplyProgressLogSpeakerColor(title, pair.Original.speaker);
        title.maxVisibleCharacters = int.MaxValue;
        _input = stableKey;
        _promptOpen = false;
        _previewActive = true;
        _plugin.PluginLog.LogMessage($"F6 日志预览已打开：{stableKey}；再次按 F6 关闭。");
    }

    private void ShowProgressReport(string stableKey)
    {
        if (!int.TryParse(stableKey.Substring("report:".Length), out int weekId))
        {
            _message = "Invalid report ID";
            return;
        }

        Meeting meeting = Resources.FindObjectsOfTypeAll<Meeting>()
            .FirstOrDefault(item => item != null && item.gameObject.scene.IsValid() &&
                                    item.progressLogData != null &&
                                    item.progressLogData.weekID == weekId) ??
            Resources.FindObjectsOfTypeAll<Meeting>()
                .FirstOrDefault(item => item != null && item.progressLogData != null &&
                                        item.progressLogData.weekID == weekId);
        ProgressLogData data = meeting?.progressLogData;
        _progressLog = Resources.FindObjectsOfTypeAll<ProgressLog>()
            .FirstOrDefault(item => item != null && item.gameObject.scene.IsValid());
        if (_progressLog == null || data == null)
        {
            _message = data == null ? "Weekly report not found" : "ProgressLog not found";
            _progressLog = null;
            return;
        }
        if (_progressLog.progress == null || _progressLog.progress.Length < 4 ||
            _progressLog.weekLabel == null || _progressLog.wordsNamedThisWeek == null ||
            _progressLog.transmissionGroupProgress == null)
        {
            _message = "ProgressLog labels not found";
            _progressLog = null;
            return;
        }

        try
        {
            _canvasWasActive = _progressLog.progressLogCanvas.activeSelf;
            _continueWasActive = _progressLog.continueButton != null &&
                                 _progressLog.continueButton.activeSelf;
            _progressLog.OpenLogBG();
            _progressLog.progressParent.SetActive(true);
            _progressLog.translatorLogGroup.SetActive(false);
            _progressLog.actSequence.SetActive(false);
            if (_progressLog.hypothesesLog != null)
                _progressLog.hypothesesLog.gameObject.SetActive(false);
            if (_progressLog.continueButton != null)
                _progressLog.continueButton.SetActive(true);
            ClearJournalLabels(_progressLog);

            string weekNumber = weekId >= 0 && weekId < _progressLog.numberStrings.Length
                ? _progressLog.numberStrings[weekId]
                : weekId.ToString();
            SetReportLabel(_progressLog.weekLabel, _progressLog.week_s + weekNumber);
            SetReportLabel(_progressLog.progress[0], InvokeProgressLine(
                TransmissionsEncounteredMethod, _progressLog, data));
            SetReportLabel(_progressLog.progress[1], InvokeProgressLine(
                SignalsEncounteredMethod, _progressLog, data));
            SetReportLabel(_progressLog.progress[2], InvokeProgressLine(
                TransmissionsEncounteredTotalMethod, _progressLog, data));
            SetReportLabel(_progressLog.progress[3], InvokeProgressLine(
                SignalsEncounteredTotalMethod, _progressLog, data));
            SetReportLabel(_progressLog.wordsNamedThisWeek,
                InvokeProgressText(BuildWordsNamedStringMethod, _progressLog, data));

            string groups = InvokeProgressText(BuildTransmissionGroupStringMethod,
                _progressLog, data);
            string[] names = (data.listsCompleted ?? Array.Empty<PuzzleList>())
                .Select(item => item?.PuzzleGroupName ?? string.Empty).ToArray();
            groups = _plugin.LocalizeCompletedPuzzleGroups(groups, names);
            SetReportLabel(_progressLog.transmissionGroupProgress, groups);

            _input = stableKey;
            _promptOpen = false;
            _previewActive = true;
            _plugin.PluginLog.LogMessage(
                $"F6 周报预览已打开：{stableKey}；再次按 F6 关闭。");
        }
        catch (Exception ex)
        {
            _message = "Unable to build weekly report";
            _plugin.PluginLog.LogError($"F6 周报预览失败：\n{ex}");
            if (!_canvasWasActive && _progressLog.progressLogCanvas != null)
                _progressLog.progressLogCanvas.SetActive(false);
            _progressLog = null;
        }
    }

    private void SetReportLabel(TMP_Text label, string source)
    {
        label.maxVisibleCharacters = int.MaxValue;
        label.text = _plugin.PrepareGenericTypedText(label, source ?? string.Empty);
    }

    private static string InvokeProgressLine(MethodInfo method, ProgressLog log,
        ProgressLogData data)
    {
        if (method == null)
            return string.Empty;
        object[] arguments = { data, 0, 0 };
        return method.Invoke(log, arguments) as string ?? string.Empty;
    }

    private static string InvokeProgressText(MethodInfo method, ProgressLog log,
        ProgressLogData data) => method?.Invoke(log, new object[] { data }) as string ??
                                 string.Empty;

    private static MethodInfo FindProgressLogMethod(string name) =>
        typeof(ProgressLog).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);

    private void ShowDialogueReplay(DialogueReplayRequest request, string stableKey)
    {
        if (_dialogue == null || !_dialogue.TryGetChunk(request.DialogueId,
                out DialogueChunk source))
        {
            _message = "Dialogue chunk not found";
            return;
        }
        DialogueFrame[] frames = source.Frames ?? Array.Empty<DialogueFrame>();
        if (request.FrameId.HasValue && request.FrameId.Value >= frames.Length)
        {
            _message = "Frame not found";
            return;
        }
        DialogueManager manager = DialogueManager.Instance ??
            Resources.FindObjectsOfTypeAll<DialogueManager>()
                .FirstOrDefault(item => item != null && item.gameObject.scene.IsValid());
        if (manager == null || FramesField == null || TestChunkField == null ||
            QueueDebugDialogueField == null)
        {
            _message = "DialogueManager test entry not found";
            return;
        }

        DialogueChunk replayChunk = UnityEngine.Object.Instantiate(source);
        replayChunk.name = source.name + " [F6 Replay]";
        if (request.FrameId.HasValue)
            FramesField.SetValue(replayChunk,
                new[] { frames[request.FrameId.Value] });

        manager.QuitCurrent();
        object previousTestChunk = TestChunkField.GetValue(manager);
        object previousQueueMode = QueueDebugDialogueField.GetValue(manager);
        TestChunkField.SetValue(manager, replayChunk);
        QueueDebugDialogueField.SetValue(manager, false);
        _replayManager = manager;
        _replayChunk = replayChunk;
        DialogueReplayRuntime.Register(replayChunk.GetInstanceID());
        try
        {
            manager.OnDialogueComplete += OnReplayComplete;
            manager.PlayTestChunk();
        }
        catch
        {
            ReleaseReplay(quitCurrent: false);
            throw;
        }
        finally
        {
            TestChunkField.SetValue(manager, previousTestChunk);
            QueueDebugDialogueField.SetValue(manager, previousQueueMode);
        }
        _input = stableKey;
        _promptOpen = false;
        _previewActive = true;
        _plugin.PluginLog.LogMessage(
            $"F6 真实对白重播已开始：{stableKey}；再次按 F6 可中止。");
    }

    private void OnReplayComplete(DialoguePlayInfo playInfo)
    {
        if (_replayChunk == null || playInfo.dc != _replayChunk)
            return;
        ReleaseReplay(quitCurrent: false);
        _previewActive = false;
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
        hypotheses.haveUnlockedLabel.richText = true;
        hypotheses.haveUnlockedLabel.textWrappingMode = TextWrappingModes.Normal;
        hypotheses.haveUnlockedLabel.overflowMode = TextOverflowModes.Overflow;
        hypotheses.haveUnlockedLabel.maxVisibleLines = int.MaxValue;
        hypotheses.haveUnlockedLabel.maxVisibleCharacters = int.MaxValue;
        hypotheses.viewInDictLabel.richText = true;
        hypotheses.viewInDictLabel.textWrappingMode = TextWrappingModes.Normal;
        hypotheses.viewInDictLabel.overflowMode = TextOverflowModes.Overflow;
        hypotheses.viewInDictLabel.maxVisibleLines = int.MaxValue;
        hypotheses.viewInDictLabel.maxVisibleCharacters = int.MaxValue;
        hypotheses.haveUnlockedLabel.text = _plugin.PrepareGenericTypedText(
            hypotheses.haveUnlockedLabel, hypotheses.haveUnlocked_s);
        hypotheses.viewInDictLabel.text = _plugin.PrepareGenericTypedText(
            hypotheses.viewInDictLabel, hypotheses.viewInDict_s);
        if (!TrySelectHypothesesCluster(hypotheses.dictionaryHypotheses, stableKey,
                out DictionaryHypotheses.TermCluster cluster, out int clusterIndex))
        {
            hypotheses.Close();
            _message = "Hypotheses cluster not found";
            return;
        }
        if (cluster.terms != null && cluster.terms.Length != 0)
            _hypothesesRoutine = _plugin.StartCoroutine(hypotheses.SpawnAllWords(cluster));
        if (_progressLog.continueButton != null)
            _progressLog.continueButton.SetActive(true);

        _input = stableKey;
        _promptOpen = false;
        _previewActive = true;
        _hypothesesPreviewActive = true;
        _plugin.PluginLog.LogMessage(
            $"F6 假说说明页预览已打开：词群 {clusterIndex}；再次按 F6 关闭。");
    }

    private void ShowCredits()
    {
        CreditsSequence credits = FindSceneObject<CreditsSequence>();
        if (credits != null)
        {
            PlayCredits(credits);
            return;
        }

        GameEnd gameEnd = FindSceneObject<GameEnd>();
        if (gameEnd == null)
        {
            _message = "Credits entry not found";
            return;
        }

        gameEnd.ForceStartEndSequence();
        _creditsTransition = gameEnd.manualSpaceshipEnter;
        _input = "credits";
        _promptOpen = false;
        _previewActive = true;
        _creditsLaunchRoutine = _plugin.StartCoroutine(WaitForCreditsScene());
        _plugin.PluginLog.LogMessage(
            "F6 正在进入结局场景；场景就绪后将直接播放滚动字幕。再次按 F6 可中止。");
    }

    private void ShowContact()
    {
        SpaceshipSequence spaceship = FindSceneObject<SpaceshipSequence>();
        if (spaceship != null)
        {
            PlayContact(spaceship);
            return;
        }

        GameEnd gameEnd = FindSceneObject<GameEnd>();
        if (gameEnd == null)
        {
            _message = "Final contact entry not found";
            return;
        }

        gameEnd.ForceStartEndSequence();
        _contactTransition = gameEnd.manualSpaceshipEnter;
        _input = "contact";
        _promptOpen = false;
        _previewActive = true;
        _contactLaunchRoutine = _plugin.StartCoroutine(WaitForContactScene());
        _plugin.PluginLog.LogMessage(
            "F6 正在进入结局场景；场景就绪后将直接打开最终联络输入。再次按 F6 可中止。");
    }

    private IEnumerator WaitForContactScene()
    {
        while (true)
        {
            SpaceshipSequence spaceship = FindSceneObject<SpaceshipSequence>();
            if (spaceship != null)
            {
                _contactLaunchRoutine = null;
                PlayContact(spaceship);
                yield break;
            }
            yield return null;
        }
    }

    private void PlayContact(SpaceshipSequence spaceship)
    {
        _contactTransition = null;
        if (ContactRoutineMethod == null)
        {
            _message = "ContactRoutine not found";
            _previewActive = false;
            return;
        }

        // Start() has already launched the complete ending coroutine by the time the scene
        // object becomes visible. Stop it before moving to the contact stage, otherwise its
        // delayed dialogue and camera operations will race this test entry.
        spaceship.StopAllCoroutines();
        spaceship.smallShip?.SetActive(true);
        spaceship.bigShip?.SetActive(false);
        spaceship.povCamera?.SetActive(true);
        if (spaceship.povCamera != null && spaceship.camTransmissionPos != null)
        {
            spaceship.povCamera.transform.position =
                spaceship.camTransmissionPos.transform.position;
            spaceship.povCamera.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
        }
        if (spaceship.mainScreenCanvasGroup != null)
            spaceship.mainScreenCanvasGroup.alpha = 0f;

        IEnumerator contact = ContactRoutineMethod.Invoke(spaceship, null) as IEnumerator;
        if (contact == null)
        {
            _message = "ContactRoutine could not start";
            _previewActive = false;
            return;
        }
        _contactSequence = spaceship;
        _contactRoutine = spaceship.StartCoroutine(contact);
        _input = "contact";
        _promptOpen = false;
        _previewActive = true;
        _plugin.PluginLog.LogMessage(
            "F6 最终联络输入已打开；发送内容不会修改存档。再次按 F6 可中止。");
    }

    private IEnumerator WaitForCreditsScene()
    {
        while (true)
        {
            CreditsSequence credits = FindSceneObject<CreditsSequence>();
            if (credits != null)
            {
                _creditsLaunchRoutine = null;
                PlayCredits(credits);
                yield break;
            }
            yield return null;
        }
    }

    private void PlayCredits(CreditsSequence credits)
    {
        _creditsTransition = null;
        SpaceshipSequence spaceship = FindSceneObject<SpaceshipSequence>();
        if (spaceship != null)
        {
            spaceship.StopAllCoroutines();
            spaceship.enabled = false;
        }
        credits.StopAllCoroutines();
        _plugin.LocalizeCredits(credits);
        CreditsScrollRuntime.Prepare(credits);
        credits.PlayCredits();
        _creditsSequence = credits;
        _input = "credits";
        _promptOpen = false;
        _previewActive = true;
        _plugin.PluginLog.LogMessage(
            "F6 结局滚动字幕已开始；再次按 F6 可中止。字幕播放完毕后游戏会按原流程保存并退出。");
    }

    private static T FindSceneObject<T>() where T : UnityEngine.Object
    {
        return Resources.FindObjectsOfTypeAll<T>()
            .FirstOrDefault(item => item != null &&
                                    (item as Component)?.gameObject.scene.IsValid() == true);
    }

    private static bool TrySelectHypothesesCluster(DictionaryHypotheses hypotheses,
        string stableKey, out DictionaryHypotheses.TermCluster cluster,
        out int clusterIndex)
    {
        cluster = default;
        clusterIndex = -1;
        if (hypotheses == null)
            return false;
        if (stableKey == "hypotheses")
        {
            cluster = hypotheses.TermClusterExceeded;
            return true;
        }
        if (!int.TryParse(stableKey.Substring("hypotheses:".Length),
                out clusterIndex) || hypotheses.termClusters == null ||
            clusterIndex < 0 || clusterIndex >= hypotheses.termClusters.Length)
            return false;
        cluster = hypotheses.termClusters[clusterIndex];
        return true;
    }

    private void ClosePreview()
    {
        if (_contactLaunchRoutine != null)
        {
            _plugin.StopCoroutine(_contactLaunchRoutine);
            _contactLaunchRoutine = null;
            if (_contactTransition != null)
            {
                _contactTransition.StopAllCoroutines();
                _contactTransition = null;
            }
            _previewActive = false;
            return;
        }
        if (_contactSequence != null)
        {
            if (_contactRoutine != null)
                _contactSequence.StopCoroutine(_contactRoutine);
            if (_contactSequence.submitPromptLabel != null)
                _contactSequence.submitPromptLabel.text = string.Empty;
            _contactRoutine = null;
            _contactSequence = null;
            _previewActive = false;
            return;
        }
        if (_creditsLaunchRoutine != null)
        {
            _plugin.StopCoroutine(_creditsLaunchRoutine);
            _creditsLaunchRoutine = null;
            if (_creditsTransition != null)
            {
                _creditsTransition.StopAllCoroutines();
                _creditsTransition = null;
            }
            _previewActive = false;
            return;
        }
        if (_creditsSequence != null)
        {
            _creditsSequence.StopAllCoroutines();
            _creditsSequence = null;
            _previewActive = false;
            return;
        }
        if (_replayChunk != null)
        {
            ReleaseReplay(quitCurrent: true);
            _previewActive = false;
            return;
        }
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

    private void ReleaseReplay(bool quitCurrent)
    {
        DialogueManager manager = _replayManager;
        DialogueChunk chunk = _replayChunk;
        _replayManager = null;
        _replayChunk = null;
        if (manager != null)
        {
            manager.OnDialogueComplete -= OnReplayComplete;
            if (quitCurrent)
                manager.QuitCurrent();
        }
        if (chunk != null)
        {
            DialogueReplayRuntime.Unregister(chunk.GetInstanceID());
            UnityEngine.Object.Destroy(chunk);
        }
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
