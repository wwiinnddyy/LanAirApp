# 插件开发指引

## 中文

本指引面向 LanMountainDesktop 插件作者，说明 v4 语义下的插件清单、组件注册、外观能力和发布组织方式。权威 API 形状始终以主仓 `LanMountainDesktop` 为准，本仓只提供文档、模板和镜像样例。

### 必要文件

- `plugin.json`：插件清单，声明 `id`、`name`、`author`、`version`、`apiVersion` 和 `entranceAssembly`
- 插件程序集：与 `plugin.json` 的 `entranceAssembly` 对应
- 入口类：标记为插件入口，并实现 `IPlugin` 或继承 `PluginBase`
- 本地化资源：建议提供 `Localization/zh-CN.json` 和 `Localization/en-US.json`

### v4 开发要点

- `plugin.json.apiVersion` 使用 `4.x`
- 插件入口仍然使用 `Initialize(HostBuilderContext, IServiceCollection)`
- 组件注册改用 `PluginDesktopComponentOptions`
- 圆角和外观信息通过 `IPluginAppearanceContext` 与 `PluginAppearanceSnapshot` 获取
- 圆角预设使用 `PluginCornerRadiusPreset`
- 插件能力继续通过 `AddPluginSettingsSection`、`AddPluginDesktopComponent`、`AddPluginExport` 暴露

### 推荐流程

1. 先以官方示例插件仓 `LanMountainDesktop.SamplePlugin` 为模板
2. 按 v4 语义填写 `plugin.json`
3. 使用 `IServiceCollection` 注册设置页、桌面组件、导出契约和 hosted service
4. 在控制和服务中读取 `IPluginRuntimeContext` 与外观快照
5. 打包为 `.laapp`

### 运行时能力

- 设置页注册
- 桌面组件注册
- 插件内部服务注册
- 宿主生命周期调用
- 插件消息总线通信

### 不要假设

- 不要假设 `LanAirApp` 是 SDK 的权威来源
- 不要假设镜像样例仓比官方示例仓更新
- 不要直接依赖宿主内部实现细节

## English

This guide is for LanMountainDesktop plugin authors and describes plugin manifests, component registration, appearance APIs, and packaging flow under the v4 semantic model. The authoritative API shape always lives in the host repository `LanMountainDesktop`; this repository provides documentation, templates, and mirrored sample material only.

### Required files

- `plugin.json`: plugin manifest declaring `id`, `name`, `author`, `version`, `apiVersion`, and `entranceAssembly`
- Plugin assembly matching the manifest
- Entry class marked as the plugin entrance, implementing `IPlugin` or inheriting from `PluginBase`
- Localization resources, preferably `Localization/zh-CN.json` and `Localization/en-US.json`

### v4 highlights

- `plugin.json.apiVersion` uses `4.x`
- The plugin entry point remains `Initialize(HostBuilderContext, IServiceCollection)`
- Desktop components are registered with `PluginDesktopComponentOptions`
- Corner radius and appearance state come from `IPluginAppearanceContext` and `PluginAppearanceSnapshot`
- Corner radius presets use `PluginCornerRadiusPreset`
- Host-facing capabilities continue to be exposed with `AddPluginSettingsSection`, `AddPluginDesktopComponent`, and `AddPluginExport`

### Recommended workflow

1. Start from the official sample plugin repository `LanMountainDesktop.SamplePlugin`
2. Fill in the manifest with v4 semantics
3. Register settings pages, desktop components, exports, and hosted services via `IServiceCollection`
4. Read `IPluginRuntimeContext` and appearance snapshots inside controls and services
5. Package the plugin as `.laapp`

### Runtime capabilities

- settings page registration
- desktop component registration
- plugin internal service registration
- host lifecycle requests
- plugin message bus communication

### Do not assume

- do not assume `LanAirApp` is the authoritative SDK source
- do not assume mirrored templates are ahead of the official sample repository
- do not depend directly on host implementation details
