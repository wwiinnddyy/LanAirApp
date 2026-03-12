# LanMountainDesktop.SamplePlugin

## 中文

这是阑山桌面的独立示例插件仓库，也是当前插件市场联调与发布链路的参考项目。

### 仓库职责

- 提供可直接构建的示例插件工程
- 在仓库根目录产出 `.laapp`
- 在仓库根目录提供 `README.md`
- 通过 `LanAirApp/airappmarket/index.json` 被官方市场引用

### 发布约定

- 当前版本：`0.0.12`
- 当前 Release 标签：`v0.0.12`
- 当前根目录发布包：`LanMountainDesktop.SamplePlugin.0.0.12.laapp`
- 阑山桌面插件市场会优先解析 GitHub Release 里的同名资产
- 如果 Release 未发布、资产缺失或 GitHub API 无法解析，宿主会退回到仓库根目录 `.laapp`
- 插件介绍始终读取仓库根目录 `README.md`

### GitHub Actions

- `Sample Plugin CI` 只负责检查：在 `pull_request` 和推送到 `main` 时执行版本校验、构建、打包和市场元数据生成
- `Sample Plugin Release` 只负责发布：在推送 `v*` 标签或手动触发时创建或更新 GitHub Release
- 手动触发发布时，workflow 会先写入 `plugin.json`、`.csproj` 和根目录 `README.md` 的版本号，再基于这个版本提交创建 Release
- 推送 `v*` 标签发布时，workflow 要求标签本身已经指向版本号匹配的源码提交，否则会直接失败
- 发布完成后，`Sample Plugin Release` 会自动创建并合并本仓库同步 PR，把根目录 `.laapp` 和版本文件落回 `main`
- 发布完成后，`Sample Plugin Release` 也会自动创建并合并 `LanAirApp` 同步 PR，把官方市场索引落回 `main`
- 如果任一同步 PR 无法创建或合并，整个发布会直接失败，不允许 release 成功但主线仍停留旧版本

## English

This repository is the standalone sample plugin for LanMountainDesktop and the reference project for the current plugin market release flow.

### Repository role

- provide a directly buildable sample plugin project
- generate the `.laapp` package in the repository root
- keep `README.md` in the repository root
- be referenced by `LanAirApp/airappmarket/index.json`

### Publishing contract

- Current version: `0.0.12`
- Current release tag: `v0.0.12`
- Current root package: `LanMountainDesktop.SamplePlugin.0.0.12.laapp`
- Plugin API baseline: `2.0.0`
- Entry model: `Initialize(HostBuilderContext, IServiceCollection)`
- Private managed dependencies are supported and packaged with the plugin output. This sample now includes `Humanizer.Core` as a plugin-only dependency probe.
- Shared contracts are resolved from the official market. This sample declares `LanMountainDesktop.SharedContracts.SampleClock` and exports a clock service through that shared contract.
- the desktop market resolves the exact GitHub Release asset first
- if Release resolution fails, the host falls back to the repository root `.laapp`
- plugin details always come from the repository root `README.md`

### GitHub Actions

- `Sample Plugin CI` handles validation only on pull requests and pushes to `main`
- `Sample Plugin Release` handles publishing only on `v*` tags or manual dispatch
- on manual dispatch, the workflow writes the release version into `plugin.json`, the `.csproj`, and the repository-root `README.md` before creating the Release
- on `v*` tag pushes, the workflow requires the tag to already point to a commit whose version files match the tag
- after a release, the workflow creates and merges a PR in this repository to sync the root fallback package and version files back to `main`
- after a release, the workflow also creates and merges a PR in `LanAirApp` to sync the official market index back to `main`
- if either sync PR cannot be created or merged, the release fails instead of leaving `main` on an older version
