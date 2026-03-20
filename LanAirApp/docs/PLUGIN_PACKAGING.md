# 插件打包指引

## 中文

LanMountainDesktop 插件的标准交付格式是 `.laapp`。本仓只维护打包约定、模板和辅助工具，权威的插件源码与发布说明应以 `LanMountainDesktop.SamplePlugin` 为准。

### 打包内容

- `plugin.json`
- 插件主程序集
- 插件依赖程序集和 `.deps.json`
- `Localization/zh-CN.json`
- `Localization/en-US.json`
- 插件运行所需的其他资源文件

### 目录约定

- `.laapp` 应输出到插件仓库根目录
- 仓库根目录应同时提供 `README.md`
- 官方市场只消费 `airappmarket/index.json` 中的元数据和下载链接
- `LanAirApp/releases/` 仅用于暂存或本地调试，不是权威发布源

### 与 v4 对齐

- `plugin.json.apiVersion` 使用 `4.x`
- 组件注册应采用 `PluginDesktopComponentOptions`
- 外观和圆角信息应通过 `IPluginAppearanceContext` / `PluginAppearanceSnapshot` 消费
- 圆角预设使用 `PluginCornerRadiusPreset`

### 打包工具

```powershell
dotnet run --project .\LanAirApp\tools\LanMountainDesktop.PluginPackager -- --input .\path\to\plugin-output --output .\YourPlugin.1.0.0.laapp --overwrite
```

### 发布链路

1. 官方市场读取 `airappmarket/index.json`
2. 用户从市场选择插件
3. 宿主下载对应仓库中的 `.laapp`
4. 宿主校验哈希并完成安装
5. 重启或重新加载后启用新插件

### 注意事项

- `entranceAssembly` 必须能在包内找到
- 不要把无关开发产物打进安装包
- 版本号、资产名和清单应保持一致
- 文档和市场元数据应指向权威主仓与官方示例仓

## English

The standard delivery format for LanMountainDesktop plugins is `.laapp`. This repository only maintains packaging conventions, templates, and helper tools. Authoritative plugin source and release notes should come from `LanMountainDesktop.SamplePlugin`.

### Packaging contents

- `plugin.json`
- the plugin main assembly
- dependency assemblies and `.deps.json`
- `Localization/zh-CN.json`
- `Localization/en-US.json`
- other runtime assets required by the plugin

### Directory conventions

- `.laapp` should be emitted to the plugin repository root
- the repository root should also provide `README.md`
- the official market only consumes metadata and download links from `airappmarket/index.json`
- `LanAirApp/releases/` is only for staging or local debugging, not the authoritative release source

### v4 alignment

- `plugin.json.apiVersion` uses `4.x`
- component registration should use `PluginDesktopComponentOptions`
- appearance and corner radius data should be consumed through `IPluginAppearanceContext` / `PluginAppearanceSnapshot`
- corner radius presets use `PluginCornerRadiusPreset`

### Packaging tool

```powershell
dotnet run --project .\LanAirApp\tools\LanMountainDesktop.PluginPackager -- --input .\path\to\plugin-output --output .\YourPlugin.1.0.0.laapp --overwrite
```

### Release flow

1. The official market reads `airappmarket/index.json`
2. The user selects a plugin
3. The host downloads the `.laapp` from the plugin repository
4. The host validates the hash and stages installation
5. The app reloads or restarts to enable the plugin

### Notes

- `entranceAssembly` must exist inside the package
- do not bundle unrelated build artifacts
- version, asset name, and manifest should stay in sync
- docs and market metadata should point to the authoritative host repo and official sample repo
