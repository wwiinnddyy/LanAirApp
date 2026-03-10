# 插件开发指南

## 中文

本指南面向阑山桌面的插件作者，说明如何使用 `LanMountainDesktop.PluginSdk` 开发一个可被宿主发现、加载和展示的插件。

### 必备文件

- `plugin.json`：插件清单，声明 `id`、`name`、`author`、`version`、`apiVersion`、`entranceAssembly`。
- 插件程序集：包含入口类，并与 `plugin.json` 中的 `entranceAssembly` 一致。
- 入口类：使用插件入口特性标记，并实现 `IPlugin` 或继承 `PluginBase`。
- 本地化资源：建议至少提供 `Localization/zh-CN.json` 和 `Localization/en-US.json`。

### 基本开发流程

1. 以 `samples/LanMountainDesktop.SamplePlugin` 或独立示例插件仓库为模板。
2. 修改 `plugin.json` 和程序集名称，确保插件 ID 全局唯一。
3. 在入口类中通过 `IPluginContext` 注册服务、设置页和桌面组件。
4. 为设置页和组件准备本地化文本与状态数据。
5. 构建并打包为 `.laapp`。

### 运行时能力

插件当前可以：

- 注册设置页。
- 注册桌面组件。
- 注册插件内部服务。
- 使用宿主提供的上下文服务。
- 使用插件消息总线进行内部通信。

插件当前不应假设：

- 存在强隔离沙箱。
- 可以热更新自身。
- 拥有超出 SDK 明确定义的宿主内部访问权限。

### 推荐目录结构

```text
YourPlugin/
  plugin.json
  YourPlugin.csproj
  Localization/
    zh-CN.json
    en-US.json
  README.md
  YourPlugin.1.0.0.laapp
```

### 本地化建议

- 中文作为主文案。
- 英文作为附加扩展语言。
- 所有 UI 文案都通过本地化文件读取，不直接硬编码到界面逻辑中。

## English

This guide explains how to build a LanMountainDesktop plugin with `LanMountainDesktop.PluginSdk`.

### Required files

- `plugin.json`: plugin manifest declaring `id`, `name`, `author`, `version`, `apiVersion`, and `entranceAssembly`.
- Plugin assembly matching the manifest.
- Entry class marked as the plugin entrance, implementing `IPlugin` or inheriting from `PluginBase`.
- Localization resources, preferably `Localization/zh-CN.json` and `Localization/en-US.json`.

### Recommended workflow

1. Start from the sample plugin.
2. Update the manifest and assembly names.
3. Register services, settings pages, and desktop components through `IPluginContext`.
4. Prepare localized texts and state handling.
5. Build and package the plugin as `.laapp`.
