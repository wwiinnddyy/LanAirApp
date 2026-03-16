# AirApp Market

## 中文

`airappmarket/` 是阑山桌面的官方插件市场源目录。宿主应用只读取这里的 `index.json`，再根据索引里的仓库信息访问具体插件项目。

### 主要职责

- 维护官方 `index.json`
- 维护 Schema、校验工具和静态市场资源
- 维护 Release-first 的插件发布元数据

### 与示例插件的关系

- 官方示例插件条目指向独立仓库 `LanMountainDesktop.SamplePlugin`
- `LanAirApp/LanAirApp/samples/` 中的样例只作为镜像模板，不作为市场发布真源

## English

`airappmarket/` is the official plugin market source for LanMountainDesktop. The desktop app reads `index.json` here and then resolves each plugin repository from the metadata.

### Responsibilities

- maintain the official `index.json`
- maintain the schema, validator, and static market assets
- maintain release-first publishing metadata for plugins

### Relationship with the sample plugin

- the official sample plugin entry points to the standalone `LanMountainDesktop.SamplePlugin` repository
- the in-repo `LanAirApp/LanAirApp/samples/` sample is only a mirrored template, not the market release source
