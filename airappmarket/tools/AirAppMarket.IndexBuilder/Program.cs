using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

return await RunAsync(args);

static async Task<int> RunAsync(string[] args)
{
    try
    {
        var options = CliOptions.Parse(args);
        if (options.ShowHelp)
        {
            PrintHelp();
            return 0;
        }

        var registryPath = Path.GetFullPath(options.RegistryPath);
        var outputPath = Path.GetFullPath(options.OutputPath);

        if (!File.Exists(registryPath))
        {
            throw new FileNotFoundException($"Official plugin registry '{registryPath}' was not found.", registryPath);
        }

        var registry = await RegistryDocument.LoadAsync(registryPath, CancellationToken.None);

        using var httpClient = CreateHttpClient();
        var builder = new MarketIndexBuilder(httpClient);
        object index = options.Mode switch
        {
            MarketIndexMode.Index => await builder.BuildIndexAsync(registry, CancellationToken.None),
            MarketIndexMode.Snapshot => await builder.BuildSnapshotAsync(registry, options.RequireMarketManifest, CancellationToken.None),
            _ => throw new InvalidOperationException($"Unsupported market index mode '{options.Mode}'.")
        };

        var outputDirectory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        await WriteJsonAsync(outputPath, index, CancellationToken.None);

        Console.WriteLine($"Generated market index at '{outputPath}'.");
        if (index is MarketIndexIndexV3 pureIndex)
        {
            Console.WriteLine($"Plugins: {pureIndex.Plugins.Count}");
        }
        else if (index is MarketIndexV2 snapshot)
        {
            Console.WriteLine($"Plugins: {snapshot.Plugins.Count}");
            Console.WriteLine($"Contracts: {snapshot.Contracts.Count}");
        }
        return 0;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine(ex.Message);
        return 1;
    }
}

static HttpClient CreateHttpClient()
{
    var client = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(60)
    };
    client.DefaultRequestHeaders.UserAgent.ParseAdd("LanAirApp-IndexBuilder/2.0");
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
    return client;
}

static async Task WriteJsonAsync<T>(string path, T index, CancellationToken cancellationToken)
{
    var serializerOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    var json = JsonSerializer.Serialize(index, serializerOptions) + Environment.NewLine;
    var encoding = new UTF8Encoding(false);
    await File.WriteAllTextAsync(path, json, encoding, cancellationToken);
}

static void PrintHelp()
{
    Console.WriteLine("AirAppMarket.IndexBuilder");
    Console.WriteLine();
    Console.WriteLine("Builds airappmarket/index.json from the official plugin registry.");
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  dotnet run --project airappmarket/tools/AirAppMarket.IndexBuilder -- [options]");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  --registry <path>                 Path to registry JSON.");
    Console.WriteLine("                                    Default: airappmarket/registry/official-plugins.json");
    Console.WriteLine("  --output <path>                   Path to generated market index JSON.");
    Console.WriteLine("                                    Default: airappmarket/index.json");
    Console.WriteLine("  --mode <index|snapshot>           Build a pure index or a snapshot index.");
    Console.WriteLine("                                    Default: index");
    Console.WriteLine("  --snapshot                        Shortcut for --mode snapshot.");
    Console.WriteLine("  --require-market-manifest         Fail when market-manifest.json release asset is missing.");
    Console.WriteLine("  --help                            Show help.");
}

enum MarketIndexMode
{
    Index,
    Snapshot
}

internal sealed class CliOptions
{
    public string RegistryPath { get; private set; } = Path.Combine("airappmarket", "registry", "official-plugins.json");
    public string OutputPath { get; private set; } = Path.Combine("airappmarket", "index.json");
    public bool RequireMarketManifest { get; private set; }
    public bool ShowHelp { get; private set; }
    public MarketIndexMode Mode { get; private set; } = MarketIndexMode.Index;

    public static CliOptions Parse(string[] args)
    {
        var options = new CliOptions();
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg)
            {
                case "--registry":
                    options.RegistryPath = ReadValue(args, ref i, "--registry");
                    break;
                case "--output":
                    options.OutputPath = ReadValue(args, ref i, "--output");
                    break;
                case "--mode":
                    options.Mode = ParseMode(ReadValue(args, ref i, "--mode"));
                    break;
                case "--snapshot":
                    options.Mode = MarketIndexMode.Snapshot;
                    break;
                case "--require-market-manifest":
                    options.RequireMarketManifest = true;
                    break;
                case "--help":
                case "-h":
                    options.ShowHelp = true;
                    break;
                default:
                    throw new InvalidOperationException($"Unknown option '{arg}'.");
            }
        }

        return options;
    }

    private static MarketIndexMode ParseMode(string value)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "index" => MarketIndexMode.Index,
            "snapshot" => MarketIndexMode.Snapshot,
            _ => throw new InvalidOperationException($"Unknown market index mode '{value}'.")
        };
    }

    private static string ReadValue(string[] args, ref int index, string option)
    {
        var next = index + 1;
        if (next >= args.Length)
        {
            throw new InvalidOperationException($"Option '{option}' requires a value.");
        }

        index = next;
        return args[next];
    }
}

internal sealed class MarketIndexBuilder
{
    private readonly HttpClient _httpClient;

    public MarketIndexBuilder(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<MarketIndexIndexV3> BuildIndexAsync(
        RegistryDocument registry,
        CancellationToken cancellationToken)
    {
        var plugins = new List<MarketPluginIndexV3>();
        foreach (var plugin in registry.Plugins)
        {
            var built = await BuildPluginIndexAsync(plugin, cancellationToken);
            plugins.Add(built);
        }

        return new MarketIndexIndexV3
        {
            SchemaVersion = "3.0.0",
            SourceId = registry.SourceId.Trim(),
            SourceName = registry.SourceName.Trim(),
            GeneratedAt = DateTimeOffset.UtcNow,
            Plugins = plugins
                .OrderBy(plugin => plugin.PluginId, StringComparer.OrdinalIgnoreCase)
                .ToList()
        };
    }

    public async Task<MarketIndexV2> BuildSnapshotAsync(
        RegistryDocument registry,
        bool requireMarketManifest,
        CancellationToken cancellationToken)
    {
        var plugins = new List<MarketPluginV2>();
        foreach (var plugin in registry.Plugins)
        {
            var built = await BuildPluginAsync(registry, plugin, requireMarketManifest, cancellationToken);
            plugins.Add(built);
        }

        var contracts = registry.Contracts
            .OrderBy(contract => contract.Id, StringComparer.OrdinalIgnoreCase)
            .ThenBy(contract => contract.Version, StringComparer.OrdinalIgnoreCase)
            .Select(contract => new MarketContractV2
            {
                Id = contract.Id.Trim(),
                Version = contract.Version.Trim(),
                AssemblyName = contract.AssemblyName.Trim(),
                DownloadUrl = contract.DownloadUrl.Trim(),
                Sha256 = contract.Sha256.Trim().ToLowerInvariant(),
                PackageSizeBytes = contract.PackageSizeBytes
            })
            .ToList();

        return new MarketIndexV2
        {
            SchemaVersion = "2.0.0",
            SourceId = registry.SourceId.Trim(),
            SourceName = registry.SourceName.Trim(),
            GeneratedAt = DateTimeOffset.UtcNow,
            Contracts = contracts,
            Plugins = plugins
                .OrderBy(plugin => plugin.Manifest.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(plugin => plugin.PluginId, StringComparer.OrdinalIgnoreCase)
                .ToList()
        };
    }

    private async Task<MarketPluginIndexV3> BuildPluginIndexAsync(
        RegistryPlugin plugin,
        CancellationToken cancellationToken)
    {
        var repo = GitHubRepositoryIdentity.Parse(plugin.RepositoryUrl);
        var release = await GetLatestReleaseAsync(repo, cancellationToken);
        var packageAsset = SelectPackageAsset(plugin, release);
        var packageSources = BuildPackageSources(repo, packageAsset.Name, packageAsset.BrowserDownloadUrl);

        return new MarketPluginIndexV3
        {
            PluginId = plugin.Id.Trim(),
            Repository = new MarketPluginRepositoryIndexV3
            {
                IconUrl = FirstNonEmpty(plugin.IconUrl) ?? string.Empty,
                ProjectUrl = FirstNonEmpty(plugin.ProjectUrl, plugin.RepositoryUrl) ?? string.Empty,
                ReadmeUrl = FirstNonEmpty(plugin.ReadmeUrl) ?? string.Empty,
                HomepageUrl = FirstNonEmpty(plugin.HomepageUrl, plugin.ProjectUrl, plugin.RepositoryUrl) ?? string.Empty,
                RepositoryUrl = FirstNonEmpty(plugin.RepositoryUrl) ?? string.Empty
            },
            Publication = new MarketPluginPublicationIndexV3
            {
                PackageSources = packageSources
            }
        };
    }

    private async Task<MarketPluginV2> BuildPluginAsync(
        RegistryDocument registry,
        RegistryPlugin plugin,
        bool requireMarketManifest,
        CancellationToken cancellationToken)
    {
        var repo = GitHubRepositoryIdentity.Parse(plugin.RepositoryUrl);
        var release = await GetLatestReleaseAsync(repo, cancellationToken);
        var packageAsset = SelectPackageAsset(plugin, release);

        var manifestAssetName = string.IsNullOrWhiteSpace(plugin.MarketManifestAssetName)
            ? registry.DefaultMarketManifestAssetName
            : plugin.MarketManifestAssetName!;
        var manifestAsset = release.Assets.FirstOrDefault(asset =>
            string.Equals(asset.Name, manifestAssetName, StringComparison.OrdinalIgnoreCase));
        if (requireMarketManifest && manifestAsset is null)
        {
            throw new InvalidOperationException(
                $"Latest release '{release.TagName}' for '{plugin.Id}' does not contain '{manifestAssetName}'.");
        }
        if (manifestAsset is null)
        {
            Console.Error.WriteLine(
                $"Warning: Latest release '{release.TagName}' for '{plugin.Id}' does not contain '{manifestAssetName}'. Falling back to release/package metadata.");
        }

        MarketManifestMetadata? marketMetadata = null;
        if (manifestAsset is not null)
        {
            var manifestText = await DownloadTextAsync(manifestAsset.BrowserDownloadUrl, cancellationToken);
            marketMetadata = MarketManifestMetadata.Parse(manifestText);
        }

        var temporaryPackagePath = Path.Combine(
            Path.GetTempPath(),
            "LanAirApp",
            "AirAppMarket.IndexBuilder",
            $"{Guid.NewGuid():N}.{packageAsset.Name}");
        Directory.CreateDirectory(Path.GetDirectoryName(temporaryPackagePath)!);

        try
        {
            await DownloadFileAsync(packageAsset.BrowserDownloadUrl, temporaryPackagePath, cancellationToken);
            var packageManifest = ReadPackageManifest(temporaryPackagePath);
            var packageInfo = ComputePackageInfo(temporaryPackagePath);

            var minHostVersion = FirstNonEmpty(
                marketMetadata?.MinHostVersion,
                plugin.DefaultMinHostVersion,
                "0.0.1")!;
            var tags = MergeTags(
                plugin.Tags,
                marketMetadata?.Tags);

            var capabilityHints = MarketCapabilityHints.Merge(plugin.CapabilityHints, marketMetadata?.Capabilities);

            var releaseNotes = FirstNonEmpty(
                marketMetadata?.ReleaseNotes,
                release.Body,
                $"Release {release.TagName}")!;

            var repository = new MarketPluginRepositoryV2
            {
                IconUrl = FirstNonEmpty(marketMetadata?.IconUrl, plugin.IconUrl)!,
                ProjectUrl = FirstNonEmpty(marketMetadata?.ProjectUrl, plugin.ProjectUrl, plugin.RepositoryUrl)!,
                ReadmeUrl = FirstNonEmpty(marketMetadata?.ReadmeUrl, plugin.ReadmeUrl)!,
                HomepageUrl = FirstNonEmpty(marketMetadata?.HomepageUrl, plugin.HomepageUrl, plugin.ProjectUrl, plugin.RepositoryUrl)!,
                RepositoryUrl = FirstNonEmpty(marketMetadata?.RepositoryUrl, plugin.RepositoryUrl)!,
                Tags = tags,
                ReleaseNotes = releaseNotes
            };

            var publishedAt = release.PublishedAt ?? release.CreatedAt ?? DateTimeOffset.UtcNow;
            var updatedAt = release.PublishedAt ?? release.CreatedAt ?? DateTimeOffset.UtcNow;
            var releaseTag = release.TagName.Trim();
            if (!releaseTag.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            {
                releaseTag = $"v{releaseTag}";
            }

            var packageSources = BuildPackageSources(repo, packageAsset.Name, packageAsset.BrowserDownloadUrl);
            var publication = new MarketPluginPublicationV2
            {
                ReleaseTag = releaseTag,
                ReleaseAssetName = packageAsset.Name,
                PublishedAt = publishedAt,
                UpdatedAt = updatedAt,
                PackageSizeBytes = packageInfo.PackageSizeBytes,
                Sha256 = packageInfo.Sha256,
                Md5 = packageInfo.Md5,
                PackageSources = packageSources
            };

            return new MarketPluginV2
            {
                PluginId = packageManifest.Id,
                Manifest = new MarketPluginManifestV2
                {
                    Id = packageManifest.Id,
                    Name = FirstNonEmpty(marketMetadata?.Name, packageManifest.Name)!,
                    Description = FirstNonEmpty(marketMetadata?.Description, packageManifest.Description)!,
                    Author = FirstNonEmpty(marketMetadata?.Author, packageManifest.Author)!,
                    Version = packageManifest.Version,
                    ApiVersion = packageManifest.ApiVersion,
                    EntranceAssembly = packageManifest.EntranceAssembly,
                    SharedContracts = packageManifest.SharedContracts
                        .Select(contract => new MarketSharedContractV2
                        {
                            Id = contract.Id,
                            Version = contract.Version,
                            AssemblyName = contract.AssemblyName
                        })
                        .ToList()
                },
                Compatibility = new MarketPluginCompatibilityV2
                {
                    MinHostVersion = minHostVersion,
                    PluginApiVersion = packageManifest.ApiVersion
                },
                Capabilities = new MarketPluginCapabilitiesV2
                {
                    SharedContracts = packageManifest.SharedContracts
                        .Select(contract => new MarketSharedContractV2
                        {
                            Id = contract.Id,
                            Version = contract.Version,
                            AssemblyName = contract.AssemblyName
                        })
                        .ToList(),
                    DesktopComponents = capabilityHints.DesktopComponents,
                    SettingsSections = capabilityHints.SettingsSections,
                    Exports = capabilityHints.Exports,
                    MessageTypes = capabilityHints.MessageTypes
                },
                Repository = repository,
                Publication = publication
            };
        }
        finally
        {
            TryDeleteFile(temporaryPackagePath);
        }
    }

    private static List<string> MergeTags(IReadOnlyList<string> registryTags, IReadOnlyList<string>? metadataTags)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var tag in registryTags)
        {
            if (!string.IsNullOrWhiteSpace(tag))
            {
                set.Add(tag.Trim());
            }
        }

        if (metadataTags is not null)
        {
            foreach (var tag in metadataTags)
            {
                if (!string.IsNullOrWhiteSpace(tag))
                {
                    set.Add(tag.Trim());
                }
            }
        }

        return set.OrderBy(tag => tag, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static List<MarketPluginPackageSourceV2> BuildPackageSources(
        GitHubRepositoryIdentity repository,
        string releaseAssetName,
        string releaseDownloadUrl)
    {
        var encodedAssetName = Uri.EscapeDataString(releaseAssetName);
        return
        [
            new MarketPluginPackageSourceV2
            {
                Kind = "releaseAsset",
                Url = releaseDownloadUrl
            },
            new MarketPluginPackageSourceV2
            {
                Kind = "rawFallback",
                Url = $"https://raw.githubusercontent.com/{repository.Owner}/{repository.Name}/main/{encodedAssetName}"
            },
            new MarketPluginPackageSourceV2
            {
                Kind = "workspaceLocal",
                Url = $"workspace://{repository.Name}/{encodedAssetName}"
            }
        ];
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }
        return null;
    }

    private static MarketPluginManifestData ReadPackageManifest(string packagePath)
    {
        using var archive = ZipFile.OpenRead(packagePath);
        var entry = archive.Entries.FirstOrDefault(candidate =>
            string.Equals(candidate.FullName, "plugin.json", StringComparison.OrdinalIgnoreCase));
        if (entry is null)
        {
            throw new InvalidOperationException($"Package '{packagePath}' does not contain plugin.json.");
        }

        using var stream = entry.Open();
        using var reader = new StreamReader(stream, Encoding.UTF8, true);
        var text = reader.ReadToEnd();
        var model = JsonSerializer.Deserialize<MarketPluginManifestData>(text, JsonSerializerOptionsProvider.CaseInsensitive)
            ?? throw new InvalidOperationException($"Package '{packagePath}' contains an invalid plugin.json.");
        model.Validate(packagePath);
        return model;
    }

    private static PackageInfo ComputePackageInfo(string packagePath)
    {
        var fileInfo = new FileInfo(packagePath);
        using var stream = File.OpenRead(packagePath);
        var hash = SHA256.HashData(stream);
        stream.Position = 0;
        var md5 = MD5.HashData(stream);
        return new PackageInfo(
            fileInfo.Length,
            Convert.ToHexString(hash).ToLowerInvariant(),
            Convert.ToHexString(md5).ToLowerInvariant());
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Ignore temp file cleanup issues.
        }
    }

    private MarketReleaseAsset SelectPackageAsset(RegistryPlugin plugin, GitHubRelease release)
    {
        var candidates = release.Assets
            .Where(asset => asset.Name.EndsWith(".laapp", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (candidates.Count == 0)
        {
            throw new InvalidOperationException(
                $"Latest release '{release.TagName}' for '{plugin.Id}' does not contain a .laapp asset.");
        }

        if (!string.IsNullOrWhiteSpace(plugin.ReleaseAssetName))
        {
            var selected = candidates.FirstOrDefault(asset =>
                string.Equals(asset.Name, plugin.ReleaseAssetName, StringComparison.OrdinalIgnoreCase));
            if (selected is null)
            {
                throw new InvalidOperationException(
                    $"Latest release '{release.TagName}' for '{plugin.Id}' does not contain declared asset '{plugin.ReleaseAssetName}'.");
            }
            return selected;
        }

        return candidates
            .OrderByDescending(asset => asset.Size)
            .First();
    }

    private async Task<GitHubRelease> GetLatestReleaseAsync(
        GitHubRepositoryIdentity repository,
        CancellationToken cancellationToken)
    {
        var url = $"https://api.github.com/repos/{repository.Owner}/{repository.Name}/releases/latest";
        var json = await DownloadTextAsync(url, cancellationToken);
        var release = JsonSerializer.Deserialize<GitHubRelease>(json, JsonSerializerOptionsProvider.CaseInsensitive)
            ?? throw new InvalidOperationException($"Failed to parse latest release for '{repository.Owner}/{repository.Name}'.");
        release.Validate(repository);
        return release;
    }

    private async Task<string> DownloadTextAsync(string url, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private async Task DownloadFileAsync(string url, string destinationPath, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var target = File.Create(destinationPath);
        await source.CopyToAsync(target, cancellationToken);
    }
}

internal static class JsonSerializerOptionsProvider
{
    public static readonly JsonSerializerOptions CaseInsensitive = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };
}

internal sealed record PackageInfo(long PackageSizeBytes, string Sha256, string Md5);

internal sealed class RegistryDocument
{
    public string SchemaVersion { get; init; } = string.Empty;
    public string SourceId { get; init; } = string.Empty;
    public string SourceName { get; init; } = string.Empty;
    public string DefaultMarketManifestAssetName { get; init; } = "market-manifest.json";
    public List<RegistryPlugin> Plugins { get; init; } = [];
    public List<RegistryContract> Contracts { get; init; } = [];

    public static async Task<RegistryDocument> LoadAsync(string path, CancellationToken cancellationToken)
    {
        var json = await File.ReadAllTextAsync(path, cancellationToken);
        var model = JsonSerializer.Deserialize<RegistryDocument>(json, JsonSerializerOptionsProvider.CaseInsensitive)
            ?? throw new InvalidOperationException($"Failed to parse registry '{path}'.");
        model.Validate(path);
        return model;
    }

    private void Validate(string sourceName)
    {
        if (string.IsNullOrWhiteSpace(SchemaVersion))
        {
            throw new InvalidOperationException($"Registry '{sourceName}' is missing schemaVersion.");
        }

        if (string.IsNullOrWhiteSpace(SourceId) || string.IsNullOrWhiteSpace(SourceName))
        {
            throw new InvalidOperationException($"Registry '{sourceName}' is missing source metadata.");
        }

        if (Plugins.Count == 0)
        {
            throw new InvalidOperationException($"Registry '{sourceName}' does not declare plugins.");
        }

        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var plugin in Plugins)
        {
            plugin.Validate(sourceName);
            if (!ids.Add(plugin.Id))
            {
                throw new InvalidOperationException($"Registry '{sourceName}' contains duplicate plugin id '{plugin.Id}'.");
            }
        }

        var contractIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var contract in Contracts)
        {
            contract.Validate(sourceName);
            var key = $"{contract.Id}@{contract.Version}";
            if (!contractIds.Add(key))
            {
                throw new InvalidOperationException($"Registry '{sourceName}' contains duplicate contract '{key}'.");
            }
        }
    }
}

internal sealed class RegistryPlugin
{
    public string Id { get; init; } = string.Empty;
    public string RepositoryUrl { get; init; } = string.Empty;
    public string? ProjectUrl { get; init; }
    public string? ReadmeUrl { get; init; }
    public string? HomepageUrl { get; init; }
    public string? IconUrl { get; init; }
    public string? DefaultMinHostVersion { get; init; }
    public string? MarketManifestAssetName { get; init; }
    public string? ReleaseAssetName { get; init; }
    public List<string> Tags { get; init; } = [];
    public MarketCapabilityHints CapabilityHints { get; init; } = new();

    public void Validate(string sourceName)
    {
        if (string.IsNullOrWhiteSpace(Id))
        {
            throw new InvalidOperationException($"Registry '{sourceName}' has a plugin without id.");
        }

        if (string.IsNullOrWhiteSpace(RepositoryUrl))
        {
            throw new InvalidOperationException($"Registry '{sourceName}' has a plugin '{Id}' without repositoryUrl.");
        }

        _ = GitHubRepositoryIdentity.Parse(RepositoryUrl);
    }
}

internal sealed class RegistryContract
{
    public string Id { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string AssemblyName { get; init; } = string.Empty;
    public string DownloadUrl { get; init; } = string.Empty;
    public string Sha256 { get; init; } = string.Empty;
    public long PackageSizeBytes { get; init; }

    public void Validate(string sourceName)
    {
        if (string.IsNullOrWhiteSpace(Id) ||
            string.IsNullOrWhiteSpace(Version) ||
            string.IsNullOrWhiteSpace(AssemblyName) ||
            string.IsNullOrWhiteSpace(DownloadUrl) ||
            string.IsNullOrWhiteSpace(Sha256))
        {
            throw new InvalidOperationException($"Registry '{sourceName}' has an invalid contract entry.");
        }

        if (PackageSizeBytes <= 0)
        {
            throw new InvalidOperationException(
                $"Registry '{sourceName}' declares invalid packageSizeBytes for contract '{Id}@{Version}'.");
        }
    }
}

internal sealed class MarketCapabilityHints
{
    public List<string> DesktopComponents { get; init; } = [];
    public List<string> SettingsSections { get; init; } = [];
    public List<string> Exports { get; init; } = [];
    public List<string> MessageTypes { get; init; } = [];

    public static MarketCapabilityHints Merge(
        MarketCapabilityHints registry,
        MarketCapabilityHints? marketManifest)
    {
        return new MarketCapabilityHints
        {
            DesktopComponents = MergeList(registry.DesktopComponents, marketManifest?.DesktopComponents),
            SettingsSections = MergeList(registry.SettingsSections, marketManifest?.SettingsSections),
            Exports = MergeList(registry.Exports, marketManifest?.Exports),
            MessageTypes = MergeList(registry.MessageTypes, marketManifest?.MessageTypes)
        };
    }

    private static List<string> MergeList(IReadOnlyList<string> baseList, IReadOnlyList<string>? patchList)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in baseList)
        {
            if (!string.IsNullOrWhiteSpace(item))
            {
                set.Add(item.Trim());
            }
        }

        if (patchList is not null)
        {
            foreach (var item in patchList)
            {
                if (!string.IsNullOrWhiteSpace(item))
                {
                    set.Add(item.Trim());
                }
            }
        }

        return set.OrderBy(item => item, StringComparer.OrdinalIgnoreCase).ToList();
    }
}

internal sealed class MarketManifestMetadata
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? Author { get; init; }
    public string? MinHostVersion { get; init; }
    public string? IconUrl { get; init; }
    public string? ProjectUrl { get; init; }
    public string? ReadmeUrl { get; init; }
    public string? HomepageUrl { get; init; }
    public string? RepositoryUrl { get; init; }
    public string? ReleaseNotes { get; init; }
    public List<string> Tags { get; init; } = [];
    public MarketCapabilityHints Capabilities { get; init; } = new();

    public static MarketManifestMetadata Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var manifestElement = TryGetObject(root, "manifest");
        var compatibilityElement = TryGetObject(root, "compatibility");
        var repositoryElement = TryGetObject(root, "repository");
        var publicationElement = TryGetObject(root, "publication");
        var capabilitiesElement = TryGetObject(root, "capabilities");

        return new MarketManifestMetadata
        {
            Name = FirstString(manifestElement, "name") ?? FirstString(root, "name"),
            Description = FirstString(manifestElement, "description") ?? FirstString(root, "description"),
            Author = FirstString(manifestElement, "author") ?? FirstString(root, "author"),
            MinHostVersion = FirstString(compatibilityElement, "minHostVersion") ?? FirstString(root, "minHostVersion"),
            IconUrl = FirstString(repositoryElement, "iconUrl") ?? FirstString(root, "iconUrl"),
            ProjectUrl = FirstString(repositoryElement, "projectUrl") ?? FirstString(root, "projectUrl"),
            ReadmeUrl = FirstString(repositoryElement, "readmeUrl") ?? FirstString(root, "readmeUrl"),
            HomepageUrl = FirstString(repositoryElement, "homepageUrl") ?? FirstString(root, "homepageUrl"),
            RepositoryUrl = FirstString(repositoryElement, "repositoryUrl") ?? FirstString(root, "repositoryUrl"),
            ReleaseNotes = FirstString(repositoryElement, "releaseNotes")
                ?? FirstString(publicationElement, "releaseNotes")
                ?? FirstString(root, "releaseNotes"),
            Tags = ReadStringArray(repositoryElement, "tags").Concat(ReadStringArray(root, "tags"))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            Capabilities = new MarketCapabilityHints
            {
                DesktopComponents = ReadStringArray(capabilitiesElement, "desktopComponents"),
                SettingsSections = ReadStringArray(capabilitiesElement, "settingsSections"),
                Exports = ReadStringArray(capabilitiesElement, "exports"),
                MessageTypes = ReadStringArray(capabilitiesElement, "messageTypes")
            }
        };
    }

    private static JsonElement? TryGetObject(JsonElement source, string propertyName)
    {
        if (!source.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return property;
    }

    private static string? FirstString(JsonElement? source, string propertyName)
    {
        if (!source.HasValue)
        {
            return null;
        }

        if (!source.Value.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var text = property.GetString();
        return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    }

    private static List<string> ReadStringArray(JsonElement? source, string propertyName)
    {
        if (!source.HasValue)
        {
            return [];
        }

        if (!source.Value.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in property.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var text = item.GetString();
            if (!string.IsNullOrWhiteSpace(text))
            {
                set.Add(text.Trim());
            }
        }

        return set.OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToList();
    }
}

internal sealed class GitHubRelease
{
    [JsonPropertyName("tag_name")]
    public string TagName { get; init; } = string.Empty;

    [JsonPropertyName("body")]
    public string? Body { get; init; }

    [JsonPropertyName("published_at")]
    public DateTimeOffset? PublishedAt { get; init; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset? CreatedAt { get; init; }

    [JsonPropertyName("assets")]
    public List<MarketReleaseAsset> Assets { get; init; } = [];

    public void Validate(GitHubRepositoryIdentity repository)
    {
        if (string.IsNullOrWhiteSpace(TagName))
        {
            throw new InvalidOperationException($"Latest release for '{repository.Owner}/{repository.Name}' is missing tag_name.");
        }

        if (Assets.Count == 0)
        {
            throw new InvalidOperationException($"Latest release '{TagName}' for '{repository.Owner}/{repository.Name}' has no assets.");
        }
    }
}

internal sealed class MarketReleaseAsset
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("browser_download_url")]
    public string BrowserDownloadUrl { get; init; } = string.Empty;

    [JsonPropertyName("size")]
    public long Size { get; init; }
}

internal sealed class GitHubRepositoryIdentity
{
    public string Owner { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;

    public static GitHubRepositoryIdentity Parse(string repositoryUrl)
    {
        if (!Uri.TryCreate(repositoryUrl, UriKind.Absolute, out var uri) ||
            !string.Equals(uri.Host, "github.com", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Unsupported repository url '{repositoryUrl}'.");
        }

        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length < 2 || string.IsNullOrWhiteSpace(segments[0]) || string.IsNullOrWhiteSpace(segments[1]))
        {
            throw new InvalidOperationException($"Repository url '{repositoryUrl}' must point to the repository root.");
        }

        return new GitHubRepositoryIdentity
        {
            Owner = segments[0],
            Name = segments[1]
        };
    }
}

internal sealed class MarketPluginManifestData
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string ApiVersion { get; init; } = string.Empty;
    public string EntranceAssembly { get; init; } = string.Empty;
    public List<MarketSharedContractV2> SharedContracts { get; init; } = [];

    public void Validate(string packagePath)
    {
        if (string.IsNullOrWhiteSpace(Id) ||
            string.IsNullOrWhiteSpace(Name) ||
            string.IsNullOrWhiteSpace(Description) ||
            string.IsNullOrWhiteSpace(Author) ||
            string.IsNullOrWhiteSpace(Version) ||
            string.IsNullOrWhiteSpace(ApiVersion) ||
            string.IsNullOrWhiteSpace(EntranceAssembly))
        {
            throw new InvalidOperationException($"Package '{packagePath}' contains an incomplete plugin manifest.");
        }

        if (!System.Version.TryParse(ApiVersion, out _))
        {
            throw new InvalidOperationException($"Package '{packagePath}' declares invalid apiVersion '{ApiVersion}'.");
        }
    }
}

internal sealed class MarketIndexIndexV3
{
    [JsonPropertyName("schemaVersion")]
    public string SchemaVersion { get; init; } = string.Empty;

    [JsonPropertyName("sourceId")]
    public string SourceId { get; init; } = string.Empty;

    [JsonPropertyName("sourceName")]
    public string SourceName { get; init; } = string.Empty;

    [JsonPropertyName("generatedAt")]
    public DateTimeOffset GeneratedAt { get; init; }

    [JsonPropertyName("plugins")]
    public List<MarketPluginIndexV3> Plugins { get; init; } = [];
}

internal sealed class MarketPluginIndexV3
{
    [JsonPropertyName("pluginId")]
    public string PluginId { get; init; } = string.Empty;

    [JsonPropertyName("repository")]
    public MarketPluginRepositoryIndexV3 Repository { get; init; } = new();

    [JsonPropertyName("publication")]
    public MarketPluginPublicationIndexV3 Publication { get; init; } = new();
}

internal sealed class MarketPluginRepositoryIndexV3
{
    [JsonPropertyName("iconUrl")]
    public string IconUrl { get; init; } = string.Empty;

    [JsonPropertyName("projectUrl")]
    public string ProjectUrl { get; init; } = string.Empty;

    [JsonPropertyName("readmeUrl")]
    public string ReadmeUrl { get; init; } = string.Empty;

    [JsonPropertyName("homepageUrl")]
    public string HomepageUrl { get; init; } = string.Empty;

    [JsonPropertyName("repositoryUrl")]
    public string RepositoryUrl { get; init; } = string.Empty;
}

internal sealed class MarketPluginPublicationIndexV3
{
    [JsonPropertyName("packageSources")]
    public List<MarketPluginPackageSourceV2> PackageSources { get; init; } = [];
}

internal sealed class MarketIndexV2
{
    [JsonPropertyName("schemaVersion")]
    public string SchemaVersion { get; init; } = string.Empty;

    [JsonPropertyName("sourceId")]
    public string SourceId { get; init; } = string.Empty;

    [JsonPropertyName("sourceName")]
    public string SourceName { get; init; } = string.Empty;

    [JsonPropertyName("generatedAt")]
    public DateTimeOffset GeneratedAt { get; init; }

    [JsonPropertyName("contracts")]
    public List<MarketContractV2> Contracts { get; init; } = [];

    [JsonPropertyName("plugins")]
    public List<MarketPluginV2> Plugins { get; init; } = [];
}

internal sealed class MarketContractV2
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; init; } = string.Empty;

    [JsonPropertyName("assemblyName")]
    public string AssemblyName { get; init; } = string.Empty;

    [JsonPropertyName("downloadUrl")]
    public string DownloadUrl { get; init; } = string.Empty;

    [JsonPropertyName("sha256")]
    public string Sha256 { get; init; } = string.Empty;

    [JsonPropertyName("packageSizeBytes")]
    public long PackageSizeBytes { get; init; }
}

internal sealed class MarketPluginV2
{
    [JsonPropertyName("pluginId")]
    public string PluginId { get; init; } = string.Empty;

    [JsonPropertyName("manifest")]
    public MarketPluginManifestV2 Manifest { get; init; } = new();

    [JsonPropertyName("compatibility")]
    public MarketPluginCompatibilityV2 Compatibility { get; init; } = new();

    [JsonPropertyName("repository")]
    public MarketPluginRepositoryV2 Repository { get; init; } = new();

    [JsonPropertyName("publication")]
    public MarketPluginPublicationV2 Publication { get; init; } = new();

    [JsonPropertyName("capabilities")]
    public MarketPluginCapabilitiesV2 Capabilities { get; init; } = new();
}

internal sealed class MarketPluginManifestV2
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("author")]
    public string Author { get; init; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; init; } = string.Empty;

    [JsonPropertyName("apiVersion")]
    public string ApiVersion { get; init; } = string.Empty;

    [JsonPropertyName("entranceAssembly")]
    public string EntranceAssembly { get; init; } = string.Empty;

    [JsonPropertyName("sharedContracts")]
    public List<MarketSharedContractV2> SharedContracts { get; init; } = [];
}

internal sealed class MarketPluginCompatibilityV2
{
    [JsonPropertyName("minHostVersion")]
    public string MinHostVersion { get; init; } = string.Empty;

    [JsonPropertyName("pluginApiVersion")]
    public string PluginApiVersion { get; init; } = string.Empty;
}

internal sealed class MarketPluginRepositoryV2
{
    [JsonPropertyName("iconUrl")]
    public string IconUrl { get; init; } = string.Empty;

    [JsonPropertyName("projectUrl")]
    public string ProjectUrl { get; init; } = string.Empty;

    [JsonPropertyName("readmeUrl")]
    public string ReadmeUrl { get; init; } = string.Empty;

    [JsonPropertyName("homepageUrl")]
    public string HomepageUrl { get; init; } = string.Empty;

    [JsonPropertyName("repositoryUrl")]
    public string RepositoryUrl { get; init; } = string.Empty;

    [JsonPropertyName("tags")]
    public List<string> Tags { get; init; } = [];

    [JsonPropertyName("releaseNotes")]
    public string ReleaseNotes { get; init; } = string.Empty;
}

internal sealed class MarketPluginPublicationV2
{
    [JsonPropertyName("releaseTag")]
    public string ReleaseTag { get; init; } = string.Empty;

    [JsonPropertyName("releaseAssetName")]
    public string ReleaseAssetName { get; init; } = string.Empty;

    [JsonPropertyName("publishedAt")]
    public DateTimeOffset PublishedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset UpdatedAt { get; init; }

    [JsonPropertyName("packageSizeBytes")]
    public long PackageSizeBytes { get; init; }

    [JsonPropertyName("sha256")]
    public string Sha256 { get; init; } = string.Empty;

    [JsonPropertyName("md5")]
    public string Md5 { get; init; } = string.Empty;

    [JsonPropertyName("packageSources")]
    public List<MarketPluginPackageSourceV2> PackageSources { get; init; } = [];
}

internal sealed class MarketPluginCapabilitiesV2
{
    [JsonPropertyName("sharedContracts")]
    public List<MarketSharedContractV2> SharedContracts { get; init; } = [];

    [JsonPropertyName("desktopComponents")]
    public List<string> DesktopComponents { get; init; } = [];

    [JsonPropertyName("settingsSections")]
    public List<string> SettingsSections { get; init; } = [];

    [JsonPropertyName("exports")]
    public List<string> Exports { get; init; } = [];

    [JsonPropertyName("messageTypes")]
    public List<string> MessageTypes { get; init; } = [];
}

internal sealed class MarketPluginPackageSourceV2
{
    [JsonPropertyName("kind")]
    public string Kind { get; init; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; init; } = string.Empty;
}

internal sealed class MarketSharedContractV2
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; init; } = string.Empty;

    [JsonPropertyName("assemblyName")]
    public string AssemblyName { get; init; } = string.Empty;
}
