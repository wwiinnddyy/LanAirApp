# Air App SDK 设计文档

## 概述

Air App SDK 是阑山桌面的下一代应用开发框架，将原有的 Plugin SDK 全面升级为统一的轻应用开发体系。通过 Air App SDK，开发者可以：

- 开发桌面组件（Desktop Components）
- 开发窗口化应用（Window Apps）
- 开发后台服务（Background Services）
- 享受现代化的开发体验（预览、热重载、调试）

## 核心理念

### 1. 统一体验
开发者开发的 AirApp 与系统内置应用享有完全相同的能力和体验：
- 相同的 UI 组件库
- 相同的主题系统
- 相同的生命周期管理
- 相同的 IPC 通信机制

### 2. 开发者友好
提供完整的现代开发工具链：
- 项目模板快速启动
- 实时预览和热重载
- 调试和日志支持
- 完整的 API 文档和示例

### 3. 灵活架构
支持多种应用形态：
- **组件模式**：嵌入桌面的小部件
- **窗口模式**：独立窗口应用
- **混合模式**：同时提供组件和窗口
- **后台模式**：纯后台服务

## 架构设计

### 核心概念

#### AirApp
AirApp 是阑山桌面生态中的应用单元，它可以：
- 注册桌面组件
- 创建窗口
- 提供服务
- 响应系统事件

#### AirApp Manifest
每个 AirApp 都有一个清单文件 `airapp.json`，描述应用的元数据：

```json
{
  "id": "com.example.myapp",
  "name": "My Air App",
  "version": "1.0.0",
  "apiVersion": "6.0.0",
  "author": "Your Name",
  "description": "A sample Air App",
  "entranceAssembly": "MyAirApp.dll",
  "runtime": {
    "mode": "in-process",
    "capabilities": ["desktop-component", "window", "background"]
  },
  "components": [
    {
      "id": "my-widget",
      "name": "My Widget",
      "type": "desktop-component",
      "defaultWidth": 2,
      "defaultHeight": 2
    }
  ],
  "windows": [
    {
      "id": "main-window",
      "name": "Main Window",
      "defaultWidth": 800,
      "defaultHeight": 600
    }
  ],
  "sharedContracts": []
}
```

### API 层次

#### 1. AirAppBase - 应用入口

```csharp
public abstract class AirAppBase : IAirApp
{
    // 核心生命周期
    public virtual void Initialize(HostBuilderContext context, IServiceCollection services) { }
    public virtual Task OnStartedAsync(IAirAppRuntimeContext context) { }
    public virtual Task OnStoppingAsync() { }
    
    // 运行时上下文
    protected IAirAppRuntimeContext RuntimeContext { get; }
    
    // 快捷注册方法
    protected void RegisterComponent<TWidget>(string id, string name) where TWidget : IAirAppWidget;
    protected void RegisterWindow<TWindow>(string id, string name) where TWindow : IAirAppWindow;
    protected void RegisterService<TService, TImplementation>() where TImplementation : TService;
}
```

#### 2. IAirAppWidget - 桌面组件

```csharp
public interface IAirAppWidget
{
    // 组件上下文
    IAirAppComponentContext Context { get; set; }
    
    // 生命周期
    void OnAttached();
    void OnDetached();
    
    // 主题和外观
    void OnAppearanceChanged(AirAppAppearanceSnapshot snapshot);
}

public abstract class AirAppWidgetBase : UserControl, IAirAppWidget
{
    public IAirAppComponentContext Context { get; set; }
    
    protected virtual void OnAttached() { }
    protected virtual void OnDetached() { }
    protected virtual void OnAppearanceChanged(AirAppAppearanceSnapshot snapshot) { }
}
```

#### 3. IAirAppWindow - 窗口应用

```csharp
public interface IAirAppWindow
{
    // 窗口配置
    AirAppWindowDescriptor Descriptor { get; }
    
    // 生命周期
    Task OnWindowOpeningAsync();
    void OnWindowOpened();
    void OnWindowClosing(WindowClosingEventArgs e);
    void OnWindowClosed();
}

public abstract class AirAppWindowBase : Window, IAirAppWindow
{
    protected AirAppWindowBase()
    {
        // 自动应用 AirApp 窗口样式
        ApplyAirAppWindowTheme();
    }
    
    public virtual AirAppWindowDescriptor Descriptor => new()
    {
        Width = 800,
        Height = 600,
        ChromeMode = AirAppWindowChromeMode.Standard,
        CanResize = true,
        ShowInTaskbar = true
    };
}
```

#### 4. IAirAppRuntimeContext - 运行时上下文

```csharp
public interface IAirAppRuntimeContext
{
    // 应用信息
    string AirAppId { get; }
    string AirAppName { get; }
    string DataDirectory { get; }
    string CacheDirectory { get; }
    
    // 宿主服务
    IServiceProvider Services { get; }
    
    // 生命周期
    IHostApplicationLifetime Lifetime { get; }
    
    // 消息总线
    IAirAppMessageBus MessageBus { get; }
    
    // 窗口管理
    Task<IAirAppWindow> OpenWindowAsync(string windowId);
    void CloseWindow(string windowId);
    
    // 外观管理
    IAirAppAppearanceContext Appearance { get; }
    
    // 日志
    IAirAppLogger Logger { get; }
}
```

### 运行时模式

#### In-Process 模式
AirApp 运行在宿主进程中，性能最优，但崩溃会影响宿主。

```json
{
  "runtime": {
    "mode": "in-process"
  }
}
```

#### Isolated-Background 模式
AirApp 运行在独立的后台进程中，通过 IPC 通信。

```json
{
  "runtime": {
    "mode": "isolated-background"
  }
}
```

#### Isolated-Window 模式
AirApp 运行在独立的窗口进程中，完全隔离。

```json
{
  "runtime": {
    "mode": "isolated-window"
  }
}
```

## 开发工具链

### 1. 项目模板

```bash
# 安装 AirApp 模板
dotnet new install LanMountainDesktop.AirAppTemplate

# 创建桌面组件 AirApp
dotnet new lmd-airapp -n MyWidget --type component

# 创建窗口 AirApp
dotnet new lmd-airapp -n MyApp --type window

# 创建混合 AirApp
dotnet new lmd-airapp -n MyApp --type hybrid
```

### 2. 开发服务器

```bash
# 启动开发服务器（带热重载）
dotnet run --project MyAirApp.csproj -- --dev

# 启动预览窗口
dotnet run --project MyAirApp.csproj -- --preview
```

开发服务器功能：
- 文件监视和自动重新编译
- 热重载（组件级别）
- 实时日志输出
- 调试断点支持

### 3. 打包工具

```bash
# 打包 AirApp
dotnet build MyAirApp.csproj -c Release

# 输出: bin/Release/net10.0/MyAirApp.laapp
```

### 4. 调试工具

```bash
# 在宿主中调试
dotnet run --project LanMountainDesktop.csproj -- --debug-airapp path/to/MyAirApp

# 查看 AirApp 日志
dotnet run --project LanMountainDesktop.csproj -- --airapp-logs MyAirApp
```

## 向后兼容

### Plugin SDK 迁移路径

现有的 Plugin SDK (v5.0) 应用可以通过以下方式迁移到 Air App SDK (v6.0)：

1. **清单文件更新**：`plugin.json` → `airapp.json`
2. **命名空间更新**：`PluginBase` → `AirAppBase`
3. **API 更新**：使用新的 AirApp API

提供自动迁移工具：

```bash
dotnet tool install -g LanMountainDesktop.AirAppMigrator
lmd-airapp-migrate path/to/plugin/project
```

### 兼容层

在 Air App SDK v6.0 中保留兼容层，支持加载 v5.0 Plugin：

```csharp
// 在 AirApp 运行时中
public class PluginCompatibilityAdapter
{
    public static AirAppBase WrapPlugin(PluginBase plugin)
    {
        return new PluginToAirAppAdapter(plugin);
    }
}
```

## AirApp 市场

### 市场索引结构

```json
{
  "version": "2.0.0",
  "airApps": [
    {
      "id": "com.example.myapp",
      "name": "My Air App",
      "author": "Your Name",
      "description": "A sample Air App",
      "version": "1.0.0",
      "apiVersion": "6.0.0",
      "tags": ["widget", "productivity"],
      "downloadUrl": "https://example.com/myapp-1.0.0.laapp",
      "icon": "https://example.com/icon.png",
      "screenshots": ["https://example.com/screenshot1.png"],
      "capabilities": ["desktop-component", "window"],
      "minHostVersion": "2.0.0"
    }
  ]
}
```

### 应用内市场

用户可以在阑山桌面内：
- 浏览 AirApp 市场
- 搜索和筛选 AirApp
- 一键安装和更新
- 管理已安装的 AirApp
- 查看 AirApp 评分和评论

## 安全和沙箱

### 权限系统

AirApp 需要声明所需的权限：

```json
{
  "permissions": [
    "filesystem.read",
    "filesystem.write",
    "network.http",
    "system.clipboard"
  ]
}
```

### 沙箱隔离

Isolated 模式的 AirApp 运行在受限环境中：
- 文件系统访问限制
- 网络访问限制
- 进程创建限制
- 系统 API 访问限制

## 性能优化

### 懒加载
AirApp 在需要时才加载：
- 组件首次显示时加载
- 窗口首次打开时加载
- 后台服务按需启动

### 资源管理
- 自动卸载不活跃的 AirApp
- 内存使用监控和限制
- CPU 使用监控和限制

## 开发者资源

### 文档
- 快速开始指南
- API 参考文档
- 最佳实践
- 迁移指南

### 示例项目
- 简单时钟组件
- 天气小部件
- 笔记应用
- 系统监控工具

### 社区支持
- GitHub Discussions
- Discord 社区
- 示例代码仓库
- 视频教程

## 技术栈

- **.NET 10**：运行时和编译器
- **Avalonia UI 11.2**：跨平台 UI 框架
- **FluentAvalonia**：Fluent Design 组件
- **System.Text.Json**：JSON 序列化
- **Named Pipes**：IPC 通信
- **FileSystemWatcher**：热重载支持

## 路线图

### v6.0 (当前版本)
- [x] 核心 AirApp API
- [x] 组件开发支持
- [x] 窗口开发支持
- [ ] 开发工具链
- [ ] 项目模板
- [ ] 基础文档

### v6.1 (计划中)
- [ ] 热重载支持
- [ ] 调试工具增强
- [ ] 性能分析工具
- [ ] 更多示例项目

### v6.2 (计划中)
- [ ] 跨 AirApp 通信
- [ ] 插件依赖管理
- [ ] AirApp 商店 API
- [ ] 自动更新机制

## 总结

Air App SDK v6.0 是阑山桌面插件系统的全面升级，提供：

1. **统一的开发体验**：开发者和用户享受一致的应用体验
2. **现代化的工具链**：预览、热重载、调试等现代开发工具
3. **灵活的架构**：支持组件、窗口、后台服务等多种形态
4. **完善的生态**：市场、文档、社区全方位支持

通过 Air App SDK，开发者可以轻松构建高质量的阑山桌面应用，为用户提供丰富的功能扩展。
