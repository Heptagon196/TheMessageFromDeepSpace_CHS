using System;
using HarmonyLib;

namespace DeepSpaceChinese;

internal static class AnalogTextBannerLocalizationRuntime
{
    internal static string PrepareSource(UiLocalizer localizer, string source) =>
        localizer?.TranslateAnimatedSource(source) ?? source;
}

[HarmonyPatch(typeof(SoundtrackTitleView), "DisplayTitle")]
internal static class SoundtrackTitleViewDisplayPatch
{
    private static bool Prefix(SoundtrackTitleView __instance, SoundtrackProfile sp)
    {
        try
        {
            DeepSpaceChinesePlugin plugin = DeepSpaceChinesePlugin.Instance;
            if (plugin == null || __instance?.textBanner == null || sp == null)
                return true;

            string source = (__instance.nowDisplayingText ?? string.Empty) +
                            (sp.songTitle ?? string.Empty);
            __instance.textBanner.DisplayText(plugin.PrepareAnalogBannerText(source));
            return false;
        }
        catch (Exception ex)
        {
            DeepSpaceChinesePlugin.Instance?.PluginLog.LogError(
                $"预翻译滚动歌曲标题失败：\n{ex}");
            return true;
        }
    }
}
