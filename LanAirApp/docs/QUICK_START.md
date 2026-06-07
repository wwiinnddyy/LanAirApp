# AirApp SDK 快速开始指南

欢迎使用 LanMountainDesktop AirApp SDK！本指南将帮助你在 15 分钟内创建第一个 AirApp。

## 前置要求

- .NET SDK 10.0 或更高版本
- LanMountainDesktop 2.0+ （用于测试）
- 任何 C# IDE（推荐 VS Code 或 Visual Studio）

## 第一步：安装模板

```bash
# 安装 AirApp 项目模板
dotnet new install LanMountainDesktop.AirAppTemplate

# 验证安装
dotnet new list | grep lmd-airapp
```

你应该看到：
- `lmd-airapp-component` - 桌面组件 AirApp
- `lmd-airapp-window` - 窗口应用 AirApp
- `lmd-airapp-hybrid` - 混合模式 AirApp

## 第二步：创建你的第一个 AirApp

### 选择 1：使用模板创建

```bash
# 创建一个桌面组件 AirApp
dotnet new lmd-airapp-component -n MyClock
cd MyClock

# 查看生成的文件
ls -la
# 你会看到：
# - MyAirApp.cs (入口点)
# - MyWidget.cs (组件实现)
# - airapp.json (清单文件)
# - MyClock.csproj (项目文件)
```

### 选择 2：从头开始

```bash
# 创建新项目
dotnet new classlib -n MyClock
cd MyClock

# 添加 AirApp SDK
dotnet add package LanMountainDesktop.AirAppSdk
```

## 第三步：编写代码

### 1. 创建入口点 (MyAirApp.cs)

```csharp
using LanMountainDesktop.AirAppSdk;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MyClock;

[AirAppEntrance]
public sealed class MyAirApp : AirAppBase
{
    public override void Initialize(HostBuilderContext context, IServiceCollection services)
    {
        // 注册时钟组件
        services.AddAirAppComponent<ClockWidget>(
            "clock",
            "时钟",
            options =>
            {
                options.Description = "显示当前时间";
                options.DefaultWidth = 2;
                options.DefaultHeight = 1;
                options.Category = "工具";
                options.IconKey = "Clock";
            });
    }

    public override Task OnStartedAsync(IAirAppRuntimeContext context)
    {
        context.Logger.Info("时钟 AirApp 已启动！");
        return Task.CompletedTask;
    }
}
```

### 2. 创建组件 (ClockWidget.cs)

```csharp
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using LanMountainDesktop.AirAppSdk;

namespace MyClock;

public sealed class ClockWidget : AirAppWidgetBase
{
    private readonly TextBlock _timeText;
    private readonly DispatcherTimer _timer;

    public ClockWidget()
    {
        // 创建显示时间的文本
        _timeText = new TextBlock
        {
            FontSize = 32,
            FontWeight = FontWeight.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        Content = _timeText;

        // 创建定时器，每秒更新一次
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += (s, e) => UpdateTime();
    }

    protected override void OnAttachedCore()
    {
        // 组件被添加到桌面时启动定时器
        UpdateTime();
        _timer.Start();
    }

    protected override void OnDetachedCore()
    {
        // 组件从桌面移除时停止定时器
        _timer.Stop();
    }

    protected override void OnAppearanceChangedCore(AirAppAppearanceSnapshot snapshot)
    {
        // 响应主题变化
        _timeText.Foreground = new SolidColorBrush(snapshot.AccentColor);
    }

    private void UpdateTime()
    {
        _timeText.Text = DateTime.Now.ToString("HH:mm:ss");
    }
}
```

### 3. 创建清单文件 (airapp.json)

```json
{
  "id": "com.example.myclock",
  "name": "我的时钟",
  "version": "1.0.0",
  "apiVersion": "6.0.0",
  "author": "你的名字",
  "description": "一个简单的时钟组件",
  "entranceAssembly": "MyClock.dll",
  "runtime": {
    "mode": "in-process",
    "capabilities": ["desktop-component"]
  },
  "components": [
    {
      "id": "clock",
      "name": "时钟",
      "defaultWidth": 2,
      "defaultHeight": 1
    }
  ]
}
```

### 4. 更新项目文件 (MyClock.csproj)

确保你的项目文件包含：

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="LanMountainDesktop.AirAppSdk" Version="6.0.0" />
  </ItemGroup>

  <!-- 将 airapp.json 复制到输出目录 -->
  <ItemGroup>
    <None Update="airapp.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>
</Project>
```

## 第四步：构建和打包

```bash
# 构建项目（Debug 模式用于开发）
dotnet build

# 发布生产版本
dotnet build -c Release

# 生成的 .laapp 包位于：
# bin/Release/net10.0/MyClock.laapp
```

## 第五步：测试你的 AirApp

### 方法 1：直接安装测试

1. 复制 `bin/Release/net10.0/MyClock.laapp` 到 LanMountainDesktop 的插件目录
2. 启动 LanMountainDesktop
3. 在桌面上右键 → 添加组件 → 找到"我的时钟"

### 方法 2：开发模式测试（推荐）

```bash
# 启动 LanMountainDesktop 并加载你的 AirApp
dotnet run --project path/to/LanMountainDesktop.csproj -- \
    --debug-airapp path/to/MyClock/bin/Debug/net10.0
```

这样可以在不打包的情况下直接测试，方便调试。

## 进阶功能

### 1. 添加设置页

```csharp
public override void Initialize(HostBuilderContext context, IServiceCollection services)
{
    // 注册组件
    services.AddAirAppComponent<ClockWidget>("clock", "时钟");

    // 添加设置页
    services.AddAirAppSettings(
        "clock-settings",
        "时钟设置",
        section => section
            .AddToggle("show-seconds", "显示秒数", defaultValue: true)
            .AddToggle("use-24h", "24小时制", defaultValue: true)
            .AddSelect("font-size", "字体大小", 
                options: new[] { "小", "中", "大" },
                defaultValue: "中"));
}
```

### 2. 创建窗口应用

```csharp
// 注册窗口
services.AddAirAppWindow<SettingsWindow>("settings", "设置窗口");

// 在组件中打开窗口
protected override void OnAttachedCore()
{
    // 双击打开设置窗口
    this.DoubleTapped += async (s, e) =>
    {
        await Context.OpenWindowAsync("settings");
    };
}
```

### 3. 使用消息总线

```csharp
// 发布消息
Context.MessageBus.Publish("clock-updated", DateTime.Now);

// 订阅消息
var subscription = Context.MessageBus.Subscribe<DateTime>("clock-updated", time =>
{
    Context.Logger.Info($"时间更新: {time}");
});

// 取消订阅
subscription.Dispose();
```

### 4. 保存数据

```csharp
// 获取数据目录
var dataDir = Context.DataDirectory;
var settingsFile = Path.Combine(dataDir, "settings.json");

// 保存设置
await File.WriteAllTextAsync(settingsFile, JsonSerializer.Serialize(settings));

// 加载设置
var json = await File.ReadAllTextAsync(settingsFile);
var settings = JsonSerializer.Deserialize<MySettings>(json);
```

## 调试技巧

### 1. 使用日志

```csharp
Context.Logger.Debug("调试信息");
Context.Logger.Info("一般信息");
Context.Logger.Warn("警告信息");
Context.Logger.Error("错误信息", exception);
```

### 2. 附加调试器

在 VS Code 或 Visual Studio 中：
1. 启动 LanMountainDesktop
2. 附加到 LanMountainDesktop 进程
3. 在你的 AirApp 代码中设置断点

### 3. 查看实时日志

```bash
# 查看 AirApp 日志
tail -f ~/.lanmountaindesktop/logs/airapp-myclock.log
```

## 常见问题

### Q: 我的 AirApp 没有出现在组件列表中？

检查：
1. `airapp.json` 是否正确复制到输出目录
2. `[AirAppEntrance]` 特性是否正确应用
3. API 版本是否匹配（当前为 6.0.0）

### Q: 如何更新已安装的 AirApp？

1. 增加 `airapp.json` 中的 `version` 字段
2. 重新构建并安装新版本
3. LanMountainDesktop 会检测版本变化并提示更新

### Q: 我可以使用第三方 NuGet 包吗？

可以！只需添加 PackageReference：

```xml
<ItemGroup>
  <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
</ItemGroup>
```

所有依赖会自动包含在 `.laapp` 包中。

## 下一步

- 📖 阅读 [完整 API 文档](./API_REFERENCE.md)
- 🎨 查看 [UI 设计指南](./UI_GUIDELINES.md)
- 🔧 学习 [最佳实践](./BEST_PRACTICES.md)
- 💡 浏览 [示例项目](../samples/)
- 🚀 发布到 [AirApp 市场](./PUBLISHING.md)

## 获取帮助

- GitHub Issues: https://github.com/LanMountain/LanMountainDesktop/issues
- Discord 社区: https://discord.gg/lanmountain
- 开发者文档: https://docs.lanmountain.com

---

祝你开发愉快！🎉
