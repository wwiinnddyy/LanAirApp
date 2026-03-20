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
- `LanMountainDesktop.PluginSdk/`、`LanMountainDesktop.SharedContracts.SampleClock/` 与 `samples/` 仅作为镜像/模板材料

## English

`LanAirApp` is the public ecosystem repository for LanMountainDesktop. It owns market metadata, documentation, tools, and templates only. It does not own the desktop runtime, the authoritative SDK surface, or the official sample release source.

### This repository owns

- `airappmarket/`: the official market index, schema, validator, and static assets
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
- `LanMountainDesktop.PluginSdk/`, `LanMountainDesktop.SharedContracts.SampleClock/`, and `samples/` are mirrored/template material only
