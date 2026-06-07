# AirApp 开发工具使用指南

LanMountainDesktop AirApp 开发工具集的完整使用说明。

## 工具概览

| 工具 | 用途 | 命令 |
|------|------|------|
| **开发服务器** | 热重载、自动编译 | `airapp-dev dev` |
| **预览工具** | 无需安装预览组件 | `airapp-dev preview` |
| **打包工具** | 生成 .laapp 文件 | `airapp-dev package` |

## 安装

### 全局安装

```bash
dotnet tool install -g LanMountainDesktop.AirAppDevServer
```

### 更新

```bash
dotnet tool update -g LanMountainDesktop.AirAppDevServer
```

### 卸载

```bash
dotnet tool uninstall -g LanMountainDesktop.AirAppDevServer
```

## 开发服务器

### 基本用法

```bash
# 在项目目录中启动
cd MyAirApp
airapp-dev dev

# 指定项目路径
airapp-dev dev --project path/to/project

# 自定义端口
airapp-dev dev --port 5001

# 显示详细日志
airapp-dev dev --verbose
```

### 功能特性

#### ✅ 文件监视

自动监视以下文件类型的变化：
- `.cs` - C# 源代码
- `.axaml` - Avalonia XAML 文件
- `.json` - 配置文件
- `.csproj` - 项目文件

**自动忽略的目录：**
- `bin/`
- `obj/`
- `.vs/`
- `.git/`

#### ✅ 自动重新编译

检测到文件变化后：
1. 等待 300ms（确保文件写入完成）
2. 执行 `dotnet build`
3. 显示构建结果
4. 通知热重载生效

#### ✅ 热重载

构建成功后，已加载的 AirApp 会自动重新加载新版本，无需手动重启。

### 工作流程

```
编辑代码 → 保存文件 → 自动编译 → 热重载 → 立即看到效果
    ↑                                              ↓
    └──────────────── 继续开发 ←───────────────────┘
```

### 示例输出

```
🔨 初始构建中...
✅ 初始构建成功

👁️ 文件监视已启动，等待更改...

✅ 开发服务器已启动
🌐 预览地址: http://localhost:5000

按 Ctrl+C 停止服务器...

📝 检测到文件更改: MyWidget.cs
🔄 重新构建中...
✅ 重新构建成功 [14:32:18]
♻️ 热重载已生效
```

### 错误处理

如果构建失败，服务器会显示详细的错误信息：

```
🔄 重新构建中...
❌ 重新构建失败 [14:35:22]
❌ 构建错误:
MyWidget.cs(25,9): error CS1002: ; expected
```

修复错误后保存文件，会自动重新构建。

## 预览工具

### 列出可预览项

```bash
# 显示所有组件和窗口
airapp-dev preview
```

输出示例：

```
📋 加载 AirApp 清单...
✅ AirApp: 我的时钟

📦 可用组件:
  - clock: 时钟

🪟 可用窗口:
  - settings: 设置窗口

使用以下命令预览:
  airapp-dev preview --component <component-id>
  airapp-dev preview --window <window-id>
```

### 预览组件

```bash
# 预览指定组件
airapp-dev preview --component clock

# 简写
airapp-dev preview -c clock
```

### 预览窗口

```bash
# 预览指定窗口
airapp-dev preview --window settings

# 简写
airapp-dev preview -w settings
```

### 工作原理

预览工具会：
1. 查找项目的构建输出
2. 读取 `airapp.json` 清单
3. 启动预览宿主应用
4. 加载并显示指定的组件或窗口

**注意：** 预览功能需要配合 LanMountainDesktop 宿主运行。如果预览功能不可用，请使用：

```bash
dotnet run --project path/to/LanMountainDesktop.csproj -- \
    --debug-airapp path/to/your/bin/Debug/net10.0
```

## 打包工具

### 基本用法

```bash
# 自动打包到 bin/Release/net10.0/
cd MyAirApp
airapp-dev package
```

输出示例：

```
🔨 构建项目...
📁 输出目录: D:\MyAirApp\bin\Release\net10.0
📦 打包到: D:\MyAirApp\bin\Release\net10.0\MyAirApp.laapp
📄 打包 45 个文件...
✅ 包大小: 256 KB

✅ 打包完成: D:\MyAirApp\bin\Release\net10.0\MyAirApp.laapp
```

### 指定输出路径

```bash
# 指定输出文件
airapp-dev package --output ~/Desktop/MyAirApp.laapp

# 指定输出目录（自动命名）
airapp-dev package --output ~/Desktop/

# 简写
airapp-dev package -o output.laapp
```

### 打包内容

`.laapp` 包是一个 ZIP 压缩文件，包含：
- 所有程序集（.dll）
- `airapp.json` 清单
- 所有依赖项
- 资源文件

**自动排除：**
- `.pdb` 调试符号文件
- 已存在的 `.laapp` 文件

### 手动打包

如果不使用工具，也可以手动打包：

```bash
# 1. 构建发布版本
dotnet build -c Release

# 2. 进入输出目录
cd bin/Release/net10.0

# 3. 创建 ZIP 文件
# Windows PowerShell:
Compress-Archive -Path * -DestinationPath MyAirApp.laapp

# Linux/macOS:
zip -r MyAirApp.laapp *
```

## 完整开发工作流

### 1. 创建新项目

```bash
dotnet new lmd-airapp-component -n MyWidget
cd MyWidget
```

### 2. 启动开发服务器

```bash
airapp-dev dev
```

### 3. 开发迭代

编辑 `MyWidget.cs`：

```csharp
public MyWidget()
{
    Content = new TextBlock
    {
        Text = "Hello, World!",  // 修改这里
        FontSize = 32
    };
}
```

保存文件，开发服务器会自动：
- 重新编译
- 热重载
- 显示新内容

### 4. 测试

在另一个终端中：

```bash
# 预览组件
airapp-dev preview --component my-widget

# 或在宿主中测试
dotnet run --project path/to/LanMountainDesktop.csproj -- \
    --debug-airapp path/to/MyWidget/bin/Debug/net10.0
```

### 5. 打包发布

```bash
# 停止开发服务器（Ctrl+C）

# 打包
airapp-dev package

# 生成的文件：
# bin/Release/net10.0/MyWidget.laapp
```

### 6. 安装测试

将 `.laapp` 文件复制到阑山桌面插件目录，或通过 AirApp 市场安装。

## 常见问题

### Q: 开发服务器无法启动

**A:** 检查：
1. 是否在正确的项目目录中
2. 是否存在 `.csproj` 文件
3. 是否安装了 .NET SDK 10.0+

```bash
# 检查 .NET 版本
dotnet --version
```

### Q: 热重载不生效

**A:** 可能原因：
1. 构建失败 - 查看错误信息
2. 文件在忽略列表中 - 只监视 .cs, .axaml, .json, .csproj
3. 修改了结构性代码 - 需要完全重启

### Q: 预览功能不可用

**A:** 预览功能需要 LanMountainDesktop 宿主。暂时使用：

```bash
dotnet run --project LanMountainDesktop.csproj -- \
    --debug-airapp <path-to-output>
```

### Q: 打包失败

**A:** 检查：
1. 项目是否成功构建
2. 输出目录是否存在
3. 是否有写入权限

```bash
# 手动构建测试
dotnet build -c Release
```

### Q: 构建很慢

**A:** 优化建议：
1. 排除不必要的文件监视
2. 使用 SSD 硬盘
3. 关闭杀毒软件实时扫描（开发时）
4. 使用增量构建

## 高级用法

### 自定义构建配置

编辑 `.csproj` 文件：

```xml
<PropertyGroup>
  <!-- 加速增量构建 -->
  <Deterministic>true</Deterministic>
  
  <!-- 禁用不必要的分析器 -->
  <EnableNETAnalyzers>false</EnableNETAnalyzers>
  
  <!-- 自定义输出路径 -->
  <OutputPath>bin\$(Configuration)\</OutputPath>
</PropertyGroup>
```

### 集成到 CI/CD

```bash
# 在 CI 环境中打包
dotnet tool install -g LanMountainDesktop.AirAppDevServer
airapp-dev package --output ./artifacts/

# 或使用 MSBuild
dotnet build -c Release
cd bin/Release/net10.0
zip -r MyAirApp.laapp *
```

### 与 VS Code 集成

创建 `.vscode/tasks.json`：

```json
{
  "version": "2.0.0",
  "tasks": [
    {
      "label": "开发服务器",
      "type": "shell",
      "command": "airapp-dev dev",
      "isBackground": true,
      "problemMatcher": []
    },
    {
      "label": "打包",
      "type": "shell",
      "command": "airapp-dev package",
      "group": {
        "kind": "build",
        "isDefault": true
      }
    }
  ]
}
```

按 `Ctrl+Shift+B` 即可快速打包。

## 性能提示

### 减少重新构建时间

1. **仅修改方法体**：不修改类结构，构建更快
2. **使用 partial 类**：分离 UI 和逻辑
3. **避免修改 .csproj**：触发完整重新构建
4. **使用本地 NuGet 缓存**

### 优化文件监视

如果项目很大，可以禁用某些目录的监视。编辑开发服务器源码或提交 Issue 请求功能。

## 获取帮助

```bash
# 查看帮助
airapp-dev --help
airapp-dev dev --help
airapp-dev preview --help
airapp-dev package --help
```

## 相关资源

- [开发指南](./DEVELOPER_GUIDE.md)
- [API 参考](./API_REFERENCE.md)
- [示例项目](../samples/)
- [GitHub Issues](https://github.com/LanMountain/LanMountainDesktop/issues)

---

祝开发愉快！🚀
