# AirApp Market

## 中文

`airappmarket/` 是阑山桌面的官方轻应用市场源目录。宿主只读取这里的 `index.json`，
再根据索引里的元数据解析轻应用、共享契约和下载地址。

### 约定

- `schema/`、`tools/AirAppMarket.Validator` 和 `index.json` 必须保持一致
- `plugins[].apiVersion` 主版本号必须等于 AirApp SDK 的 `1`，否则条目不会被收录
- 每个轻应用的 `sharedContracts` 都必须在顶层 `contracts[]` 中有对应条目，
  否则宿主装得上、载不起来
- 版本、依赖与包内容的真源是轻应用自己的 `.laapp`，不是这份索引
- 只有 GitHub 上的公开仓库能被收录，私有仓库的 Release 用户下载不到

### 目录

| 路径 | 说明 |
|---|---|
| `index.json` | 宿主读取的市场索引，由 CI 生成，不要手工编辑 |
| `registry/official-airapps.json` | 官方收录名单，唯一需要人工维护的文件 |
| `schema/airappmarket-index.schema.json` | 索引的 JSON Schema（schemaVersion 3.0.0） |
| `contracts/` | 市场分发的共享契约程序集，按 `<契约ID>/<版本>/` 组织 |
| `assets/` | 市场用到的静态图标 |
| `tools/AirAppMarket.IndexBuilder` | 名单 + GitHub Release → `index.json` |
| `tools/AirAppMarket.Validator` | 校验 `index.json` |

### 职责

- 维护官方市场索引与收录名单
- 维护 Schema、校验工具和静态市场资源
- 维护共享契约程序集及其 SHA-256

### 与示例轻应用的关系

- 官方示例条目指向独立仓库 `LanMountainDesktop.SamplePlugin`，那里是唯一的官方参考实现
- 本仓库只保留它用到的共享契约源码 `LanMountainDesktop.SharedContracts.SampleClock`

## English

`airappmarket/` is the official AirApp market source for LanMountainDesktop. The host reads
`index.json` here and resolves AirApps, shared contracts, and download URLs from its metadata.

### Conventions

- keep `schema/`, `tools/AirAppMarket.Validator`, and `index.json` in sync
- `plugins[].apiVersion` must share major version `1` with the AirApp SDK, or the entry is not listed
- every AirApp `sharedContracts` reference must exist in the top-level `contracts[]`, otherwise
  the AirApp installs and then fails to load
- the `.laapp` package is the source of truth for version, dependencies, and content — not this index
- only public GitHub repositories can be listed; users cannot download private release assets

### Responsibilities

- maintain the official market index and the official roster
- maintain the schema, validator, and static market assets
- maintain shared contract assemblies and their SHA-256 digests

### Relationship with the sample AirApp

- the official sample entry points to the standalone `LanMountainDesktop.SamplePlugin` repository,
  which is the single official reference implementation
- this repository only keeps the shared contract source it depends on,
  `LanMountainDesktop.SharedContracts.SampleClock`
