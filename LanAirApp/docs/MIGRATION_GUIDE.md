# Plugin SDK v5 到 AirApp SDK v6 迁移指南

本指南将帮助你将现有的 Plugin SDK v5 项目迁移到新的 AirApp SDK v6。

## 为什么要迁移？

AirApp SDK v6 带来了以下改进：

✅ **统一的开发体验** - 开发者和用户享受相同的应用能力  
✅ **更好的窗口支持** - 完整的窗口化应用开发框架  
✅ **现代化工具链** - 预览、热重载、调试支持  
✅ **改进的 API** - 更清晰、更强大的接口  
✅ **向前兼容** - 为未来功能预留扩展点  

## 快速对比

| 特性 | Plugin SDK v5 | AirApp SDK v6 |
|-----|---------------|---------------|
| 包名 | `LanMountainDesktop.PluginSdk` | `LanMountainDesktop.AirAppSdk` |
| 清单文件 | `plugin.json` | `airapp.json` |
| 包扩展名 | `.laapp` | `.laapp` (相同) |
| 入口特性 | `[PluginEntrance]` | `[AirAppEntrance]` |
| 基类 | `PluginBase` | `AirAppBase` |
| 组件接口 | `IPluginWidget` | `IAirAppWidget` |
| 窗口支持 | ❌ 有限 | ✅ 完整支持 |
| API 版本 | 5.0.0 | 6.0.0 |

## 迁移步骤

### 第一步：更新项目文件

**旧的 (v5):**

```xml
<ItemGroup>
  <PackageReference Include="LanMountainDesktop.PluginSdk" Version="5.0.0" />
</ItemGroup>
```

**新的 (v6):**

```xml
<ItemGroup>
  <PackageReference Include="LanMountainDesktop.AirAppSdk" Version="6.0.0" />
</ItemGroup>
```

### 第二步：重命名清单文件

1. 将 `plugin.json` 重命名为 `airapp.json`
2. 更新项目文件中的引用：

```xml
<ItemGroup>
  <!-- 旧的 -->
  <None Update="plugin.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
  
  <!-- 新的 -->
  <None Update="airapp.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

### 第三步：更新清单内容

**旧的 plugin.json:**

```json
{
  "id": "com.example.myplugin",
  "name": "My Plugin",
  "version": "1.0.0",
  "apiVersion": "5.0.0",
  "author": "Your Name",
  "description": "A sample plugin",
  "entranceAssembly": "MyPlugin.dll",
  "runtime": {
    "mode": "in-process"
  }
}
```

**新的 airapp.json:**

```json
{
  "id": "com.example.myplugin",
  "name": "My Plugin",
  "version": "1.0.0",
  "apiVersion": "6.0.0",
  "author": "Your Name",
  "description": "A sample plugin",
  "entranceAssembly": "MyPlugin.dll",
  "runtime": {
    "mode": "in-process",
    "capabilities": ["desktop-component"]
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

**变化说明:**
- `apiVersion` 从 `5.0.0` 改为 `6.0.0`
- 添加了 `runtime.capabilities` 字段（可选）
- 添加了 `components` 声明（可选，但推荐）

### 第四步：更新入口类

**旧的 (v5):**

```csharp
using LanMountainDesktop.PluginSdk;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MyPlugin;

[PluginEntrance]
public sealed class Plugin : PluginBase
{
    public override void Initialize(HostBuilderContext context, IServiceCollection services)
    {
        // 注册组件
        services.AddPluginDesktopComponent<MyWidget>("my-widget", "My Widget");
    }
}
```

**新的 (v6):**

```csharp
using LanMountainDesktop.AirAppSdk;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MyPlugin;

[AirAppEntrance]
public sealed class MyAirApp : AirAppBase
{
    public override void Initialize(HostBuilderContext context, IServiceCollection services)
    {
        // 注册组件
        services.AddAirAppComponent<MyWidget>("my-widget", "My Widget");
    }
}
```

**变化说明:**
- 命名空间从 `PluginSdk` 改为 `AirAppSdk`
- 特性从 `[PluginEntrance]` 改为 `[AirAppEntrance]`
- 基类从 `PluginBase` 改为 `AirAppBase`
- 方法从 `AddPluginDesktopComponent` 改为 `AddAirAppComponent`

### 第五步：更新组件类

**旧的 (v5):**

```csharp
using Avalonia.Controls;
using LanMountainDesktop.PluginSdk;

namespace MyPlugin;

public sealed class MyWidget : UserControl, IPluginWidget
{
    private IPluginComponentContext? _context;

    public IPluginComponentContext Context
    {
        get => _context!;
        set => _context = value;
    }

    public MyWidget()
    {
        Content = new TextBlock { Text = "Hello from Plugin!" };
    }

    public void OnAttached()
    {
        // 组件附加时的逻辑
    }

    public void OnDetached()
    {
        // 组件分离时的逻辑
    }

    public void OnAppearanceChanged(PluginAppearanceSnapshot snapshot)
    {
        // 主题变化时的逻辑
    }
}
```

**新的 (v6):**

```csharp
using Avalonia.Controls;
using LanMountainDesktop.AirAppSdk;

namespace MyPlugin;

public sealed class MyWidget : AirAppWidgetBase
{
    public MyWidget()
    {
        Content = new TextBlock { Text = "Hello from AirApp!" };
    }

    protected override void OnAttachedCore()
    {
        // 组件附加时的逻辑
    }

    protected override void OnDetachedCore()
    {
        // 组件分离时的逻辑
    }

    protected override void OnAppearanceChangedCore(AirAppAppearanceSnapshot snapshot)
    {
        // 主题变化时的逻辑
    }
}
```

**变化说明:**
- 直接继承 `AirAppWidgetBase` 而不是实现 `IPluginWidget`
- 生命周期方法改为 `protected override` 的 `*Core` 方法
- `PluginAppearanceSnapshot` 改为 `AirAppAppearanceSnapshot`
- `Context` 属性由基类自动处理

### 第六步：更新上下文访问

**旧的 (v5):**

```csharp
// 访问上下文属性
var componentId = Context.ComponentId;
var placementId = Context.PlacementId;

// 使用服务
var myService = Context.Services.GetService<IMyService>();

// 发送消息
Context.SendMessage("my-topic", data);

// 订阅消息
var subscription = Context.Subscribe("my-topic", payload => { });
```

**新的 (v6):**

```csharp
// 完全相同的 API！
var componentId = Context.ComponentId;
var placementId = Context.PlacementId;

var myService = Context.Services.GetService<IMyService>();

Context.SendMessage("my-topic", data);

var subscription = Context.Subscribe("my-topic", payload => { });
```

**变化说明:**
- 上下文 API 保持向后兼容，无需更改代码！

## API 映射表

### 核心类型

| Plugin SDK v5 | AirApp SDK v6 |
|---------------|---------------|
| `IPlugin` | `IAirApp` |
| `PluginBase` | `AirAppBase` |
| `IPluginWidget` | `IAirAppWidget` |
| `PluginWidgetBase` | `AirAppWidgetBase` |
| `IPluginComponentContext` | `IAirAppComponentContext` |
| `IPluginRuntimeContext` | `IAirAppRuntimeContext` |
| `PluginManifest` | `AirAppManifest` |

### 配置和选项

| Plugin SDK v5 | AirApp SDK v6 |
|---------------|---------------|
| `PluginDesktopComponentOptions` | `AirAppComponentOptions` |
| `PluginDesktopComponentResizeMode` | `AirAppComponentResizeMode` |
| `PluginRuntimeMode` | `AirAppRuntimeMode` |
| `PluginAppearanceSnapshot` | `AirAppAppearanceSnapshot` |
| `PluginCornerRadiusPreset` | `AirAppCornerRadiusPreset` |

### 扩展方法

| Plugin SDK v5 | AirApp SDK v6 |
|---------------|---------------|
| `AddPluginDesktopComponent` | `AddAirAppComponent` |
| `AddPluginSettingsSection` | `AddAirAppSettings` |
| `AddPluginExport` | `RegisterService` (在 `AirAppBase`) |

### 服务接口

| Plugin SDK v5 | AirApp SDK v6 |
|---------------|---------------|
| `IPluginLogger` | `IAirAppLogger` |
| `IPluginMessageBus` | `IAirAppMessageBus` |
| `IPluginAppearanceContext` | `IAirAppAppearanceContext` |

## 新增功能

AirApp SDK v6 新增了以下功能：

### 1. 完整的窗口支持

```csharp
// 注册窗口
services.AddAirAppWindow<MyWindow>("my-window", "My Window");

// 创建窗口类
public class MyWindow : AirAppWindowBase
{
    public override AirAppWindowDescriptor Descriptor => new()
    {
        Title = "My Window",
        Width = 800,
        Height = 600,
        ChromeMode = AirAppWindowChromeMode.Standard
    };

    public MyWindow()
    {
        Content = new TextBlock { Text = "Hello from Window!" };
    }
}

// 打开窗口
await Context.OpenWindowAsync("my-window");
```

### 2. 数据和缓存目录

```csharp
// 在 OnStartedAsync 中访问
public override Task OnStartedAsync(IAirAppRuntimeContext context)
{
    var dataDir = context.DataDirectory;   // 持久化数据目录
    var cacheDir = context.CacheDirectory; // 临时缓存目录
    
    // 保存用户数据
    var settingsPath = Path.Combine(dataDir, "settings.json");
    await File.WriteAllTextAsync(settingsPath, json);
    
    return Task.CompletedTask;
}
```

### 3. 改进的生命周期管理

```csharp
public override async Task OnStartedAsync(IAirAppRuntimeContext context)
{
    // 应用启动后的异步初始化
    await InitializeAsync();
}

public override async Task OnStoppingAsync()
{
    // 应用停止前的清理
    await CleanupAsync();
}
```

## 常见迁移问题

### Q: 我的插件会立即停止工作吗？

**A:** 不会！Plugin SDK v5 在短期内仍然受支持。你可以选择合适的时间迁移。

### Q: 迁移后需要更改包 ID 吗？

**A:** 不需要。保持相同的 `id`，但建议增加 `version` 号（例如从 1.0.0 升到 2.0.0），让用户知道这是一个重大更新。

### Q: 用户需要重新安装吗？

**A:** 是的。由于 API 版本变化（v5 → v6），用户需要卸载旧版本并安装新版本。LanMountainDesktop 会自动检测并提示用户。

### Q: 我可以同时支持两个版本吗？

**A:** 不推荐。建议发布两个独立的包：
- `MyPlugin-v5.laapp` (Plugin SDK v5)
- `MyPlugin-v6.laapp` (AirApp SDK v6)

### Q: 迁移后性能会受影响吗？

**A:** 不会。AirApp SDK v6 的性能与 Plugin SDK v5 相当或更好。

## 测试清单

在发布迁移后的版本前，请确保：

- [ ] 项目成功构建
- [ ] 生成了 `.laapp` 包
- [ ] `airapp.json` 正确复制到输出目录
- [ ] 在 LanMountainDesktop 中可以安装
- [ ] 组件可以正常添加到桌面
- [ ] 所有功能正常工作
- [ ] 主题切换正常响应
- [ ] 日志记录正常
- [ ] 设置页面正常（如果有）
- [ ] 窗口正常打开和关闭（如果有）

## 自动化迁移工具

我们提供了自动化迁移工具来简化流程：

```bash
# 安装迁移工具
dotnet tool install -g LanMountainDesktop.AirAppMigrator

# 运行迁移
cd path/to/your/plugin
lmd-airapp-migrate

# 迁移工具会自动：
# - 更新项目文件
# - 重命名清单文件
# - 更新命名空间
# - 更新类名和接口
# - 创建迁移报告
```

## 获取帮助

如果在迁移过程中遇到问题：

1. **查看示例**：`samples/` 目录中有完整的迁移前后对比
2. **阅读文档**：https://docs.lanmountain.com/migration-guide
3. **提问**：Discord 社区 https://discord.gg/lanmountain
4. **报告 Bug**：https://github.com/LanMountain/LanMountainDesktop/issues

## 总结

迁移到 AirApp SDK v6 主要涉及：

1. 更新包引用
2. 重命名文件和类
3. 更新命名空间
4. 利用新功能（可选）

大部分 API 保持兼容，迁移过程应该很顺利。如果你按照本指南操作，整个迁移通常只需要 15-30 分钟。

欢迎加入 AirApp SDK v6！🎉
