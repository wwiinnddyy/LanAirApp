---
name: 轻应用上架 / Plugin Submission
about: 将 PluginSdk 5 轻应用提交到 LanAirApp 官方市场
title: "[Plugin] <插件ID> v<版本>"
labels: plugin-submission
---

> [!IMPORTANT]
> 生产市场当前只接收 **LanMountainDesktop PluginSdk API `5.0.0`** 插件：安装包必须是根目录含 `plugin.json` 的 `.laapp`。使用 `airapp.json` 的 AirApp API 1 历史原型和 API 6 设计原型尚无生产加载链路，不能通过本模板上架。

## 最快上架 / Quick Submission

1. 在插件仓库发布 `vX.Y.Z` Release，并上传 `<插件ID>.X.Y.Z.laapp` 和 `market-manifest.json` 两个 Asset。
2. 只在 `airappmarket/registry/official-plugins.json` 的 `plugins` 数组末尾添加一条记录，不要手工修改 `airappmarket/index.json`。
3. 填写下面的链接和清单；CI 会下载真实 Release 资产并完成最终校验。

详细字段和示例见 [收录指南](../../docs/收录指南.md) 与 [market-manifest 模板](../../airappmarket/templates/market-manifest.template.json)。

## 发布信息 / Release Information

| 字段 | 请填写 |
|------|--------|
| **插件 ID** | <!-- 例：LanMountainDesktop.YourPlugin --> |
| **插件名称** | <!-- 例：Your Plugin --> |
| **作者** | <!-- 作者或组织名 --> |
| **简介** | <!-- 一句话说明用途 --> |
| **版本（不含 v）** | <!-- 例：1.0.0 --> |
| **仓库 URL** | <!-- https://github.com/owner/repo --> |
| **Release Tag** | <!-- v1.0.0 --> |
| **Release URL** | <!-- https://github.com/owner/repo/releases/tag/v1.0.0 --> |
| **`.laapp` URL** | <!-- https://github.com/owner/repo/releases/download/v1.0.0/LanMountainDesktop.YourPlugin.1.0.0.laapp --> |
| **`market-manifest.json` URL** | <!-- https://github.com/owner/repo/releases/download/v1.0.0/market-manifest.json --> |
| **PluginSdk API** | `5.0.0` |
| **最低宿主版本** | <!-- 必须 >= 0.8.6 --> |
| **本地验证结果** | <!-- PASS；如失败请附输出 --> |
| **宿主实机测试** | <!-- LanMountainDesktop 版本、系统和结果 --> |
| **CI URL / 结果** | <!-- 提交 PR 后补充，或填写 PASS --> |

## 注册表变更 / Registry Entry

最小可用条目如下；主页、图标、标签和能力提示可按 [收录指南](../../docs/收录指南.md) 追加：

```json
{
  "id": "<插件ID>",
  "repositoryUrl": "<GitHub仓库URL>",
  "marketManifestAssetName": "market-manifest.json",
  "defaultMinHostVersion": "0.8.6"
}
```

## 上架清单 / Submission Checklist

### Release 与版本

- [ ] 仓库是可访问的 GitHub 仓库，根目录含 `README.md`
- [ ] Release Tag 严格为 `vX.Y.Z`，且版本与 `plugin.json`、`market-manifest.json` 完全一致
- [ ] Release 中的包名严格为 `<插件ID>.<版本>.laapp`（版本部分不含 `v`）
- [ ] 同名 `.laapp` 已放在插件仓库 `main` 根目录，作为 `rawFallback`，且与 Release Asset 的 SHA-256/大小一致
- [ ] Release 同时包含名为 `market-manifest.json` 的 Asset，且 `schemaVersion` 为 `2.0.0`
- [ ] `apiVersion` 为当前生产 API `5.0.0`，`minHostVersion` 不低于 `0.8.6`

### 安装包

- [ ] `.laapp` 根目录包含合法的 `plugin.json`
- [ ] `plugin.json` 的 `entranceAssembly` 对应 DLL 确实存在于包中
- [ ] 包内不含宿主程序集：`LanMountainDesktop.PluginSdk.dll`、`Avalonia*.dll` 或 `sharedContracts` 声明的共享契约 DLL
- [ ] 中英文本地化文件完整；插件已在亮色和暗色主题下实际加载运行

### 市场元数据

- [ ] `publication.sha256` 和 `publication.packageSizeBytes` 由最终 `.laapp` 计算，且与 Release Asset 一致
- [ ] `publication.packageSources` 恰好包含三项，并按 `releaseAsset` → `rawFallback` → `workspaceLocal` 排列
- [ ] Release Tag、包名、下载 URL、插件 ID、版本、API 和入口 DLL 在两份清单中一致
- [ ] 新共享契约（如有）已单独提供下载 URL、SHA-256 和大小；共享契约 DLL 未打入 `.laapp`

### PR 与验证

- [ ] 只修改了 `airappmarket/registry/official-plugins.json`，未修改生成文件 `airappmarket/index.json`
- [ ] 已从 LanAirApp 仓库根目录运行下方本地验证，结果全部通过
- [ ] 本 PR 的 **AirAppMarket Validate** CI 已通过

将 `$Package` 和 `$Manifest` 指向最终发布的两个 Asset（可先下载到本地）：

```powershell
$Package = ".\LanMountainDesktop.YourPlugin.1.0.0.laapp"
$Manifest = ".\market-manifest.json"
$PluginId = "LanMountainDesktop.YourPlugin"

pwsh -File .\scripts\Test-PluginPackage.ps1 -PackagePath $Package -RequireCanonicalFileName
dotnet run --project .\airappmarket\tools\AirAppMarket.IndexBuilder --configuration Release -- --validate-release-package $Package --market-manifest $Manifest --plugin-id $PluginId
dotnet run --project .\airappmarket\tools\AirAppMarket.IndexBuilder --configuration Release -- --registry .\airappmarket\registry\official-plugins.json --validate-registry-only
```

## 共享契约（可选）/ Shared Contracts

<!-- 仅当 plugin.json 声明了尚未收录的 sharedContracts 时填写；否则删除本节。 -->

| 契约 ID / 版本 | 程序集名称 | 下载 URL | SHA-256 / bytes |
|-----------------|------------|----------|-----------------|
| <!-- ID@1.0.0 --> | <!-- Contract.dll --> | <!-- https://... --> | <!-- sha256 / size --> |

## 截图与补充说明 / Screenshots and Notes

<!-- 附上桌面组件或设置页截图，以及审核者需要了解的特殊依赖、权限或已知问题。 -->
