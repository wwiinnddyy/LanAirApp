# LanAirApp

## 中文

`LanAirApp` 是阑山桌面插件生态的对外工作区。这个仓库不承担宿主运行时本体职责，而是负责整理插件开发标准、示例插件、打包工具以及官方插件市场源。

### 目录说明

- `docs/`：插件开发与打包文档。
- `samples/`：示例插件与参考项目。
- `standards/`：插件清单和目录结构约定。
- `tools/`：插件打包与辅助工具。
- `airappmarket/`：官方插件市场索引、Schema、校验工具与静态资源。
- `releases/`：临时分发或人工分享目录，不再作为内建市场的主分发源。
- `LanMountainDesktop.PluginSdk/`：插件开发时需要引用的 SDK 契约。

### 与阑山桌面的关系

- 阑山桌面程序只连接 `LanAirApp/airappmarket/index.json` 获取官方插件列表。
- 市场索引只负责维护插件元数据和链接，不直接承载插件运行时逻辑。
- 每个插件项目应当在自己的仓库根目录提供 `.laapp` 安装包和 `README.md`。
- 宿主程序根据官方索引逐项列出插件，并从对应插件仓库读取安装包和说明文档。

### 推荐工作流

1. 从示例插件开始创建新插件项目。
2. 完成 `plugin.json`、入口类、设置页、桌面组件和本地化资源。
3. 打包生成根目录 `.laapp`。
4. 将插件仓库根目录和校验信息登记到 `airappmarket/index.json`。
5. 通过阑山桌面内建插件市场完成验证、安装和更新测试。

### 开发入口

- 仓库主入口解决方案文件为 `LanAirApp.slnx`。
- SDK 版本由仓库根目录 `global.json` 锁定。

## English

`LanAirApp` is the public-facing workspace for the LanMountainDesktop plugin ecosystem. This repository does not host the desktop runtime itself. Instead, it provides plugin development standards, sample plugins, packaging tools, and the official plugin market source.

### Directory overview

- `docs/`: plugin development and packaging guides.
- `samples/`: sample plugins and reference projects.
- `standards/`: manifest and structure conventions.
- `tools/`: packaging and helper tools.
- `airappmarket/`: the official market index, schema, validator, and static assets.
- `releases/`: temporary staging or manual sharing only, no longer the primary built-in market source.
- `LanMountainDesktop.PluginSdk/`: the SDK contract consumed by plugin authors.

### Relationship with LanMountainDesktop

- LanMountainDesktop connects only to `LanAirApp/airappmarket/index.json` for the official plugin list.
- The market index stores plugin metadata and links, not runtime implementation.
- Each plugin repository should provide its `.laapp` package and `README.md` in the repository root.
- The host app lists plugins from the official source and installs them from their own repositories.

### Development entry point

- The repository entry solution is `LanAirApp.slnx`.
- The SDK version is pinned by the root `global.json`.
