using LanMountainDesktop.PluginSdk;
using LanMountainDesktop.SharedContracts.SampleClock;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LanMountainDesktop.SamplePlugin;

[PluginEntrance]
public sealed class SamplePlugin : PluginBase
{
    public override void Initialize(HostBuilderContext context, IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(services);

        var localizer = CreateLocalizer(context);

        services.AddSingleton(provider =>
        {
            var runtimeContext = provider.GetRequiredService<IPluginRuntimeContext>();
            Directory.CreateDirectory(runtimeContext.DataDirectory);

            return new SamplePluginRuntimeStateService(
                runtimeContext.Manifest,
                runtimeContext.PluginDirectory,
                runtimeContext.DataDirectory,
                GetHostProperty(runtimeContext, PluginHostPropertyKeys.HostApplicationName, "UnknownHost"),
                GetHostProperty(runtimeContext, PluginHostPropertyKeys.HostVersion, "UnknownVersion"),
                GetHostProperty(runtimeContext, PluginHostPropertyKeys.PluginSdkApiVersion, "UnknownApiVersion"),
                provider.GetRequiredService<IPluginMessageBus>(),
                PluginLocalizer.Create(runtimeContext));
        });

        services.AddSingleton(provider =>
        {
            var runtimeContext = provider.GetRequiredService<IPluginRuntimeContext>();
            return new SamplePluginClockService(
                runtimeContext.DataDirectory,
                provider.GetRequiredService<SamplePluginRuntimeStateService>(),
                provider.GetRequiredService<IPluginMessageBus>(),
                PluginLocalizer.Create(runtimeContext));
        });

        services.AddSingleton<IHostedService, SamplePluginHostedService>();
        services.AddPluginExport<ISampleClockExport, SamplePluginClockExport>();

        services.AddPluginSettingsPage<SamplePluginSettingsView>(
            "status",
            localizer.GetString("settings.page_title", "插件状态"));

        services.AddPluginDesktopComponent<SamplePluginStatusClockWidget>(
            "LanMountainDesktop.SamplePlugin.StatusClock",
            localizer.GetString("widget.display_name", "示例插件状态时钟"),
            iconKey: "PuzzlePiece",
            category: localizer.GetString("widget.category", "插件"),
            minWidthCells: 4,
            minHeightCells: 4,
            allowDesktopPlacement: true,
            allowStatusBarPlacement: false,
            resizeMode: PluginDesktopComponentResizeMode.Proportional,
            cornerRadiusResolver: cellSize => Math.Clamp(cellSize * 0.34, 18, 34));

        services.AddPluginDesktopComponent<SamplePluginCloseDesktopWidget>(
            "LanMountainDesktop.SamplePlugin.CloseDesktop",
            localizer.GetString("widget.close_desktop.display_name", "关闭桌面"),
            iconKey: "DismissCircle",
            category: localizer.GetString("widget.category", "插件"),
            minWidthCells: 2,
            minHeightCells: 1,
            allowDesktopPlacement: true,
            allowStatusBarPlacement: false,
            resizeMode: PluginDesktopComponentResizeMode.Free,
            cornerRadiusResolver: cellSize => Math.Clamp(cellSize * 0.28, 14, 22));
    }

    private static PluginLocalizer CreateLocalizer(HostBuilderContext context)
    {
        var pluginDirectory = context.Properties.TryGetValue("LanMountainDesktop.PluginDirectory", out var directoryValue) &&
                              directoryValue is string resolvedPluginDirectory &&
                              !string.IsNullOrWhiteSpace(resolvedPluginDirectory)
            ? resolvedPluginDirectory
            : AppContext.BaseDirectory;

        var properties = context.Properties
            .Where(pair => pair.Key is string)
            .ToDictionary(pair => (string)pair.Key, pair => (object?)pair.Value, StringComparer.OrdinalIgnoreCase);

        return new PluginLocalizer(pluginDirectory, PluginLocalizer.ResolveLanguageCode(properties));
    }

    private static string GetHostProperty(IPluginRuntimeContext context, string key, string fallback)
    {
        return context.TryGetProperty<string>(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : fallback;
    }
}
