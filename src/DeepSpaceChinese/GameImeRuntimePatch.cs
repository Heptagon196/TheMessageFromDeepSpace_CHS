using HarmonyLib;

namespace DeepSpaceChinese;

// The game's InputManager.LateUpdate consists only of:
// ldc.i4.2 (IMECompositionMode.Off), Input.set_imeCompositionMode, ret.
// Skipping it lets TMP_InputField own the normal focus-driven IME lifecycle.
[HarmonyPatch(typeof(InputManager), "LateUpdate")]
internal static class GameInputManagerImePatch
{
    internal static bool AllowOriginalLateUpdate() => false;

    [HarmonyPrefix]
    private static bool Prefix() => AllowOriginalLateUpdate();
}
