# 插件打包指引

## 中文

LanMountainDesktop 插件的标准交付格式是 `.laapp`。本仓只维护打包约定、模板和辅助工具，官方市场以 `airappmarket/index.json` 为索引入口；默认它只保存插件链接，不承接版本、依赖或包内容真相。

### 打包内容

- `plugin.json`
- 插件主程序集
- 插件依赖程序集和 `.deps.json`
- `Localization/zh-CN.json`
- `Localization/en-US.json`
- 插件运行所需的其他资源文件
- `sharedContracts` 对应的契约程序集（如有）

### 目录约定

- `.laapp` 应输出到插件仓库根目录
- 仓库根目录应同时提供 `README.md`
- 官方市场只消费 `airappmarket/index.json` 中的链接指针
- `LanAirApp/releases/` 仅用于暂存或本地调试，不是权威发布源

### 与 v4 对齐

- `plugin.json.apiVersion` 使用 `4.0.0`
- `sharedContracts` 需要与宿主 `SharedContracts` 模型保持一致
- 组件注册应采用 `PluginDesktopComponentOptions`
- 外观和圆角信息应通过 `IPluginAppearanceContext` / `PluginAppearanceSnapshot` 消费
- 圆角预设使用 `PluginCornerRadiusPreset`

### 打包工具

```powershell
dotnet run --project .\LanAirApp\tools\LanMountainDesktop.PluginPackager -- --input .\path\to\plugin-output --output .\YourPlugin.<version>.laapp --overwrite
```

### 发布链路

1. 官方市场读取 `airappmarket/index.json`
2. 用户从市场选择插件
3. 宿主沿链接进入插件项目的真源
4. 宿主下载并校验对应仓库中的 `.laapp`
5. 重启或重新加载后启用新插件

### 注意事项

- `entranceAssembly` 必须能在包内找到
- 不要把无关开发产物打进安装包
- 版本号、资产名和清单应保持一致
- 文档、市场元数据和样例条目应指向同一份发布语义

## English

The standard delivery format for LanMountainDesktop plugins is `.laapp`. This repository only maintains packaging conventions, templates, and helper tools. The official market index should follow `airappmarket/index.json`, but that file is now a pure link index by default. Version, dependency, and package truth stay in the plugin repository itself; snapshot mode is available only when we explicitly want a cached market view.

### Packaging contents

- `plugin.json`
- the plugin main assembly
- dependency assemblies and `.deps.json`
- `Localization/zh-CN.json`
- `Localization/en-US.json`
- other runtime assets required by the plugin
- contract assemblies referenced by `sharedContracts`, if any

### Directory conventions

- `.laapp` should be emitted to the plugin repository root
- the repository root should also provide `README.md`
- the official market only consumes link pointers from `airappmarket/index.json`
- `LanAirApp/releases/` is only for staging or local debugging, not the authoritative release source

### v4 alignment

- `plugin.json.apiVersion` uses `4.0.0`
- `sharedContracts` should stay aligned with the host `SharedContracts` model
- component registration should use `PluginDesktopComponentOptions`
- appearance and corner radius data should be consumed through `IPluginAppearanceContext` / `PluginAppearanceSnapshot`
- corner radius presets use `PluginCornerRadiusPreset`

### Packaging tool

```powershell
dotnet run --project .\LanAirApp\tools\LanMountainDesktop.PluginPackager -- --input .\path\to\plugin-output --output .\YourPlugin.<version>.laapp --overwrite
```

### Release flow

1. The official market reads `airappmarket/index.json`
2. The user selects a plugin
3. The host follows the repository/package links to the plugin source of truth
4. The host downloads and validates the `.laapp` from the plugin repository
5. The app reloads or restarts to enable the plugin

### Notes

- `entranceAssembly` must exist inside the package
- do not bundle unrelated build artifacts
- version, asset name, and manifest should stay in sync
- docs, market metadata, and the sample entry should all describe the same release contract
