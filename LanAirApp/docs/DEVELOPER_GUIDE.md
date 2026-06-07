# AirApp SDK 开发指南

完整的 AirApp 开发指南，从入门到精通。

## 目录

1. [快速开始](#快速开始)
2. [开发工具](#开发工具)
3. [项目结构](#项目结构)
4. [核心概念](#核心概念)
5. [组件开发](#组件开发)
6. [窗口开发](#窗口开发)
7. [数据持久化](#数据持久化)
8. [主题和外观](#主题和外观)
9. [调试技巧](#调试技巧)
10. [最佳实践](#最佳实践)

## 快速开始

### 环境要求

- .NET SDK 10.0+
- 任意代码编辑器（推荐 VS Code、Visual Studio、Rider）
- LanMountainDesktop 运行环境（用于测试）

### 5分钟创建第一个 AirApp

```bash
# 1. 安装项目模板
dotnet new install LanMountainDesktop.AirAppTemplate

# 2. 创建项目
dotnet new lmd-airapp-component -n MyFirstApp
cd MyFirstApp

# 3. 查看生成的文件
ls
# MyAirApp.cs - 入口点
# MyWidget.cs - 组件实现
# airapp.json - 清单文件
# *.csproj - 项目文件

# 4. 运行开发服务器（支持热重载）
dotnet tool install -g LanMountainDesktop.AirAppDevServer
airapp-dev dev

# 5. 构建
dotnet build

# 6. 安装测试
# 将 bin/Debug/net10.0/MyFirstApp.laapp 复制到阑山桌面插件目录
```

## 开发工具

### AirApp 开发服务器

提供热重载、自动编译等功能：

```bash
# 安装开发工具
dotnet tool install -g LanMountainDesktop.AirAppDevServer

# 启动开发服务器（当前目录）
airapp-dev dev

# 指定项目路径
airapp-dev dev --project path/to/project

# 显示详细日志
airapp-dev dev --verbose
```

**功能：**
- ✅ 文件监视（.cs, .axaml, .json, .csproj）
- ✅ 自动重新编译
- ✅ 热重载支持
- ✅ 实时错误提示

### 预览工具

无需安装到宿主即可预览组件：

```bash
# 列出所有可预览项
airapp-dev preview

# 预览指定组件
airapp-dev preview --component my-widget

# 预览指定窗口
airapp-dev preview --window my-window
```

### 打包工具

将项目打包为 .laapp 文件：

```bash
# 自动打包到 bin/Release
airapp-dev package

# 指定输出路径
airapp-dev package --output path/to/output.laapp
```

## 项目结构

标准 AirApp 项目结构：

```
MyAirApp/
├── MyAirApp.cs              # 入口点
├── MyWidget.cs              # 组件实现
├── MyWindow.cs              # 窗口实现（可选）
├── Views/                   # AXAML 视图文件（可选）
│   ├── MyWidget.axaml
│   └── MyWidget.axaml.cs
├── Models/                  # 数据模型
├── Services/                # 服务类
├── airapp.json              # 清单文件
├── MyAirApp.csproj          # 项目文件
└── README.md                # 说明文档
```

## 核心概念

### 1. AirApp 生命周期

```csharp
[AirAppEntrance]
public class MyAirApp : AirAppBase
{
    // 阶段1: 初始化 - 注册服务和组件
    public override void Initialize(HostBuilderContext context, IServiceCollection services)
    {
        services.AddAirAppComponent<MyWidget>("widget-id", "组件名");
        services.AddSingleton<IMyService, MyService>();
    }

    // 阶段2: 启动 - 访问运行时服务
    public override Task OnStartedAsync(IAirAppRuntimeContext context)
    {
        var dataDir = context.DataDirectory;  // 获取数据目录
        context.Logger.Info("AirApp 已启动");
        return Task.CompletedTask;
    }

    // 阶段3: 停止 - 清理资源
    public override Task OnStoppingAsync()
    {
        // 保存状态、释放资源
        return Task.CompletedTask;
    }
}
```

### 2. 清单文件 (airapp.json)

```json
{
  "id": "com.example.myapp",           // 唯一标识符（反向域名）
  "name": "My AirApp",                 // 显示名称
  "version": "1.0.0",                  // 版本号
  "apiVersion": "1.0.0",               // API 版本
  "author": "Your Name",               // 作者
  "description": "描述",               // 描述
  "entranceAssembly": "MyApp.dll",     // 入口程序集
  "runtime": {
    "mode": "in-process",              // 运行模式
    "capabilities": [                  // 能力声明
      "desktop-component",
      "window"
    ]
  },
  "components": [                      // 组件声明
    {
      "id": "my-widget",
      "name": "我的组件",
      "defaultWidth": 2,
      "defaultHeight": 2
    }
  ],
  "windows": [                         // 窗口声明
    {
      "id": "my-window",
      "name": "我的窗口",
      "defaultWidth": 800,
      "defaultHeight": 600
    }
  ]
}
```

### 3. 运行时上下文

```csharp
public override Task OnStartedAsync(IAirAppRuntimeContext context)
{
    // 基本信息
    var appId = context.AirAppId;
    var appName = context.AirAppName;
    
    // 目录
    var dataDir = context.DataDirectory;      // 持久化数据
    var cacheDir = context.CacheDirectory;    // 临时缓存
    
    // 服务
    var myService = context.Services.GetService<IMyService>();
    
    // 日志
    context.Logger.Info("信息日志");
    context.Logger.Warn("警告日志");
    context.Logger.Error("错误日志", exception);
    
    // 消息总线
    context.MessageBus.Publish("topic", data);
    var sub = context.MessageBus.Subscribe("topic", HandleMessage);
    
    // 窗口管理
    await context.OpenWindowAsync("window-id");
    context.CloseWindow("window-id");
    
    // 外观
    var appearance = context.Appearance.CurrentSnapshot;
    var isDark = appearance.IsDarkMode;
    
    return Task.CompletedTask;
}
```

## 组件开发

### 基础组件

```csharp
public class SimpleWidget : AirAppWidgetBase
{
    public SimpleWidget()
    {
        Content = new TextBlock
        {
            Text = "Hello AirApp!",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
    }

    protected override void OnAttachedCore()
    {
        // 组件被添加到桌面时调用
        Context.Logger.Info($"组件已添加: {Context.PlacementId}");
    }

    protected override void OnDetachedCore()
    {
        // 组件从桌面移除时调用
        Context.Logger.Info("组件已移除");
    }
}
```

### 带定时器的组件

```csharp
public class ClockWidget : AirAppWidgetBase
{
    private readonly TextBlock _timeText;
    private readonly DispatcherTimer _timer;

    public ClockWidget()
    {
        _timeText = new TextBlock
        {
            FontSize = 32,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        Content = _timeText;

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += (s, e) => UpdateTime();
    }

    protected override void OnAttachedCore()
    {
        UpdateTime();
        _timer.Start();
    }

    protected override void OnDetachedCore()
    {
        _timer.Stop();
    }

    private void UpdateTime()
    {
        _timeText.Text = DateTime.Now.ToString("HH:mm:ss");
    }
}
```

### 响应主题变化

```csharp
protected override void OnAppearanceChangedCore(AirAppAppearanceSnapshot snapshot)
{
    // 获取主题信息
    var isDark = snapshot.IsDarkMode;
    var accent = snapshot.AccentColor;
    var foreground = snapshot.ForegroundColor;
    var background = snapshot.BackgroundColor;

    // 更新 UI
    _titleText.Foreground = new SolidColorBrush(foreground);
    _valueText.Foreground = new SolidColorBrush(accent);
    Background = new SolidColorBrush(background);
}
```

### 异步数据加载

```csharp
public class DataWidget : AirAppWidgetBase
{
    private readonly TextBlock _statusText;
    private bool _isLoaded;

    protected override void OnAttachedCore()
    {
        if (!_isLoaded)
        {
            _ = LoadDataAsync();
        }
    }

    private async Task LoadDataAsync()
    {
        try
        {
            _statusText.Text = "加载中...";
            
            var data = await FetchDataFromApiAsync();
            
            _statusText.Text = $"数据: {data}";
            _isLoaded = true;
        }
        catch (Exception ex)
        {
            Context.Logger.Error("加载数据失败", ex);
            _statusText.Text = "加载失败";
        }
    }
}
```

## 窗口开发

### 基础窗口

```csharp
public class MyWindow : AirAppWindowBase
{
    // 配置窗口
    public override AirAppWindowDescriptor Descriptor => new()
    {
        Title = "我的窗口",
        Width = 800,
        Height = 600,
        MinWidth = 400,
        MinHeight = 300,
        ChromeMode = AirAppWindowChromeMode.Standard,
        CanResize = true,
        ShowInTaskbar = true
    };

    public MyWindow()
    {
        Content = new TextBlock { Text = "窗口内容" };
    }

    // 窗口打开前（异步初始化）
    public override async Task OnWindowOpeningAsync()
    {
        await LoadDataAsync();
    }

    // 窗口打开后
    public override void OnWindowOpened()
    {
        // 设置焦点等
    }

    // 窗口关闭前（可以取消）
    public override void OnWindowClosing(WindowClosingEventArgs e)
    {
        if (HasUnsavedChanges())
        {
            e.Cancel = true;  // 取消关闭
            ShowSaveDialog();
        }
    }

    // 窗口关闭后
    public override void OnWindowClosed()
    {
        // 清理资源
    }
}
```

### 不同窗口样式

```csharp
// 标准窗口
ChromeMode = AirAppWindowChromeMode.Standard

// 无边框窗口
ChromeMode = AirAppWindowChromeMode.Borderless

// 全屏窗口
ChromeMode = AirAppWindowChromeMode.FullScreen

// 工具窗口
ChromeMode = AirAppWindowChromeMode.Tool
```

## 数据持久化

### 使用数据目录

```csharp
public override async Task OnStartedAsync(IAirAppRuntimeContext context)
{
    var dataDir = context.DataDirectory;
    var settingsFile = Path.Combine(dataDir, "settings.json");

    // 加载设置
    if (File.Exists(settingsFile))
    {
        var json = await File.ReadAllTextAsync(settingsFile);
        _settings = JsonSerializer.Deserialize<MySettings>(json);
    }
}

private async Task SaveSettingsAsync()
{
    var dataDir = RuntimeContext.DataDirectory;
    var settingsFile = Path.Combine(dataDir, "settings.json");
    
    var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions
    {
        WriteIndented = true
    });
    
    await File.WriteAllTextAsync(settingsFile, json);
}
```

### 使用缓存目录

```csharp
// 缓存临时数据
var cacheDir = context.CacheDirectory;
var cacheFile = Path.Combine(cacheDir, "temp_data.json");

// 缓存可以随时被清理，不要存储重要数据
```

## 主题和外观

### 获取当前主题

```csharp
var appearance = Context.Appearance.CurrentSnapshot;

if (appearance.IsDarkMode)
{
    // 深色模式
}
else
{
    // 浅色模式
}
```

### 订阅主题变化

```csharp
protected override void OnAttachedCore()
{
    // 方法1: 使用 OnAppearanceChangedCore
    // 自动调用，无需订阅
    
    // 方法2: 手动订阅
    var subscription = Context.Appearance.SubscribeToChanges(snapshot =>
    {
        UpdateTheme(snapshot);
    });
    
    // 保存 subscription 以便取消订阅
}
```

### 使用系统颜色

```csharp
protected override void OnAppearanceChangedCore(AirAppAppearanceSnapshot snapshot)
{
    // 使用系统主题色
    _titleText.Foreground = new SolidColorBrush(snapshot.AccentColor);
    _bodyText.Foreground = new SolidColorBrush(snapshot.ForegroundColor);
    Background = new SolidColorBrush(snapshot.BackgroundColor);
    BorderBrush = new SolidColorBrush(snapshot.BorderColor);
    
    // 使用圆角
    var cornerRadius = snapshot.CornerRadiusPreset switch
    {
        AirAppCornerRadiusPreset.Small => new CornerRadius(4),
        AirAppCornerRadiusPreset.Medium => new CornerRadius(8),
        AirAppCornerRadiusPreset.Large => new CornerRadius(12),
        _ => new CornerRadius(0)
    };
    
    // 使用玻璃效果
    var glassOpacity = snapshot.GlassOpacity;
}
```

## 调试技巧

### 使用日志

```csharp
Context.Logger.Debug("调试信息");
Context.Logger.Info("一般信息");
Context.Logger.Warn("警告信息");
Context.Logger.Error("错误信息", exception);
```

### 附加调试器

1. 启动阑山桌面
2. 在 IDE 中附加到进程
3. 在代码中设置断点
4. 触发功能

### 开发模式测试

```bash
# 在开发服务器中测试（支持热重载）
airapp-dev dev

# 在宿主中测试
dotnet run --project LanMountainDesktop.csproj -- \
    --debug-airapp path/to/your/bin/Debug/net10.0
```

## 最佳实践

### 1. 性能优化

```csharp
// ✅ 好：使用定时器批量更新
private void StartUpdateTimer()
{
    var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
    timer.Tick += UpdateUI;
    timer.Start();
}

// ❌ 差：频繁更新 UI
private void OnDataChanged(object? sender, EventArgs e)
{
    UpdateUI();  // 可能每秒调用数百次
}
```

### 2. 资源管理

```csharp
// ✅ 好：在 OnDetachedCore 中释放资源
protected override void OnDetachedCore()
{
    _timer?.Stop();
    _httpClient?.Dispose();
    _subscription?.Dispose();
}

// ❌ 差：忘记释放资源
```

### 3. 错误处理

```csharp
// ✅ 好：捕获并记录异常
try
{
    await LoadDataAsync();
}
catch (Exception ex)
{
    Context.Logger.Error("加载失败", ex);
    ShowErrorMessage("数据加载失败，请重试");
}

// ❌ 差：忽略异常
await LoadDataAsync();  // 可能抛出异常
```

### 4. 异步编程

```csharp
// ✅ 好：使用 async/await
protected override void OnAttachedCore()
{
    _ = LoadDataAsync();  // 不阻塞 UI
}

private async Task LoadDataAsync()
{
    var data = await FetchDataAsync();
    UpdateUI(data);
}

// ❌ 差：阻塞 UI 线程
protected override void OnAttachedCore()
{
    var data = FetchDataAsync().Result;  // 阻塞！
}
```

### 5. UI 线程

```csharp
// ✅ 好：在 UI 线程更新 UI
private async Task UpdateDataAsync()
{
    var data = await FetchDataAsync();
    
    await Dispatcher.UIThread.InvokeAsync(() =>
    {
        _text.Text = data;
    });
}

// ❌ 差：在后台线程更新 UI
private async Task UpdateDataAsync()
{
    var data = await FetchDataAsync();
    _text.Text = data;  // 可能崩溃！
}
```

---

更多示例请参考 `samples/` 目录。
