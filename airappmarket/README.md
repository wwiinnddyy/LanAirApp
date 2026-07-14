# AirApp Market

`airappmarket/` 是阑山桌面（LanMountainDesktop）的官方传统插件市场源。生产宿主只消费
`index.json` 的 flat v3 快照；索引由注册表和插件仓库的已发布 Release 自动生成，不接受手写版本、
API、哈希或包大小。

## 生产协议

| 层级 | 文件 | 版本 | 职责 |
|---|---|---|---|
| 官方注册表 | `registry/official-plugins.json` | `1.0.0` | 保存仓库指针、市场覆盖信息和启停状态 |
| 插件发布元数据 | Release Asset `market-manifest.json` | `2.0.0` | 保存展示、兼容性、能力和发布元数据 |
| 插件运行清单 | `.laapp!/plugin.json` | PluginSdk API `5.0.0` | 插件 ID、版本、入口程序集和共享契约真源 |
| 宿主市场索引 | `index.json` | flat `3.0.0` | 宿主直接读取的自包含目录和安装元数据 |

AirAppSdk API 6 属于独立的 AirApp 应用模型，不进入本传统 PluginSdk 5 插件市场协议。

## 生成与安装链路

1. IndexBuilder 读取 `official-plugins.json`，跳过 `enabled: false` 的条目。
2. 对每个启用条目读取 GitHub 最新 Release 和 `market-manifest.json`。
3. 下载注册表中的共享契约，校验实际 SHA-256 和大小。
4. 下载 `.laapp`，读取包内根目录 `plugin.json`，检查入口 DLL、API 5、版本、Release Tag 和资产名。
5. 从实际下载字节计算 SHA-256、MD5 和包大小；市场索引不信任手写校验值。
6. 生成 self-contained flat v3 `index.json`。
7. 宿主按 `releaseAsset` → `rawFallback` → `workspaceLocal` 顺序尝试安装，并再次校验 SHA-256 和大小。

每个官方条目至少需要 `releaseAsset`，可再提供后续回退源。生成器当前统一输出三种源：

- `releaseAsset`：GitHub Release 的权威安装包；
- `rawFallback`：插件仓库 `main` 根目录中的同名包；
- `workspaceLocal`：多仓库本地开发环境中的同名包。

## 发布一致性要求

设插件 ID 为 `LanMountainDesktop.YourPlugin`，版本为 `1.2.3`：

- `plugin.json.apiVersion` 必须为 `5.x`（当前生产基线 `5.0.0`）；
- `minHostVersion` 必须至少为 `0.8.6`，这是首个包含 Plugin SDK 5 的宿主版本；
- GitHub Release Tag 必须为 `v1.2.3`；
- Release Asset 必须为 `LanMountainDesktop.YourPlugin.1.2.3.laapp`；
- `.laapp!/plugin.json` 的 `id`、`version`、`apiVersion` 必须与发布元数据一致；
- `entranceAssembly` 必须是包根目录中唯一存在的 DLL；
- `sharedContracts` 的每个引用必须已发布在注册表 `contracts` 中；
- `market-manifest.json` 必须使用 schema `2.0.0`，且其版本、Tag、资产名、SHA 和大小不得与实际包冲突。

## 临时禁用不兼容插件

当某仓库最新 Release 尚未迁移到 PluginSdk 5，或发布包校验失败时，可保留注册信息但设置：

```json
{
  "id": "Example.Plugin",
  "enabled": false,
  "disabledReason": "Latest published package still targets PluginSdk API 4.0.0."
}
```

`enabled` 和 `disabledReason` 仅属于注册表/构建阶段，不会写入宿主消费的 flat v3 索引。新 Release
通过真实包重建后再恢复 `enabled: true`。

## 本地验证

```powershell
# 离线校验注册表
dotnet run --project .\airappmarket\tools\AirAppMarket.IndexBuilder -- `
  --registry .\airappmarket\registry\official-plugins.json `
  --validate-registry-only

# 运行校验器正/负回归
dotnet run --project .\airappmarket\tools\AirAppMarket.Validator -- `
  --self-test --expected-api-version 5.0.0

# 校验插件仓库的发布元数据模板
dotnet run --project .\airappmarket\tools\AirAppMarket.IndexBuilder -- `
  --validate-market-manifest .\path\to\market-manifest.json

# 在发布前交叉校验本地 .laapp 与 market-manifest.json
dotnet run --project .\airappmarket\tools\AirAppMarket.IndexBuilder -- `
  --validate-release-package .\path\to\YourPlugin.1.0.0.laapp `
  --market-manifest .\path\to\market-manifest.json `
  --plugin-id LanMountainDesktop.YourPlugin

# 从真实 GitHub Release 重建索引
dotnet run --project .\airappmarket\tools\AirAppMarket.IndexBuilder -- `
  --registry .\airappmarket\registry\official-plugins.json `
  --output .\airappmarket\index.json `
  --require-market-manifest

# 校验提交的 flat v3 索引、Schema、包源顺序和完整性字段
dotnet run --project .\airappmarket\tools\AirAppMarket.Validator -- `
  .\airappmarket\index.json `
  .\airappmarket\schema\airappmarket-index.schema.json `
  --expected-api-version 5.0.0
```

CI 会重复执行注册表校验、校验器回归、已提交索引校验、真实 Release 重建和重建结果校验。

## English

The official market uses four explicit contracts: registry v1, release `market-manifest.json` v2,
PluginSdk 5 `plugin.json`, and the host-facing self-contained flat index v3. The index builder downloads
the real release package, verifies its manifest and entry assembly, computes SHA-256 and package size,
and emits canonical package sources. Incompatible published releases stay registered with
`enabled: false` and a `disabledReason`; build-time control fields never leak into the host index.
