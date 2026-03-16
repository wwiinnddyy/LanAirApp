# 插件打包指南

## 中文

阑山桌面插件的标准安装格式为 `.laapp`。它本质上是一个包含插件输出内容的压缩包，宿主会优先按 `.laapp` 包进行安装和索引。

### 打包内容建议

- `plugin.json`
- 插件主程序集
- 插件依赖程序集
- `Localization/zh-CN.json`
- `Localization/en-US.json`
- 插件运行所需的其他资源文件

### 目录与分发约定

- 插件项目构建后，应将 `.laapp` 文件放在插件仓库根目录。
- 插件仓库根目录同时应提供 `README.md`。
- 官方市场只维护索引，不再要求把插件包提交到 `LanAirApp/releases/`。
- `LanAirApp/airappmarket/index.json` 负责记录插件仓库根目录、安装包链接和校验信息。

### 使用打包工具

```powershell
dotnet run --project .\LanAirApp\tools\LanMountainDesktop.PluginPackager -- --input .\path\to\plugin-output --output .\YourPlugin.1.0.0.laapp --overwrite
```

### 宿主安装流程

1. 阑山桌面从官方市场索引读取插件列表。
2. 用户在市场中选择插件。
3. 宿主下载对应插件仓库根目录下的 `.laapp`。
4. 宿主校验哈希并暂存安装结果。
5. 重启应用后加载新插件。

### 注意事项

- `entranceAssembly` 必须能在包内找到。
- 不要把无关开发产物打进安装包。
- 同版本包名建议稳定，例如 `YourPlugin.1.0.0.laapp`。
- README 应说明插件用途、能力和安装后的行为。

## English

The standard installation format for LanMountainDesktop plugins is `.laapp`. It is essentially a packaged archive containing the plugin runtime output.

### Packaging rules

- Put the generated `.laapp` file in the plugin repository root.
- Keep `README.md` in the same repository root.
- API `3.0.0` plugins must include `plugin.json`, the main plugin assembly, the matching `.deps.json`, and any managed/native dependency files required by that dependency graph.
- Private NuGet dependencies are supported. Package them inside the `.laapp` by zipping the full plugin output directory rather than cherry-picking files.
- If a plugin depends on shared contract assemblies, declare them in `plugin.json.sharedContracts[]`. Those contracts are resolved and cached by the host in a version-isolated directory.
- The official market index stores metadata and links; it no longer relies on `LanAirApp/releases/` as the primary source.
- The host downloads the package, validates its hash, and stages the installation.
