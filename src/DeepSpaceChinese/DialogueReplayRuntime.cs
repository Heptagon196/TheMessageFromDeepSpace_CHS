using System.Collections.Generic;

namespace DeepSpaceChinese;

/// <summary>
/// Identifies temporary F6 replay clones. The original DialogueBank event handlers cache log
/// metadata on start and write it on completion; a clone is not in that cache, so allowing those
/// two handlers to run duplicates the previous log entry. Other subscribers remain untouched.
/// </summary>
internal static class DialogueReplayRuntime
{
    private static readonly HashSet<int> ActiveInstanceIds = new();

    internal static void Register(int instanceId) => ActiveInstanceIds.Add(instanceId);

    internal static void Unregister(int instanceId) => ActiveInstanceIds.Remove(instanceId);

    internal static bool IsReplay(int instanceId) => ActiveInstanceIds.Contains(instanceId);

    internal static bool IsReplay(DialogueChunk chunk) =>
        chunk != null && IsReplay(chunk.GetInstanceID());
}
