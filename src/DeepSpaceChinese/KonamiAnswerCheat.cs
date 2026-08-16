using System;
using BepInEx.Logging;
using TMPro;
using UnityEngine;

namespace DeepSpaceChinese;

internal sealed class KonamiCodeDetector
{
    private static readonly KeyCode[] Sequence =
    {
        KeyCode.UpArrow, KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.DownArrow,
        KeyCode.LeftArrow, KeyCode.RightArrow, KeyCode.LeftArrow, KeyCode.RightArrow,
        KeyCode.B, KeyCode.A,
    };

    public int Progress { get; private set; }

    public bool Push(KeyCode key)
    {
        if (key == Sequence[Progress])
        {
            Progress++;
            if (Progress < Sequence.Length)
                return false;
            Progress = 0;
            return true;
        }

        Progress = key == Sequence[0] ? 1 : 0;
        return false;
    }

    public void Reset() => Progress = 0;
}

internal sealed class KonamiAnswerCheatRuntime
{
    private readonly PatchConfig _config;
    private readonly ManualLogSource _log;
    private readonly KonamiCodeDetector _detector = new();

    public KonamiAnswerCheatRuntime(PatchConfig config, ManualLogSource log)
    {
        _config = config;
        _log = log;
    }

    public void Update()
    {
        if (!_config.KonamiAnswerAutofillEnabled || !TryGetActivePuzzleInput(out InputTextDummy dummy))
        {
            _detector.Reset();
            return;
        }

        if (!TryReadSequenceKey(out KeyCode key))
        {
            if (Input.anyKeyDown)
                _detector.Reset();
            return;
        }
        if (!_detector.Push(key))
            return;

        FillCurrentAnswer(dummy);
    }

    private void FillCurrentAnswer(InputTextDummy dummy)
    {
        ConsoleDisplay console = ConsoleDisplay.Instance;
        Puzzle puzzle = PuzzleManager.Instance?.CurrPuzzle;
        if (console == null || puzzle == null || puzzle.WinningResponse.signals == null)
        {
            _log.LogWarning("作弊序列已触发，但当前没有可填入答案的题目。");
            return;
        }

        int lineCount = 0;
        string answer = console.QuickCompile(puzzle.WinningResponse, ref lineCount);
        if (string.IsNullOrWhiteSpace(answer))
        {
            _log.LogWarning($"作弊序列已触发，但题目 {puzzle.TotalID} 的正确答案无法编译为文本。");
            return;
        }

        dummy.SetText(answer);
        TMP_InputField inputField = dummy.InputField;
        inputField.MoveTextEnd(false);
        inputField.ActivateInputField();
        _log.LogMessage($"已把题目 {puzzle.TotalID} 的正确答案填入回复框（未自动提交）。");
    }

    private static bool TryGetActivePuzzleInput(out InputTextDummy dummy)
    {
        dummy = TextDummyManager.Instance_PuzzleInput;
        if (dummy?.InputField == null || dummy.InputRecipient == null)
            return false;

        // ConsoleDisplay holds the reply writer by serialized reference; the writer is not
        // guaranteed to be a Transform child of ConsoleDisplay. InputTextDummy clears its
        // recipient on deselect, so the concrete InputDisplaySelector type is the reliable
        // indication that the shared puzzle input is currently attached to the reply box.
        return ConsoleDisplay.Instance != null &&
               IsPuzzleReplyRecipientType(dummy.InputRecipient.GetType());
    }

    internal static bool IsPuzzleReplyRecipientType(Type recipientType) =>
        recipientType != null && typeof(InputDisplaySelector).IsAssignableFrom(recipientType);

    private static bool TryReadSequenceKey(out KeyCode key)
    {
        KeyCode[] keys =
        {
            KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow,
            KeyCode.B, KeyCode.A,
        };
        foreach (KeyCode candidate in keys)
        {
            if (!Input.GetKeyDown(candidate))
                continue;
            key = candidate;
            return true;
        }
        key = KeyCode.None;
        return false;
    }
}
