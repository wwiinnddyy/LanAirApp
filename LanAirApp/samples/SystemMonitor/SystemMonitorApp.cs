using LanMountainDesktop.AirAppSdk;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace SystemMonitor;

[AirAppEntrance]
public sealed class SystemMonitorApp : AirAppBase
{
    public override void Initialize(HostBuilderContext context, IServiceCollection services)
    {
        services.AddAirAppComponent<SystemMonitorWidget>(
            "system-monitor",
            "系统监控",
            options =>
            {
                options.Description = "显示 CPU 和内存使用率";
                options.DefaultWidth = 3;
                options.DefaultHeight = 2;
                options.ResizeMode = AirAppComponentResizeMode.Horizontal;
                options.Category = "系统";
                options.IconKey = "System";
            });
    }

    public override Task OnStartedAsync(IAirAppRuntimeContext context)
    {
        context.Logger.Info("系统监控 AirApp 已启动");
        return Task.CompletedTask;
    }
}
