# LanAirApp

## 中文

`LanAirApp` 是 LanMountainDesktop 插件生态的对外仓库，只负责市场、文档、工具和模板，不承载桌面宿主运行时，也不作为 SDK 或官方示例的权威源码仓。

### 本仓负责

- `airappmarket/`：官方市场索引、Schema、校验器和静态资源
- `docs/`：插件开发与打包文档
- `tools/`：打包、验证和辅助工具
- `samples/`：镜像样例与模板
- `standards/`：插件清单和打包约定模板
- `releases/`：临时暂存与本地调试产物

### 本仓不负责

- 桌面宿主运行时
- 插件 SDK 的权威接口定义
- 官方示例插件的权威发布源

### 权威指向

- 宿主与 SDK 权威仓：`LanMountainDesktop`
- 官方示例插件权威仓：`LanMountainDesktop.SamplePlugin`
- `LanMountainDesktop.PluginSdk/` 是不参与解决方案构建的历史 v4 快照，不得作为 v5 依赖
- v5 示例、打包器和兼容测试统一消费由宿主仓源码生成的 `LanMountainDesktop.PluginSdk 5.0.0` 本地包
- `LanMountainDesktop.SharedContracts.SampleClock/` 与 `samples/` 仅作为契约/模板材料

### 构建 v5 示例与工具

PluginSdk 5.0.0 尚未发布到 NuGet.org。首次还原前必须从相邻的宿主仓生成本地包：

```powershell
.\scripts\Initialize-PluginSdkFeed.ps1
dotnet restore .\LanAirApp.slnx --configfile .\NuGet.Config --force --no-cache
dotnet build .\LanAirApp.slnx -c Release --no-restore
dotnet test .\LanMountainDesktop.PluginSdk.Tests\LanMountainDesktop.PluginSdk.Tests.csproj -c Release --no-build
```

## English

`LanAirApp` is the public ecosystem repository for LanMountainDesktop. It owns market metadata, documentation, tools, and templates only. It does not own the desktop runtime, the authoritative SDK surface, or the official sample release source.

### This repository owns

- `airappmarket/`: the official market link index, schema, validator, and static assets
- `docs/`: plugin development and packaging documentation
- `tools/`: packaging, validation, and helper tools
- `samples/`: mirrored samples and templates
- `standards/`: manifest and packaging conventions
- `releases/`: temporary staging and local debugging output

### This repository does not own

- the desktop host runtime
- the authoritative plugin SDK interface definitions
- the official sample plugin release source

### Authoritative pointers

- Host and SDK source of truth: `LanMountainDesktop`
- Official sample plugin source of truth: `LanMountainDesktop.SamplePlugin`
- `LanMountainDesktop.PluginSdk/` is a historical v4 snapshot excluded from the solution and must not be used as a v5 dependency
- v5 samples, tooling, and compatibility tests consume the local `LanMountainDesktop.PluginSdk 5.0.0` package built from the host repository
- `LanMountainDesktop.SharedContracts.SampleClock/` and `samples/` remain contract/template material

### Building the v5 sample and tooling

PluginSdk 5.0.0 is not published on NuGet.org yet. Generate the local feed from a sibling host checkout before the first restore:

```powershell
.\scripts\Initialize-PluginSdkFeed.ps1
dotnet restore .\LanAirApp.slnx --configfile .\NuGet.Config --force --no-cache
dotnet build .\LanAirApp.slnx -c Release --no-restore
dotnet test .\LanMountainDesktop.PluginSdk.Tests\LanMountainDesktop.PluginSdk.Tests.csproj -c Release --no-build
```
