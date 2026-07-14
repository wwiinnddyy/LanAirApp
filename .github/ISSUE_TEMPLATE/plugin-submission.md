---
name: 轻应用上架申请 / Plugin Submission Request
about: 提交 PluginSdk 5 Release，由维护者代为加入 LanAirApp 市场
title: "[Submission] <插件ID> v<版本>"
labels: plugin-submission
---

> [!IMPORTANT]
> 生产市场当前只接收 **LanMountainDesktop PluginSdk API `5.0.0`** 插件：安装包必须是根目录含 `plugin.json` 的 `.laapp`。使用 `airapp.json` 的 AirApp API 1 历史原型和 API 6 设计原型尚不能在生产宿主安装或加载，不能提交上架。

无需修改市场仓库：先发布两个 Release Asset，再填写本 Issue。维护者会代为添加注册表条目。详细示例见 [收录指南](../../docs/收录指南.md)。

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

## 上架清单 / Submission Checklist

- [ ] 仓库可公开访问，根目录含 `README.md`
- [ ] Release Tag 为 `vX.Y.Z`，包名严格为 `<插件ID>.X.Y.Z.laapp`
- [ ] 同名 `.laapp` 已放在插件仓库 `main` 根目录，且与 Release Asset 的 SHA-256/大小一致，可作为 `rawFallback`
- [ ] `.laapp` 根目录含 API `5.0.0` 的 `plugin.json`，且 `entranceAssembly` 对应 DLL 存在
- [ ] `market-manifest.json` 使用 schema `2.0.0`，`minHostVersion` 不低于 `0.8.6`
- [ ] 两个清单中的插件 ID、版本、API、入口 DLL、Release Tag 和包名完全一致
- [ ] SHA-256 和 `packageSizeBytes` 与最终 Release Asset 一致
- [ ] `packageSources` 恰好按 `releaseAsset` → `rawFallback` → `workspaceLocal` 提供三项
- [ ] 包内不含 `LanMountainDesktop.PluginSdk.dll`、`Avalonia*.dll` 或共享契约 DLL
- [ ] 插件已在 LanMountainDesktop 中实际加载，亮色/暗色主题和中英文本地化正常
- [ ] 已运行下方本地验证并在上表填写结果

从 LanAirApp 仓库根目录运行（将路径替换为下载后的 Release Asset）：

```powershell
$Package = ".\LanMountainDesktop.YourPlugin.1.0.0.laapp"
$Manifest = ".\market-manifest.json"
$PluginId = "LanMountainDesktop.YourPlugin"

pwsh -File .\scripts\Test-PluginPackage.ps1 -PackagePath $Package -RequireCanonicalFileName
dotnet run --project .\airappmarket\tools\AirAppMarket.IndexBuilder --configuration Release -- --validate-release-package $Package --market-manifest $Manifest --plugin-id $PluginId
```

## 共享契约（可选）/ Shared Contracts

<!-- 仅当 plugin.json 声明了尚未收录的 sharedContracts 时填写；共享契约 DLL 不应打进 .laapp。 -->

| 契约 ID / 版本 | 程序集名称 | 下载 URL | SHA-256 / bytes |
|-----------------|------------|----------|-----------------|
| <!-- ID@1.0.0 --> | <!-- Contract.dll --> | <!-- https://... --> | <!-- sha256 / size --> |

## 截图与补充说明 / Screenshots and Notes

<!-- 附上桌面组件或设置页截图，以及特殊依赖、权限或已知问题。 -->
