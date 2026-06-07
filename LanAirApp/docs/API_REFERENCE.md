# AirApp SDK API 参考文档

完整的 AirApp SDK API 参考。

## 核心接口

### IAirApp

AirApp 的入口接口。

```csharp
public interface IAirApp
{
    void Initialize(HostBuilderContext context, IServiceCollection services);
    Task OnStartedAsync(IAirAppRuntimeContext context);
    Task OnStoppingAsync();
}
```

**方法:**

- `Initialize()` - 在宿主启动时调用，用于注册服务和组件
- `OnStartedAsync()` - 在宿主启动后调用，可以访问运行时上下文
- `OnStoppingAsync()` - 在宿主停止时调用，用于清理资源

### IAirAppRuntimeContext

运行时上下文，提供对 AirApp 运行时服务的访问。

```csharp
public interface IAirAppRuntimeContext
{
    // 基本信息
    string AirAppId { get; }
    string AirAppName { get; }
    string AirAppVersion { get; }
    
    // 目录
    string DataDirectory { get; }
    string CacheDirectory { get; }
    
    // 服务
    IServiceProvider Services { get; }
    IHostApplicationLifetime Lifetime { get; }
    IAirAppMessageBus MessageBus { get; }
    IAirAppAppearanceContext Appearance { get; }
    IAirAppLogger Logger { get; }
    
    // 窗口管理
    Task<IAirAppWindow> OpenWindowAsync(string windowId);
    void CloseWindow(string windowId);
}
```

**属性:**

- `AirAppId` - AirApp 的唯一标识符
- `AirAppName` - AirApp 的显示名称
- `DataDirectory` - 数据持久化目录（用户数据应存储在这里）
- `CacheDirectory` - 缓存目录（临时数据可存储在这里）
- `Services` - 依赖注入服务提供者
- `MessageBus` - 消息总线，用于 AirApp 间通信
- `Appearance` - 外观上下文，获取主题信息
- `Logger` - 日志记录器

**方法:**

- `OpenWindowAsync()` - 打开一个已注册的窗口
- `CloseWindow()` - 关闭指定的窗口

### IAirAppWidget

桌面组件接口。

```csharp
public interface IAirAppWidget
{
    IAirAppComponentContext Context { get; set; }
    void OnAttached();
    void OnDetached();
    void OnAppearanceChanged(AirAppAppearanceSnapshot snapshot);
}
```

**属性:**

- `Context` - 组件实例的上下文

**方法:**

- `OnAttached()` - 组件被添加到桌面时调用
- `OnDetached()` - 组件从桌面移除时调用
- `OnAppearanceChanged()` - 主题或外观变化时调用

### IAirAppWindow

窗口应用接口。

```csharp
public interface IAirAppWindow
{
    AirAppWindowDescriptor Descriptor { get; }
    Task OnWindowOpeningAsync();
    void OnWindowOpened();
    void OnWindowClosing(WindowClosingEventArgs e);
    void OnWindowClosed();
}
```

**属性:**

- `Descriptor` - 窗口配置描述符

**方法:**

- `OnWindowOpeningAsync()` - 窗口打开前调用（异步初始化）
- `OnWindowOpened()` - 窗口打开后调用
- `OnWindowClosing()` - 窗口关闭前调用（可以取消关闭）
- `OnWindowClosed()` - 窗口关闭后调用

### IAirAppComponentContext

组件实例上下文。

```csharp
public interface IAirAppComponentContext
{
    string ComponentId { get; }
    string PlacementId { get; }
    int Width { get; }
    int Height { get; }
    IServiceProvider Services { get; }
    IAirAppAppearanceContext Appearance { get; }
    
    Task OpenWindowAsync(string windowId);
    void SendMessage(string topic, object? payload = null);
    IDisposable Subscribe(string topic, Action<object?> handler);
}
```

**属性:**

- `ComponentId` - 组件类型标识符
- `PlacementId` - 组件实例的唯一标识符
- `Width` / `Height` - 组件的当前尺寸（单位：网格单元）
- `Services` - 服务提供者
- `Appearance` - 外观上下文

**方法:**

- `OpenWindowAsync()` - 打开窗口
- `SendMessage()` - 发送消息
- `Subscribe()` - 订阅消息

## 基类

### AirAppBase

`IAirApp` 的基础实现。

```csharp
public abstract class AirAppBase : IAirApp
{
    protected IAirAppRuntimeContext? RuntimeContext { get; }
    
    public virtual void Initialize(HostBuilderContext context, IServiceCollection services) { }
    public virtual Task OnStartedAsync(IAirAppRuntimeContext context) { }
    public virtual Task OnStoppingAsync() { }
    
    protected void RegisterComponent<TWidget>(string id, string name, Action<AirAppComponentOptions>? configure = null);
    protected void RegisterWindow<TWindow>(string id, string name);
    protected void RegisterService<TService, TImplementation>();
}
```

### AirAppWidgetBase

`IAirAppWidget` 的 Avalonia 基础实现。

```csharp
public abstract class AirAppWidgetBase : UserControl, IAirAppWidget
{
    public IAirAppComponentContext Context { get; set; }
    
    protected virtual void OnContextSet() { }
    protected virtual void OnAttachedCore() { }
    protected virtual void OnDetachedCore() { }
    protected virtual void OnAppearanceChangedCore(AirAppAppearanceSnapshot snapshot) { }
}
```

### AirAppWindowBase

`IAirAppWindow` 的 Avalonia 窗口基础实现。

```csharp
public abstract class AirAppWindowBase : Window, IAirAppWindow
{
    public virtual AirAppWindowDescriptor Descriptor { get; }
    
    public virtual Task OnWindowOpeningAsync() { }
    public virtual void OnWindowOpened() { }
    public virtual void OnWindowClosing(WindowClosingEventArgs e) { }
    public virtual void OnWindowClosed() { }
}
```

## 配置类

### AirAppManifest

AirApp 清单文件模型。

```csharp
public sealed record AirAppManifest(
    string Id,
    string Name,
    string EntranceAssembly,
    string? Description = null,
    string? Author = null,
    string? Version = null,
    string? ApiVersion = null,
    AirAppRuntimeConfiguration? Runtime = null,
    IReadOnlyList<AirAppComponentManifest>? Components = null,
    IReadOnlyList<AirAppWindowManifest>? Windows = null
);
```

### AirAppComponentOptions

组件注册选项。

```csharp
public sealed class AirAppComponentOptions
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required Type WidgetType { get; set; }
    public string? Description { get; set; }
    public int DefaultWidth { get; set; } = 2;
    public int DefaultHeight { get; set; } = 2;
    public AirAppComponentResizeMode ResizeMode { get; set; }
    public bool AllowMultipleInstances { get; set; } = true;
    public string? Category { get; set; }
    public string? IconKey { get; set; }
}
```

### AirAppWindowDescriptor

窗口配置描述符。

```csharp
public sealed class AirAppWindowDescriptor
{
    public string Title { get; set; } = "AirApp Window";
    public double Width { get; set; } = 800;
    public double Height { get; set; } = 600;
    public double MinWidth { get; set; } = 400;
    public double MinHeight { get; set; } = 300;
    public AirAppWindowChromeMode ChromeMode { get; set; }
    public bool CanResize { get; set; } = true;
    public bool ShowInTaskbar { get; set; } = true;
    public bool ShowAsDialog { get; set; } = false;
}
```

### AirAppAppearanceSnapshot

主题外观快照。

```csharp
public sealed class AirAppAppearanceSnapshot
{
    public bool IsDarkMode { get; init; }
    public Color AccentColor { get; init; }
    public double GlassOpacity { get; init; }
    public AirAppCornerRadiusPreset CornerRadiusPreset { get; init; }
    public Color BackgroundColor { get; init; }
    public Color ForegroundColor { get; init; }
    public Color BorderColor { get; init; }
}
```

## 枚举

### AirAppRuntimeMode

运行时模式。

```csharp
public enum AirAppRuntimeMode
{
    InProcess = 0,           // 进程内运行
    IsolatedBackground = 1,  // 独立后台进程
    IsolatedWindow = 2       // 独立窗口进程
}
```

### AirAppWindowChromeMode

窗口装饰模式。

```csharp
public enum AirAppWindowChromeMode
{
    Standard = 0,       // 标准窗口
    Borderless = 1,     // 无边框窗口
    FullScreen = 2,     // 全屏窗口
    Tool = 3,           // 工具窗口
    BackgroundOnly = 4  // 仅后台
}
```

### AirAppComponentResizeMode

组件调整大小模式。

```csharp
public enum AirAppComponentResizeMode
{
    None = 0,       // 不可调整大小
    Horizontal = 1, // 仅水平调整
    Vertical = 2,   // 仅垂直调整
    Both = 3        // 双向调整
}
```

### AirAppCornerRadiusPreset

圆角半径预设。

```csharp
public enum AirAppCornerRadiusPreset
{
    None = 0,        // 无圆角
    Small = 1,       // 小圆角 (4px)
    Medium = 2,      // 中等圆角 (8px)
    Large = 3,       // 大圆角 (12px)
    ExtraLarge = 4   // 超大圆角 (16px)
}
```

## 扩展方法

### AirAppServiceCollectionExtensions

```csharp
public static class AirAppServiceCollectionExtensions
{
    public static IServiceCollection AddAirAppComponent<TWidget>(
        this IServiceCollection services,
        string id,
        string name,
        Action<AirAppComponentOptions>? configure = null);
    
    public static IServiceCollection AddAirAppWindow<TWindow>(
        this IServiceCollection services,
        string id,
        string name);
    
    public static IServiceCollection AddAirAppSettings(
        this IServiceCollection services,
        string id,
        string name,
        Action<AirAppSettingsSectionBuilder>? configure = null);
}
```

## 特性

### AirAppEntranceAttribute

标记 AirApp 入口类。

```csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class AirAppEntranceAttribute : Attribute
{
}
```

**用法:**

```csharp
[AirAppEntrance]
public class MyAirApp : AirAppBase
{
    // ...
}
```

## 服务接口

### IAirAppLogger

日志记录器。

```csharp
public interface IAirAppLogger
{
    void Debug(string message);
    void Info(string message);
    void Warn(string message);
    void Warn(string message, Exception exception);
    void Error(string message);
    void Error(string message, Exception exception);
}
```

### IAirAppMessageBus

消息总线。

```csharp
public interface IAirAppMessageBus
{
    void Publish(string topic, object? payload = null);
    IDisposable Subscribe(string topic, Action<object?> handler);
    IDisposable Subscribe<T>(string topic, Action<T?> handler);
}
```

### IAirAppAppearanceContext

外观上下文。

```csharp
public interface IAirAppAppearanceContext
{
    AirAppAppearanceSnapshot CurrentSnapshot { get; }
    IDisposable SubscribeToChanges(Action<AirAppAppearanceSnapshot> handler);
}
```

## 常量

### AirAppSdkInfo

SDK 信息。

```csharp
public static class AirAppSdkInfo
{
    public const string SdkVersion = "6.0.0";
    public const string ApiVersion = "6.0.0";
    public const string ManifestFileName = "airapp.json";
    public const string PackageExtension = ".laapp";
    public static string DisplayName => "LanMountainDesktop AirApp SDK";
}
```

---

更多示例和详细说明，请参考 [快速开始指南](./QUICK_START.md) 和 [最佳实践](./BEST_PRACTICES.md)。
