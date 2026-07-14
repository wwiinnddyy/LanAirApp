using Avalonia;
using LanMountainDesktop.PluginSdk;

namespace LanMountainDesktop.SamplePlugin;

internal static class SamplePluginAppearance
{
    public static PluginAppearanceSnapshot? GetAppearanceSnapshot(this PluginDesktopComponentContext context)
    {
        return context.GetService<IPluginAppearanceContext>()?.Snapshot;
    }

    public static CornerRadius ResolveCornerRadius(
        this PluginAppearanceSnapshot? snapshot,
        PluginCornerRadiusPreset preset,
        CornerRadius fallback)
    {
        return snapshot?.CornerRadiusTokens is { } tokens
            ? tokens.ToCornerRadius(preset)
            : fallback;
    }
}
