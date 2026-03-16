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

        services.AddPluginSettingsSection(
            id: "status",
            titleLocalizationKey: "settings.page_title",
            configure: builder =>
            {
                builder.AddText(
                    key: "status.note",
                    titleLocalizationKey: "settings.page_title",
                    descriptionLocalizationKey: "plugin.description",
                    defaultValue: localizer.GetString(
                        "settings.section.status_hint",
                        "Use this section to verify plugin runtime status."));
            },
            descriptionLocalizationKey: "plugin.description",
            iconKey: "PuzzlePiece",
            sortOrder: 0);

        services.AddPluginDesktopComponent<SamplePluginStatusClockWidget>(
            "LanMountainDesktop.SamplePlugin.StatusClock",
            localizer.GetString("widget.display_name", "Sample Plugin Status Clock"),
            iconKey: "PuzzlePiece",
            category: localizer.GetString("widget.category", "Plugins"),
            minWidthCells: 4,
            minHeightCells: 4,
            allowDesktopPlacement: true,
            allowStatusBarPlacement: false,
            resizeMode: PluginDesktopComponentResizeMode.Proportional,
            cornerRadiusResolver: cellSize => Math.Clamp(cellSize * 0.34, 18, 34));

        services.AddPluginDesktopComponent<SamplePluginCloseDesktopWidget>(
            "LanMountainDesktop.SamplePlugin.CloseDesktop",
            localizer.GetString("widget.close_desktop.display_name", "Close Desktop"),
            iconKey: "DismissCircle",
            category: localizer.GetString("widget.category", "Plugins"),
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
