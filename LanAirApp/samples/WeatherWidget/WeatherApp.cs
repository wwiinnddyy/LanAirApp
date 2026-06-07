using LanMountainDesktop.AirAppSdk;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace WeatherWidget;

[AirAppEntrance]
public sealed class WeatherApp : AirAppBase
{
    public override void Initialize(HostBuilderContext context, IServiceCollection services)
    {
        services.AddAirAppComponent<WeatherWidget>(
            "weather",
            "天气",
            options =>
            {
                options.Description = "显示当前天气和温度";
                options.DefaultWidth = 2;
                options.DefaultHeight = 2;
                options.ResizeMode = AirAppComponentResizeMode.Both;
                options.Category = "信息";
                options.IconKey = "Weather";
            });
    }

    public override Task OnStartedAsync(IAirAppRuntimeContext context)
    {
        context.Logger.Info("天气 AirApp 已启动");
        return Task.CompletedTask;
    }
}
