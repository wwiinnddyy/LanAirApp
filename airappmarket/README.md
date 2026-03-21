# AirApp Market

## 中文

`airappmarket/` 是阑山桌面的官方插件市场源目录。宿主只读取这里的 `index.json`，再根据索引里的元数据解析插件、共享契约和下载地址。

### 约定

- `schema`、`validator` 和 `index.json` 必须保持一致
- `plugins[].apiVersion` 以 `4.0.0` 为准
- `plugins[].sharedContracts` 与宿主 `SharedContracts` 模型对齐
- 官方示例插件条目使用仓库现状中的 `LanMountainDesktop.SamplePlugin v0.1.1`

### 职责

- 维护官方市场索引
- 维护 Schema、校验工具和静态市场资源
- 维护与发布元数据一致的市场条目

### 与示例插件的关系

- 官方示例插件条目指向独立仓库 `LanMountainDesktop.SamplePlugin`
- 仓库内的 `LanAirApp/samples/` 仅用于镜像和本地验证，不作为发布真源

## English

`airappmarket/` is the official plugin market source for LanMountainDesktop. The host reads `index.json` here and resolves plugins, shared contracts, and download URLs from the metadata.

### Conventions

- keep `schema`, `validator`, and `index.json` in sync
- use `4.0.0` for `plugins[].apiVersion`
- keep `plugins[].sharedContracts` aligned with the host `SharedContracts` model
- the official sample plugin entry uses the sample repo state: `LanMountainDesktop.SamplePlugin v0.1.1`

### Responsibilities

- maintain the official market index
- maintain the schema, validator, and static market assets
- keep market entries aligned with release metadata

### Relationship with the sample plugin

- the official sample plugin entry points to the standalone `LanMountainDesktop.SamplePlugin` repository
- the in-repo `LanAirApp/samples/` tree is only a mirrored template for local validation, not the release source
