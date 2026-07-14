# LanMountainDesktop.SamplePlugin (Mirror)

## 中文

这是官方示例插件仓 `LanMountainDesktop.SamplePlugin` 的镜像模板副本，用于 `LanAirApp` 内部文档、SDK 联调和样例启动。

### 角色说明

- 这里保留可构建的镜像样例代码
- 这里的 `plugin.json` 与示例代码应跟随官方示例仓的 v5 语义
- 这里不是官方市场发布真源，也不是独立 Release 仓库

### 当前接口基线

- 插件 API 基线：`5.0.0`
- 样例版本：`0.4.0`
- UI 基线：Avalonia `12.1.0`
- 运行模式：`in-proc`
- 共享契约：`LanMountainDesktop.SharedContracts.SampleClock` `2.0.0`
- 共享契约 DLL 由市场 contracts 索引下发，不打进 `.laapp`
- 当前样例演示：
  - `IPluginRuntimeContext`
  - `IHostApplicationLifecycle`
  - `IPluginMessageBus`
  - `AddPluginExport`
  - `AddPluginSettingsSection`
  - `AddPluginDesktopComponent`
  - `PluginDesktopComponentOptions`
  - `IPluginAppearanceContext`
  - `PluginAppearanceSnapshot`
  - `PluginCornerRadiusPreset`

## English

This directory is the mirrored template copy of the official `LanMountainDesktop.SamplePlugin` repository. It exists inside `LanAirApp` for documentation, SDK integration, and sample bootstrapping.

### Role

- keep a buildable mirrored sample
- keep the manifest and sample code aligned with the official sample at API `5.0.0`
- avoid becoming the authoritative market release source or Release repository

### Current interface baseline

- Plugin API baseline: `5.0.0`
- Sample version: `0.4.0`
- UI baseline: Avalonia `12.1.0`
- Runtime mode: `in-proc`
- Shared contract: `LanMountainDesktop.SharedContracts.SampleClock` `2.0.0`
- The shared-contract DLL is delivered by the market contracts index and is not bundled in the `.laapp`
- This sample explicitly demonstrates:
  - `IPluginRuntimeContext`
  - `IHostApplicationLifecycle`
  - `IPluginMessageBus`
  - `AddPluginExport`
  - `AddPluginSettingsSection`
  - `AddPluginDesktopComponent`
  - `PluginDesktopComponentOptions`
  - `IPluginAppearanceContext`
  - `PluginAppearanceSnapshot`
  - `PluginCornerRadiusPreset`
