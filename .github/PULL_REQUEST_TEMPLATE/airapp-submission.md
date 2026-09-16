---
name: 轻应用收录申请 / AirApp Submission
about: 向 LanAirApp 官方市场提交轻应用收录申请
title: "[AirApp] 收录 <轻应用ID>"
labels: airapp-submission
---

## 轻应用信息 / AirApp Information

| 字段 | 值 |
|------|-----|
| **轻应用 ID** | <!-- airapp.json 中的 id，如 com.example.myapp --> |
| **名称** | <!-- 如 My AirApp --> |
| **作者** | <!-- 如 YourName --> |
| **简介** | <!-- 一句话描述功能 --> |
| **仓库 URL** | <!-- 必须为 github.com 上的公开仓库，如 https://github.com/owner/repo --> |
| **最新 Release Tag** | <!-- 如 v1.0.0 --> |
| **.laapp Asset 名称** | <!-- 如 MyAirApp.1.0.0.laapp --> |
| **apiVersion** | <!-- 必须为 1.0.0 --> |
| **最低宿主版本** | <!-- 如 0.9.1 --> |

## 注册表条目 / Registry Entry

请在下方填写要添加到 `airappmarket/registry/official-airapps.json` 的条目（JSON 格式）：

```json
{
  "id": "<轻应用ID>",
  "repositoryUrl": "<仓库URL>",
  "marketManifestAssetName": "market-manifest.json",
  "projectUrl": "<项目主页URL>",
  "readmeUrl": "<README URL>",
  "homepageUrl": "<主页URL>",
  "iconUrl": "<图标URL>",
  "defaultMinHostVersion": "0.9.1",
  "tags": [
    "<标签1>",
    "<标签2>"
  ],
  "capabilityHints": {
    "desktopComponents": [],
    "settingsSections": [],
    "exports": [],
    "messageTypes": []
  }
}
```

> **提示**：请参考 [收录指南](../docs/收录指南.md) 中“注册表条目字段说明”一节填写各字段。
> `desktopComponents` 会从包内 `airapp.json` 的 `components` 自动补全，通常留空即可。

## 共享契约 / Shared Contracts

<!-- 如果轻应用依赖共享契约，请填写以下信息；如果没有，删除本节 -->

| 字段 | 值 |
|------|-----|
| **契约 ID** | <!-- 如 LanMountainDesktop.SharedContracts.YourContract --> |
| **版本** | <!-- 如 1.0.0 --> |
| **程序集名称** | <!-- 如 YourContract.dll --> |
| **SHA256** | <!-- 契约 DLL 的 SHA256 哈希 --> |
| **文件大小 (bytes)** | <!-- 契约 DLL 的文件大小 --> |

> 契约程序集需要一并提交到 `airappmarket/contracts/<契约ID>/<版本>/`，
> 并在注册表的 `contracts` 数组中登记。索引里没有登记的契约会让轻应用装得上、载不起来。

## 自检清单 / Pre-submission Checklist

<!-- 提交前请逐项确认，在 [ ] 中填入 x 标记已完成项 -->

### 必要条件

- [ ] 使用 `LanMountainDesktop.AirAppSdk`，`airapp.json` 中 `apiVersion` 为 `1.0.0`
- [ ] 仓库为 GitHub 上的公开仓库
- [ ] 已创建 GitHub Release，并上传 `.laapp` 文件作为 Release Asset
- [ ] `.laapp` 包内包含合法的 `airapp.json`（不是旧的 `plugin.json`）
- [ ] `.laapp` 包内 `entranceAssembly` 指向的程序集存在
- [ ] 仓库根目录包含 `README.md`
- [ ] 提供了 `Localization/zh-CN.json` 和 `Localization/en-US.json` 本地化文件
- [ ] `airapp.json` 中 `id` 与注册表条目中 `id` 一致

### 推荐条件

- [ ] Release 中同时上传了 `market-manifest.json` 作为 Asset
- [ ] 在 LanMountainDesktop 中实际加载并运行正常
- [ ] 设置页（如有）功能正常
- [ ] 桌面组件（如有）显示和交互正常
- [ ] 亮色/暗色主题下显示正常
- [ ] 中英文本地化文本完整

### 注册表变更

- [ ] 仅修改了 `airappmarket/registry/official-airapps.json`，未修改 `airappmarket/index.json`（索引由 CI 自动生成）
- [ ] 注册表条目 JSON 格式正确
- [ ] `repositoryUrl` 为有效的 `https://github.com/{owner}/{repo}` 地址

## 截图 / Screenshots

<!-- 如有桌面组件或设置页，请附上截图 -->

| 桌面组件 | 设置页 |
|----------|--------|
| <!-- 截图 --> | <!-- 截图 --> |

## 补充说明 / Additional Notes

<!-- 任何需要审核者了解的信息，如已知问题、特殊依赖等 -->
