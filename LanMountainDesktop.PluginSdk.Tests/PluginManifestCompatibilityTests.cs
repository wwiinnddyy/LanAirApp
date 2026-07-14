using System.Text;
using LanMountainDesktop.PluginSdk;
using Xunit;

namespace LanMountainDesktop.PluginSdk.Tests;

public sealed class PluginManifestCompatibilityTests
{
    [Fact]
    public void SdkBaselineMatchesProductionHost()
    {
        Assert.Equal("5.0.0", PluginSdkInfo.ApiVersion);
        Assert.Equal(".laapp", PluginSdkInfo.PackageFileExtension);
    }

    [Fact]
    public void MissingRuntimeDefaultsToInProcess()
    {
        var manifest = LoadManifest("""
            {
              "id": "LanMountainDesktop.Tests.DefaultRuntime",
              "name": "Default Runtime",
              "entranceAssembly": "DefaultRuntime.dll",
              "apiVersion": "5.0.0"
            }
            """);

        Assert.Equal(PluginRuntimeMode.InProcess, manifest.RuntimeMode);
        Assert.Equal(PluginRuntimeModes.InProcess, manifest.Runtime?.Mode);
    }

    [Theory]
    [InlineData("in-proc", PluginRuntimeMode.InProcess)]
    [InlineData("isolated-background", PluginRuntimeMode.IsolatedBackground)]
    [InlineData("isolated-window", PluginRuntimeMode.IsolatedWindow)]
    public void SupportedRuntimeModesAreNormalized(string manifestValue, PluginRuntimeMode expected)
    {
        var manifest = LoadManifest($$"""
            {
              "id": "LanMountainDesktop.Tests.Runtime",
              "name": "Runtime",
              "entranceAssembly": "Runtime.dll",
              "apiVersion": "5.0.0",
              "runtime": { "mode": "  {{manifestValue.ToUpperInvariant()}}  " }
            }
            """);

        Assert.Equal(expected, manifest.RuntimeMode);
        Assert.Equal(manifestValue, manifest.Runtime?.Mode);
    }

    [Fact]
    public void UnsupportedRuntimeModeIsRejected()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => LoadManifest("""
            {
              "id": "LanMountainDesktop.Tests.BadRuntime",
              "name": "Bad Runtime",
              "entranceAssembly": "BadRuntime.dll",
              "apiVersion": "5.0.0",
              "runtime": { "mode": "external-process" }
            }
            """));

        Assert.Contains("unsupported runtime mode", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ApiV4ManifestIsRejectedByV5Sdk()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => LoadManifest("""
            {
              "id": "LanMountainDesktop.Tests.Legacy",
              "name": "Legacy",
              "entranceAssembly": "Legacy.dll",
              "apiVersion": "4.0.0"
            }
            """));

        Assert.Contains("targets API version '4.0.0'", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("host provides '5.0.0'", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static PluginManifest LoadManifest(string json)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        return PluginManifest.Load(stream, "test-plugin.json");
    }
}
