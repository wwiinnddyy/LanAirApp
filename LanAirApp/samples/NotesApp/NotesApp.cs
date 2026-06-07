using LanMountainDesktop.AirAppSdk;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace NotesApp;

[AirAppEntrance]
public sealed class NotesApp : AirAppBase
{
    public override void Initialize(HostBuilderContext context, IServiceCollection services)
    {
        // 注册组件
        services.AddAirAppComponent<NotesWidget>(
            "notes-widget",
            "笔记",
            options =>
            {
                options.Description = "快速打开笔记应用";
                options.DefaultWidth = 2;
                options.DefaultHeight = 1;
                options.Category = "工具";
                options.IconKey = "Edit";
            });

        // 注册窗口
        services.AddAirAppWindow<NotesWindow>("notes-window", "笔记");
    }

    public override Task OnStartedAsync(IAirAppRuntimeContext context)
    {
        context.Logger.Info("笔记 AirApp 已启动");
        return Task.CompletedTask;
    }
}
