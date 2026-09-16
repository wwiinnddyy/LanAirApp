# LanAirApp — 阑山桌面官方轻应用市场

> 这里是**市场源**，不是 SDK 仓库。AirApp SDK 的代码与开发文档都在主仓库
> [LanMountainDesktop](https://github.com/wwiinnddyy/LanMountainDesktop)。

阑山桌面的宿主启动市场时，只读取本仓库的 `airappmarket/index.json`。索引里每个条目都是自包含的：
展示信息、兼容性信息和下载地址全部内联，宿主不需要再回调 GitHub 拼装元数据。

## 唯一的 SDK

| 项 | 值 |
|---|---|
| SDK 包 | `LanMountainDesktop.AirAppSdk` |
| API 版本 | `1.0.0` |
| 清单文件 | `airapp.json` |
| 包格式 | `.laapp`（构建时由 SDK 自动生成） |
| 索引 schema | `3.0.0` |

`LanMountainDesktop.PluginSdk`、`plugin.json` 以及历史上的 apiVersion `4.x` / `5.x` / `6.x` 已全部废弃。
宿主不再有任何代码路径接受它们，市场也不再收录它们——`apiVersion` 主版本号必须等于 `1`，
否则 IndexBuilder 会跳过该条目、Validator 会直接报错。

## 仓库结构

```
airappmarket/
├── index.json                      # 宿主读取的市场索引（由 CI 生成，不要手工编辑）
├── registry/official-airapps.json  # 官方收录名单（唯一需要人工维护的文件）
├── schema/airappmarket-index.schema.json
├── contracts/<契约ID>/<版本>/*.dll # 市场分发的共享契约程序集
├── assets/                         # 市场用到的静态图标
└── tools/
    ├── AirAppMarket.IndexBuilder/  # 名单 + GitHub Release → index.json
    └── AirAppMarket.Validator/     # 校验 index.json
docs/
├── 收录指南.md                      # 作者视角：怎么把轻应用提交到市场
└── 收录标准.md                      # 审核视角：什么样的轻应用会被收录
LanMountainDesktop.SharedContracts.SampleClock/  # 官方示例用的共享契约源码
```

## 索引是怎么生成的

```
official-airapps.json  ──┐
                         ├─► IndexBuilder ─► index.json ─► Validator ─► 提交到 main
各轻应用的 GitHub Release ┘
```

IndexBuilder 对名单里的每个仓库取最新 Release，下载 `.laapp`，从包内 `airapp.json` 读取权威的
`id`、`version`、`apiVersion`、`entranceAssembly`、`sharedContracts` 与组件列表，计算 SHA-256 / MD5，
再叠加 Release 里可选的 `market-manifest.json` 与名单里的展示信息。

**版本真源永远是轻应用自己的包**，名单只提供市场侧的展示信息和兜底值。

一个仓库如果还没有可发布的 Release、`.laapp` 里仍是旧的 `plugin.json`、或 `apiVersion` 主版本不是 `1`，
IndexBuilder 会打印原因并跳过它，其余条目照常刷新。加 `--strict` 可以让任何一次跳过都变成失败。

### 本地运行

```bash
# 重新生成索引
dotnet run --project airappmarket/tools/AirAppMarket.IndexBuilder -- \
  --registry airappmarket/registry/official-airapps.json \
  --output airappmarket/index.json

# 校验索引
dotnet run --project airappmarket/tools/AirAppMarket.Validator -- \
  airappmarket/index.json airappmarket/schema/airappmarket-index.schema.json
```

## 自动化

| 工作流 | 触发 | 作用 |
|---|---|---|
| `airappmarket-refresh.yml` | `registry/**` 变更、手动、或轻应用仓库发 `official-plugin-released` 事件 | 重新生成并提交 `index.json` |
| `airappmarket-validate.yml` | `airappmarket/**` 的 push 与 PR | 校验索引 |

## 收录你的轻应用

1. 按主仓库的 [轻应用开发文档](https://github.com/wwiinnddyy/LanMountainDesktop/tree/main/docs/01-AirApp%E5%BC%80%E5%8F%91) 完成开发；
2. 确认满足 [收录标准](docs/收录标准.md)；
3. 按 [收录指南](docs/收录指南.md) 发 Release 并提交 PR，只修改 `registry/official-airapps.json`。

`index.json` 由 CI 生成，PR 里不要改它。

## 许可证

MIT
