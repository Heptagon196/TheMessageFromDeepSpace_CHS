using System.Reflection;
using System.Text;
using HarmonyLib;
using TMPro;

namespace DeepSpaceChinese;

[HarmonyPatch(typeof(DialogueManager), nameof(DialogueManager.DrawAdvanceInput))]
internal static class DialogueCompletionDiagnosticsPatch
{
    private static readonly FieldInfo SubtitleField =
        AccessTools.Field(typeof(DialogueManager), "subtitle");
    private static readonly FieldInfo CurrentChunkField =
        AccessTools.Field(typeof(DialogueManager), "currChunk");

    [HarmonyPostfix]
    private static void Postfix(DialogueManager __instance, bool __0)
    {
        if (!__0)
            return;
        var subtitle = SubtitleField?.GetValue(__instance) as TMP_Text;
        string raw = subtitle?.text ?? string.Empty;
        string visible = StripRichText(raw);
        if (visible.IndexOf('.') < 0 && visible.IndexOf('…') < 0)
            return;

        var chunk = CurrentChunkField?.GetValue(__instance) as DialogueChunk;
        DeepSpaceChinesePlugin.Instance?.PluginLog.LogInfo(
            $"[DEBUG-ELLIPSIS-FINAL] chunk={chunk?.UniqueID.ToString() ?? "null"}; " +
            $"visible={Quote(visible)}; codepoints={CodePoints(visible)}; " +
            $"maxVisible={subtitle?.maxVisibleCharacters.ToString() ?? "null"}; " +
            $"tmpCharacters={subtitle?.textInfo?.characterCount.ToString() ?? "null"}; " +
            $"raw={Quote(raw)}");
    }

    private static string StripRichText(string text)
    {
        var result = new StringBuilder(text?.Length ?? 0);
        bool insideTag = false;
        foreach (char value in text ?? string.Empty)
        {
            if (!insideTag && value == '<')
            {
                insideTag = true;
                continue;
            }
            if (insideTag)
            {
                if (value == '>')
                    insideTag = false;
                continue;
            }
            result.Append(value);
        }
        return result.ToString();
    }

    private static string CodePoints(string text)
    {
        var result = new StringBuilder();
        foreach (char value in text ?? string.Empty)
        {
            if (result.Length > 0)
                result.Append(',');
            result.Append("U+").Append(((int)value).ToString("X4"));
        }
        return result.ToString();
    }

    private static string Quote(string text) =>
        "\"" + (text ?? string.Empty)
            .Replace("\\", "\\\\")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n")
            .Replace("\"", "\\\"") + "\"";
}
