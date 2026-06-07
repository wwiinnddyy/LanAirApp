# LanMountainDesktop AirApp SDK v6.0

> 为阑山桌面开发轻应用的官方 SDK

[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4)](https://dotnet.microsoft.com/)
[![Avalonia UI](https://img.shields.io/badge/Avalonia%20UI-11.2-blue)](https://avaloniaui.net/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Version](https://img.shields.io/badge/Version-6.0.0-orange.svg)](https://github.com/LanMountain/LanMountainDesktop)

## 🚀 什么是 AirApp SDK？

AirApp SDK 是阑山桌面（LanMountainDesktop）的下一代应用开发框架。通过 AirApp SDK，你可以：

- 🎨 **开发桌面组件** - 创建嵌入桌面的精美小部件
- 🪟 **开发窗口应用** - 构建独立的窗口化应用
- ⚙️ **开发后台服务** - 实现后台运行的服务
- 🔗 **混合模式** - 在同一个 AirApp 中组合多种形态

## ✨ 核心特性

### 统一体验
开发者使用的 API 与系统内置应用完全相同，无缝集成到阑山桌面生态。

### 现代化工具链
- 📦 **项目模板** - 一键生成项目脚手架
- 🔥 **热重载** - 实时预览代码变化（即将推出）
- 🐛 **调试支持** - 完整的断点和日志调试
- 📖 **完整文档** - 从入门到精通

### 灵活架构
```csharp
// 选择运行模式
"runtime": {
  "mode": "in-process",          // 最佳性能
  // "isolated-background",      // 独立进程，更安全
  // "isolated-window"           // 完全隔离
}
```

## 🎯 快速开始

### 安装模板

```bash
dotnet new install LanMountainDesktop.AirAppTemplate
```

### 创建你的第一个 AirApp

```bash
# 创建桌面组件 AirApp
dotnet new lmd-airapp-component -n MyClock
cd MyClock

# 构建项目
dotnet build

# 生成的 .laapp 包位于 bin/Debug/net10.0/
```

### 示例代码

```csharp
// 1. 入口点
[AirAppEntrance]
public class MyAirApp : AirAppBase
{
    public override void Initialize(HostBuilderContext context, IServiceCollection services)
    {
        services.AddAirAppComponent<ClockWidget>("clock", "时钟");
    }
}

// 2. 组件实现
public class ClockWidget : AirAppWidgetBase
{
    private readonly TextBlock _timeText;
    
    public ClockWidget()
    {
        _timeText = new TextBlock
        {
            FontSize = 32,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        Content = _timeText;
        
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        timer.Tick += (s, e) => _timeText.Text = DateTime.Now.ToString("HH:mm:ss");
        timer.Start();
    }
}
```

仅需 20 行代码即可创建一个时钟组件！

## 📚 文档

| 文档 | 描述 |
|------|------|
| [快速开始](./docs/QUICK_START.md) | 15分钟上手指南 |
| [API 参考](./docs/API_REFERENCE.md) | 完整的 API 文档 |
| [迁移指南](./docs/MIGRATION_GUIDE.md) | 从 Plugin SDK v5 迁移 |
| [设计文档](./docs/AIR_APP_SDK_DESIGN.md) | 架构设计和规划 |
| [实现总结](./docs/IMPLEMENTATION_SUMMARY.md) | 项目完成情况 |

## 🎨 示例项目

查看 `samples/` 目录获取完整示例：

- **简单时钟** - 显示当前时间的桌面组件
- **天气小部件** - 获取并显示天气信息（即将推出）
- **笔记应用** - 窗口化的笔记应用（即将推出）
- **系统监控** - 显示 CPU、内存等信息（即将推出）

## 🔄 从 Plugin SDK v5 迁移

如果你已经有基于 Plugin SDK v5 的项目，迁移到 AirApp SDK v6 非常简单：

1. 更新包引用：`PluginSdk` → `AirAppSdk`
2. 重命名清单文件：`plugin.json` → `airapp.json`
3. 更新命名空间和类名
4. 更新 API 版本：`5.0.0` → `6.0.0`

**迁移时间**: 15-30 分钟  
**详细指南**: [MIGRATION_GUIDE.md](./docs/MIGRATION_GUIDE.md)

## 🛠️ 技术栈

- **.NET 10** - 现代化的运行时平台
- **Avalonia UI 11.2** - 跨平台 UI 框架
- **FluentAvalonia** - Fluent Design 组件库
- **Microsoft.Extensions*** - 依赖注入和配置

## 📦 项目结构

```
LanMountainDesktop.AirAppSdk/
├── Core Interfaces/          # 核心接口定义
│   ├── IAirApp.cs
│   ├── IAirAppWidget.cs
│   ├── IAirAppWindow.cs
│   └── IAirAppRuntimeContext.cs
├── Base Classes/             # 基础实现类
│   ├── AirAppBase.cs
│   ├── AirAppWidgetBase.cs
│   └── AirAppWindowBase.cs
├── Configuration/            # 配置和模型
│   ├── AirAppManifest.cs
│   ├── AirAppComponentOptions.cs
│   └── AirAppWindowDescriptor.cs
├── Services/                 # 服务接口
│   ├── IAirAppLogger.cs
│   ├── IAirAppMessageBus.cs
│   └── IAirAppAppearanceContext.cs
└── Extensions/               # 扩展方法
    └── AirAppServiceCollectionExtensions.cs
```

## 🌟 核心概念

### AirApp 清单 (airapp.json)

```json
{
  "id": "com.example.myapp",
  "name": "My AirApp",
  "version": "1.0.0",
  "apiVersion": "6.0.0",
  "author": "Your Name",
  "description": "A sample AirApp",
  "entranceAssembly": "MyApp.dll",
  "runtime": {
    "mode": "in-process",
    "capabilities": ["desktop-component", "window"]
  },
  "components": [
    {
      "id": "my-widget",
      "name": "My Widget",
      "defaultWidth": 2,
      "defaultHeight": 2
    }
  ]
}
```

### 生命周期

```csharp
[AirAppEntrance]
public class MyAirApp : AirAppBase
{
    // 1. 初始化 - 注册服务和组件
    public override void Initialize(HostBuilderContext context, IServiceCollection services)
    {
        services.AddAirAppComponent<MyWidget>("widget", "组件");
    }

    // 2. 启动后 - 访问运行时服务
    public override Task OnStartedAsync(IAirAppRuntimeContext context)
    {
        context.Logger.Info("AirApp 已启动");
        return Task.CompletedTask;
    }

    // 3. 停止前 - 清理资源
    public override Task OnStoppingAsync()
    {
        return Task.CompletedTask;
    }
}
```

### 桌面组件

```csharp
public class MyWidget : AirAppWidgetBase
{
    protected override void OnAttachedCore()
    {
        // 组件被添加到桌面
    }

    protected override void OnDetachedCore()
    {
        // 组件从桌面移除
    }

    protected override void OnAppearanceChangedCore(AirAppAppearanceSnapshot snapshot)
    {
        // 主题变化时响应
        Foreground = new SolidColorBrush(snapshot.ForegroundColor);
    }
}
```

### 窗口应用

```csharp
public class MyWindow : AirAppWindowBase
{
    public override AirAppWindowDescriptor Descriptor => new()
    {
        Title = "My Window",
        Width = 800,
        Height = 600,
        ChromeMode = AirAppWindowChromeMode.Standard
    };

    public override Task OnWindowOpeningAsync()
    {
        // 窗口打开前的异步初始化
        return Task.CompletedTask;
    }
}
```

## 💡 最佳实践

### 1. 数据持久化

```csharp
// 使用提供的数据目录
var dataDir = Context.DataDirectory;
var settingsPath = Path.Combine(dataDir, "settings.json");
await File.WriteAllTextAsync(settingsPath, json);
```

### 2. 响应主题变化

```csharp
protected override void OnAppearanceChangedCore(AirAppAppearanceSnapshot snapshot)
{
    var isDark = snapshot.IsDarkMode;
    var accent = snapshot.AccentColor;
    // 更新你的 UI
}
```

### 3. 使用消息总线

```csharp
// 发布消息
Context.MessageBus.Publish("data-updated", newData);

// 订阅消息
var subscription = Context.MessageBus.Subscribe<MyData>("data-updated", data =>
{
    UpdateUI(data);
});

// 取消订阅
subscription.Dispose();
```

### 4. 日志记录

```csharp
Context.Logger.Debug("调试信息");
Context.Logger.Info("一般信息");
Context.Logger.Warn("警告信息");
Context.Logger.Error("错误信息", exception);
```

## 🔧 开发工具

### 调试

```bash
# 在 LanMountainDesktop 中调试你的 AirApp
dotnet run --project path/to/LanMountainDesktop.csproj -- \
    --debug-airapp path/to/your/bin/Debug/net10.0
```

### 打包

```bash
# 构建发布版本
dotnet build -c Release

# .laapp 包位于
# bin/Release/net10.0/YourAirApp.laapp
```

## 🤝 贡献

欢迎贡献代码、报告问题或提出建议！

- **GitHub Issues**: [报告问题](https://github.com/LanMountain/LanMountainDesktop/issues)
- **Pull Requests**: [贡献代码](https://github.com/LanMountain/LanMountainDesktop/pulls)
- **Discord**: [加入社区](https://discord.gg/lanmountain)

## 📄 许可证

MIT License - 详见 [LICENSE](LICENSE) 文件

## 🙏 致谢

感谢所有为阑山桌面和 AirApp SDK 做出贡献的开发者！

特别感谢：
- [Avalonia UI](https://avaloniaui.net/) - 跨平台 UI 框架
- [FluentAvalonia](https://github.com/amwx/FluentAvalonia) - Fluent Design 组件
- [ClassIsland](https://github.com/ClassIsland/ClassIsland) - 参考项目

## 📞 联系我们

- **官网**: https://lanmountain.com
- **文档**: https://docs.lanmountain.com
- **GitHub**: https://github.com/LanMountain/LanMountainDesktop
- **Discord**: https://discord.gg/lanmountain
- **Email**: dev@lanmountain.com

---

**开始你的 AirApp 开发之旅！** 🚀

查看 [快速开始指南](./docs/QUICK_START.md) 在 15 分钟内创建你的第一个 AirApp。
