# Legacy AirApp sample audit

## Status

`NotesApp`, `SystemMonitor`, and `WeatherWidget` are historical third-party AirApp API 1.0.0 design prototypes. They are not production-installable applications and are intentionally excluded from:

- `LanAirApp.slnx`
- repository CI build entry points
- the Plugin SDK 5 `.laapp` packaging flow
- the traditional Plugin SDK 5 market registry

Their `PackageReference` and `airapp.json` API versions remain at 1.0.0 on purpose. Changing those strings to 6.0.0 would imply a supported migration that the current host and packaging chain cannot provide.

For a currently supported example, use [`LanMountainDesktop.SamplePlugin`](./LanMountainDesktop.SamplePlugin/README.md). It consumes the authoritative Plugin SDK 5.0.0 package, uses `plugin.json`, builds in `LanAirApp.slnx`, and follows the production plugin market contract.

## Source-level API audit

The prototype source was compared with the authoritative `LanMountainDesktop.AirAppSdk` 6.0.0 surface in the sibling host repository. The main members used by the samples still exist:

- `[AirAppEntrance]`, `AirAppBase.Initialize`, and `AirAppBase.OnStartedAsync`
- `AddAirAppComponent`, `AddAirAppWindow`, and `AirAppComponentOptions`
- `AirAppWidgetBase.Context`, `OnAttachedCore`, `OnDetachedCore`, and `OnAppearanceChangedCore`
- `AirAppWindowBase.Descriptor`, `OnWindowOpeningAsync`, and `IAirAppRuntimeContext.OpenWindowAsync`

This is only a source-signature comparison. It does not establish runtime compatibility, installation compatibility, or a releasable package.

## Blocking production gaps

### 1. No third-party AirApp host loader

The production Host, Runtime, AirAppHost, and Launcher do not reference the AirApp SDK as a third-party extension runtime. There is no implementation that scans `airapp.json`, loads an entrance assembly, finds `[AirAppEntrance]`, creates its service provider, registers components/windows, or invokes the SDK lifecycle.

### 2. The production `.laapp` route is the Plugin SDK 5 route

The current installer treats `.laapp` as a Plugin SDK package and requires `plugin.json` at the archive root. An AirApp prototype archive containing `airapp.json` is therefore rejected rather than loaded. These samples must not be submitted to the Plugin SDK 5 market registry.

### 3. No supported AirApp build-to-host toolchain

AirAppDevServer's preview path is still a TODO and does not launch a real preview host. Its packager creates a ZIP with `airapp.json`, but that output has no production install route. In addition, the authoritative AirAppSdk 6.0.0 NuGet project declares build/buildTransitive packaging resources while the actual package contains no such targets, so a normal project build does not automatically produce the documented AirApp package. There is no end-to-end host integration test covering discovery, installation, load, lifecycle, widgets, or windows.

## Required gates before migration

Do not update these samples to AirApp API 6 or add them to build/CI until all of the following exist:

1. A production third-party AirApp loader wired to the authoritative SDK lifecycle.
2. An unambiguous installation route for `airapp.json` packages that cannot be confused with Plugin SDK 5 packages.
3. A supported, validated packager whose output the production installer accepts.
4. End-to-end tests proving manifest validation, assembly loading, component/window registration, lifecycle, unload, and failure isolation.
5. A documented market schema and release contract dedicated to AirApp API 6.

Once those gates are met, migrate package references and manifests together, choose new sample versions, add the projects to an AirApp-specific solution/CI job, and validate generated packages against the production host. Until then, these directories remain documentation-only historical prototypes.
