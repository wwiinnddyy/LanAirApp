# 插件标准说明

## 中文

这个目录保存 LanMountainDesktop 插件开发需要遵循的基础约定和模板文件。权威 API 定义应以 `LanMountainDesktop` 主仓为准，本仓只维护可复用的约定文本。

### 当前标准

- 安装包扩展名：`.laapp`
- 插件清单文件：`plugin.json`
- 清单 API 版本：`4.x`
- 本地化目录：`Localization/`
- 推荐语言文件：`zh-CN.json` 和 `en-US.json`
- 组件注册方式：`PluginDesktopComponentOptions`
- 圆角与外观语义：`IPluginAppearanceContext`、`PluginAppearanceSnapshot`、`PluginCornerRadiusPreset`
- 仓库根目录交付物：`.laapp` 和 `README.md`

### 建议

- 中文作为主文案和主界面语言
- 英文作为附加扩展语言
- 清单字段尽量保持稳定，避免频繁改动插件 `id` 和入口程序集
- 文档和模板优先引用官方主仓和官方示例仓

## English

This directory stores the baseline conventions and template files for LanMountainDesktop plugin development.

### Current standards

- Package extension: `.laapp`
- Manifest file: `plugin.json`
- Manifest API version: `4.x`
- Localization directory: `Localization/`
- Recommended languages: `zh-CN.json` and `en-US.json`
- Component registration: `PluginDesktopComponentOptions`
- Appearance and corner radius semantics: `IPluginAppearanceContext`, `PluginAppearanceSnapshot`, and `PluginCornerRadiusPreset`
- Repository root deliverables: `.laapp` and `README.md`

### Suggestions

- use Chinese as the primary documentation and UI language
- use English as an additional expansion language
- keep manifest fields stable to avoid frequent `id` and entrance assembly changes
- point docs and templates to the authoritative host repo and official sample repo first
