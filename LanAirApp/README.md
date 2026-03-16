# LanAirApp

## 中文

`LanAirApp` 是阑山桌面插件生态的对外仓库，负责官方插件市场、插件开发文档、打包与校验工具，以及镜像样例模板。

### 本仓库负责什么

- `airappmarket/`：官方市场索引、Schema、校验工具和静态资源
- `docs/`：插件开发与打包文档
- `tools/`：打包和辅助工具
- `samples/`：镜像样例与模板
- `LanMountainDesktop.PluginSdk/`：镜像 SDK，需与宿主仓库保持一致

### 不负责什么

- 不承载桌面宿主运行时
- 不作为插件 API 的独立权威来源
- 不作为官方示例插件发布仓库

### 关系约束

- 宿主应用权威仓库：`LanMontainDesktop`
- 官方示例插件权威仓库：`LanMountainDesktop.SamplePlugin`
- `samples/LanMountainDesktop.SamplePlugin` 是镜像模板副本，用于文档和本地联调

## English

`LanAirApp` is the public-facing repository for the LanMountainDesktop plugin ecosystem. It owns the official plugin market, developer-facing documentation, packaging and validation tools, and mirrored sample templates.

### What this repository owns

- `airappmarket/`: the official market index, schema, validator, and static assets
- `docs/`: plugin development and packaging guides
- `tools/`: packaging and helper tools
- `samples/`: mirrored samples and templates
- `LanMountainDesktop.PluginSdk/`: the mirrored SDK copy that must stay aligned with the host repository

### What this repository does not own

- the desktop host runtime
- an independent plugin API baseline
- the authoritative release repository for the official sample plugin

### Relationship boundaries

- Host source of truth: `LanMontainDesktop`
- Official sample plugin source of truth: `LanMountainDesktop.SamplePlugin`
- `samples/LanMountainDesktop.SamplePlugin` is a mirrored template copy for docs and local integration
