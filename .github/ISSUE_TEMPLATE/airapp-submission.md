---
name: 轻应用收录申请 / AirApp Submission Request
about: 申请将你的轻应用收录到 LanAirApp 官方市场
title: "[Submission] <轻应用名称>"
labels: airapp-submission
---

## 轻应用信息 / AirApp Information

| 字段 | 值 |
|------|-----|
| **轻应用 ID** | <!-- airapp.json 中的 id，如 com.example.myapp --> |
| **名称** | <!-- 如 My AirApp --> |
| **作者** | <!-- 如 YourName --> |
| **简介** | <!-- 一句话描述功能 --> |
| **仓库 URL** | <!-- 必须为 github.com 上的公开仓库 --> |
| **最新版本** | <!-- 如 v1.0.0 --> |
| **apiVersion** | <!-- 必须为 1.0.0 --> |
| **最低宿主版本** | <!-- 如 0.9.1 --> |

## 功能描述 / Feature Description

<!-- 详细描述功能、使用场景和目标用户 -->

## 桌面组件 / Desktop Components

<!-- 列出提供的桌面组件（如有） -->

## 设置页 / Settings Sections

<!-- 列出提供的设置页（如有） -->

## 共享契约 / Shared Contracts

<!-- 列出导出的共享契约（如有） -->

## 自检清单 / Checklist

- [ ] 使用 `LanMountainDesktop.AirAppSdk`，`airapp.json` 中 `apiVersion` 为 `1.0.0`
- [ ] 仓库为 GitHub 上的公开仓库
- [ ] 已创建 GitHub Release 并上传 `.laapp` 文件
- [ ] `.laapp` 包内包含合法的 `airapp.json` 和入口程序集
- [ ] 提供了中英文本地化文件
- [ ] 在 LanMountainDesktop 中实际运行正常
- [ ] 亮色/暗色主题下显示正常

## 截图 / Screenshots

<!-- 附上桌面组件和设置页的截图 -->

## 补充说明 / Additional Notes

<!-- 任何需要审核者了解的信息 -->
