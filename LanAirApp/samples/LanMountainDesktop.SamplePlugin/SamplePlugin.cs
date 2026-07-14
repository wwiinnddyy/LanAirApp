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
            CreateStatusClockComponentOptions(localizer));

        services.AddPluginDesktopComponent<SamplePluginCloseDesktopWidget>(
            CreateCloseDesktopComponentOptions(localizer));
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

    private static PluginDesktopComponentOptions CreateStatusClockComponentOptions(PluginLocalizer localizer)
    {
        return new PluginDesktopComponentOptions
        {
            ComponentId = "LanMountainDesktop.SamplePlugin.StatusClock",
            DisplayName = localizer.GetString("widget.display_name", "Sample Plugin Status Clock"),
            DisplayNameLocalizationKey = "widget.display_name",
            IconKey = "PuzzlePiece",
            Category = localizer.GetString("widget.category", "Plugins"),
            MinWidthCells = 4,
            MinHeightCells = 4,
            AllowDesktopPlacement = true,
            AllowStatusBarPlacement = false,
            ResizeMode = PluginDesktopComponentResizeMode.Proportional,
            CornerRadiusPreset = PluginCornerRadiusPreset.Default
        };
    }

    private static PluginDesktopComponentOptions CreateCloseDesktopComponentOptions(PluginLocalizer localizer)
    {
        return new PluginDesktopComponentOptions
        {
            ComponentId = "LanMountainDesktop.SamplePlugin.CloseDesktop",
            DisplayName = localizer.GetString("widget.close_desktop.display_name", "Close Desktop"),
            DisplayNameLocalizationKey = "widget.close_desktop.display_name",
            IconKey = "DismissCircle",
            Category = localizer.GetString("widget.category", "Plugins"),
            MinWidthCells = 2,
            MinHeightCells = 1,
            AllowDesktopPlacement = true,
            AllowStatusBarPlacement = false,
            ResizeMode = PluginDesktopComponentResizeMode.Free,
            CornerRadiusPreset = PluginCornerRadiusPreset.Default
        };
    }
}
