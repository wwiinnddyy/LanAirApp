using Humanizer;
using LanMountainDesktop.PluginSdk;
using Microsoft.Extensions.Hosting;

namespace LanMountainDesktop.SamplePlugin;

internal sealed class SamplePluginHostedService : IHostedService
{
    private readonly IPluginRuntimeContext _runtimeContext;
    private readonly SamplePluginRuntimeStateService _stateService;
    private readonly SamplePluginClockService _clockService;
    private readonly PluginLocalizer _localizer;

    public SamplePluginHostedService(
        IPluginRuntimeContext runtimeContext,
        SamplePluginRuntimeStateService stateService,
        SamplePluginClockService clockService)
    {
        _runtimeContext = runtimeContext;
        _stateService = stateService;
        _clockService = clockService;
        _localizer = PluginLocalizer.Create(runtimeContext);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var logPath = Path.Combine(_runtimeContext.DataDirectory, "sample-plugin.log");
        var dependencyText = TimeSpan.FromSeconds(75).Humanize(culture: System.Globalization.CultureInfo.InvariantCulture);
        var initMessage =
            $"[{DateTimeOffset.UtcNow:O}] {_runtimeContext.Manifest.Name} initialized in {_runtimeContext.Manifest.Version ?? "dev"}; dependency probe={dependencyText}.";

        try
        {
            File.AppendAllText(logPath, initMessage + Environment.NewLine);
            _stateService.MarkBackendReady(_localizer.Format(
                "status.backend.detail.log_written",
                "初始化日志已写入：{0}",
                logPath));
        }
        catch (Exception ex)
        {
            _stateService.MarkBackendFaulted(_localizer.Format(
                "status.backend.detail.log_write_failed",
                "初始化日志写入失败：{0}",
                ex.Message));
            throw;
        }

        _stateService.AttachClockService(_clockService);
        _clockService.Start();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _clockService.Dispose();
        return Task.CompletedTask;
    }
}
