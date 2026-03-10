# AirApp Market

## 中文

`airappmarket/` 是阑山桌面的官方插件市场源目录。阑山桌面程序只连接这里的 `index.json`，再根据索引里的仓库信息访问各个插件项目。

### 当前职责

- 提供官方市场索引 `index.json`
- 提供索引 Schema 与本地校验工具
- 维护市场静态资源，例如图标
- 约束插件条目的 Release-first 发布信息

### 索引约定

1. 阑山桌面先读取 `airappmarket/index.json`。
2. 每个插件条目提供项目仓库、README、回退下载地址，以及可选的 GitHub Release 元数据。
3. 当条目同时声明 `releaseTag` 和 `releaseAssetName` 时，宿主优先按精确标签解析 GitHub Release 资产。
4. 当 Release 不存在、资产缺失或解析失败时，宿主退回到 `downloadUrl` 指向的仓库根目录 `.laapp`。
5. 插件介绍始终读取仓库根目录 `README.md`，不会直接使用 Release body 代替 README。

### 维护范围

- 这里只维护官方市场源，不做多源聚合。
- 插件市场数据集中在 `LanAirApp`。
- 插件内容分散在各自仓库，仓库应至少提供根目录 `.laapp` 和根目录 `README.md` 作为回退入口。

## English

`airappmarket/` is the official plugin market source for LanMountainDesktop. The desktop app reads `index.json` here and then resolves each plugin repository from the metadata.

### Responsibilities

- provide the official `index.json`
- provide the schema and validation tools
- host static market assets such as icons
- define the release-first publishing contract

### Release-first rule

1. The desktop app reads `airappmarket/index.json`.
2. Each entry declares repository metadata, README, a root-package fallback URL, and optional GitHub Release metadata.
3. If both `releaseTag` and `releaseAssetName` exist, the host first resolves the exact GitHub Release asset.
4. If Release resolution fails, the host falls back to the repository root `.laapp` from `downloadUrl`.
5. Plugin details always come from the repository root `README.md`.
