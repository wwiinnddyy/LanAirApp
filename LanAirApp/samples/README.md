# 样例目录 / Samples

本目录包含两条完全不同的扩展路线，不能混用清单、包或市场入口。

## 当前生产路线：Plugin SDK 5

- [`LanMountainDesktop.SamplePlugin`](./LanMountainDesktop.SamplePlugin/README.md) 是当前可构建的 Plugin SDK 5.0.0 镜像样例。
- 它使用 `plugin.json` 和生产 `.laapp` 插件安装链，并进入 `LanAirApp.slnx` 与兼容性 CI。
- 正式发布仍以独立 `LanMountainDesktop.SamplePlugin` 仓库为权威源，本目录只保存镜像模板。

## 历史设计原型：AirApp API 1

以下目录是早期第三方 AirApp 设计原型，不是当前可安装产品：

- `NotesApp`
- `SystemMonitor`
- `WeatherWidget`

它们仍声明 `LanMountainDesktop.AirAppSdk 1.0.0` 与 `airapp.json` API 1.0.0，并被有意排除在 `LanAirApp.slnx`、CI、Plugin SDK v5 打包流程和市场 registry 之外。不要把它们标记为 API 6.0.0，也不要发布其 `.laapp`。

无法完整迁移的三项具体阻塞是：

1. 生产 Host、Runtime、AirAppHost 和 Launcher 没有第三方 `airapp.json` 扫描、程序集加载、`[AirAppEntrance]` 发现或 AirApp 生命周期装配。
2. 当前生产 `.laapp` 安装路由属于 Plugin SDK 5，并要求包根目录存在 `plugin.json`；仅含 `airapp.json` 的原型包会被拒绝。
3. AirApp 工具链尚不完整：AirAppDevServer 的预览加载仍是 TODO，权威 AirAppSdk 6.0.0 NuGet 包也没有实际携带其项目声明的自动打包 targets，因而不存在受生产支持的构建、打包和端到端验证链。

完整审计、源码级 API 对照和后续迁移验收条件见 [`LEGACY_AIRAPP_SAMPLES.md`](./LEGACY_AIRAPP_SAMPLES.md)。

## English

This directory contains two distinct extension tracks.

- [`LanMountainDesktop.SamplePlugin`](./LanMountainDesktop.SamplePlugin/README.md) is the current buildable Plugin SDK 5.0.0 sample. It uses `plugin.json`, the production `.laapp` plugin path, and the Plugin SDK v5 market contract.
- `NotesApp`, `SystemMonitor`, and `WeatherWidget` are historical third-party AirApp API 1.0.0 design prototypes. They are intentionally excluded from `LanAirApp.slnx`, CI, Plugin SDK v5 packaging, and the market registry.

The legacy AirApp samples cannot be called production-ready API 6 apps: the production host has no third-party `airapp.json` loader or lifecycle integration; its `.laapp` installer requires `plugin.json`; and the AirApp preview/packaging toolchain has no supported end-to-end path. See [`LEGACY_AIRAPP_SAMPLES.md`](./LEGACY_AIRAPP_SAMPLES.md) for the evidence and migration gates.
