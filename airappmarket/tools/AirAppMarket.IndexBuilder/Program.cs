using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
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

        if (options.RunSelfTest)
        {
            MarketIndexBuilder.RunPackageSecuritySelfTests();
            return 0;
        }

        if (!string.IsNullOrWhiteSpace(options.StandaloneMarketManifestPath))
        {
            var manifestPath = Path.GetFullPath(options.StandaloneMarketManifestPath);
            if (!File.Exists(manifestPath))
            {
                throw new FileNotFoundException($"Market manifest '{manifestPath}' was not found.", manifestPath);
            }

            var metadata = MarketManifestMetadata.Parse(File.ReadAllText(manifestPath));
            metadata.ValidateStandalone();
            Console.WriteLine($"Validated market manifest '{manifestPath}'.");
            Console.WriteLine($"PluginId: {metadata.PluginId}");
            Console.WriteLine($"Version: {metadata.Version}");
            Console.WriteLine($"ApiVersion: {metadata.ApiVersion}");
            Console.WriteLine($"PackageSources: {string.Join(", ", metadata.PackageSources.Select(source => source.Kind))}");
            return 0;
        }

        if (!string.IsNullOrWhiteSpace(options.ReleasePackagePath))
        {
            if (string.IsNullOrWhiteSpace(options.MarketManifestPath))
            {
                throw new InvalidOperationException(
                    "--validate-release-package requires --market-manifest <path>.");
            }

            MarketIndexBuilder.ValidateLocalReleasePackage(
                Path.GetFullPath(options.ReleasePackagePath),
                Path.GetFullPath(options.MarketManifestPath),
                options.ExpectedPluginId);
            return 0;
        }

        var registryPath = Path.GetFullPath(options.RegistryPath);
        var outputPath = Path.GetFullPath(options.OutputPath);

        if (!File.Exists(registryPath))
        {
            throw new FileNotFoundException($"Official plugin registry '{registryPath}' was not found.", registryPath);
        }

        var registry = await RegistryDocument.LoadAsync(registryPath, CancellationToken.None);

        if (options.ValidateRegistryOnly)
        {
            Console.WriteLine($"Validated registry '{registryPath}'.");
            Console.WriteLine($"Registered plugins: {registry.Plugins.Count}");
            Console.WriteLine($"Enabled plugins: {registry.Plugins.Count(plugin => plugin.Enabled)}");
            Console.WriteLine($"Contracts: {registry.Contracts.Count}");
            return 0;
        }

        using var httpClient = CreateHttpClient();
        var builder = new MarketIndexBuilder(httpClient);
        var index = await builder.BuildIndexAsync(registry, options.RequireMarketManifest, CancellationToken.None);
        PreserveGeneratedAtWhenContentIsUnchanged(outputPath, index);

        var outputDirectory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        await WriteJsonAsync(outputPath, index, CancellationToken.None);

        Console.WriteLine($"Generated self-contained market index at '{outputPath}'.");
        Console.WriteLine($"Plugins: {index.Plugins.Count}");
        Console.WriteLine($"Contracts: {index.Contracts.Count}");
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
    client.DefaultRequestHeaders.UserAgent.ParseAdd("LanAirApp-IndexBuilder/3.0");
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
    client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");

    var githubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
    if (!string.IsNullOrWhiteSpace(githubToken))
    {
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", githubToken.Trim());
    }

    return client;
}

static async Task WriteJsonAsync<T>(string path, T index, CancellationToken cancellationToken)
{
    var serializerOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
    var json = JsonSerializer.Serialize(index, serializerOptions) + Environment.NewLine;
    var encoding = new UTF8Encoding(false);
    await File.WriteAllTextAsync(path, json, encoding, cancellationToken);
}

static void PreserveGeneratedAtWhenContentIsUnchanged(string outputPath, MarketIndexDocument index)
{
    if (!File.Exists(outputPath))
    {
        return;
    }

    try
    {
        var existing = JsonNode.Parse(File.ReadAllText(outputPath))?.AsObject();
        var candidate = JsonSerializer.SerializeToNode(index)?.AsObject();
        if (existing is null || candidate is null)
        {
            return;
        }

        var existingGeneratedAtText = existing["generatedAt"]?.GetValue<string>();
        existing.Remove("generatedAt");
        candidate.Remove("generatedAt");
        if (JsonNode.DeepEquals(existing, candidate) &&
            DateTimeOffset.TryParse(existingGeneratedAtText, out var existingGeneratedAt))
        {
            index.GeneratedAt = existingGeneratedAt;
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine(
            $"Warning: Could not compare existing market index timestamp at '{outputPath}': {ex.Message}");
    }
}

static void PrintHelp()
{
    Console.WriteLine("AirAppMarket.IndexBuilder");
    Console.WriteLine();
    Console.WriteLine("Builds a self-contained airappmarket/index.json (schemaVersion 3.0.0).");
    Console.WriteLine("Each plugin entry carries all display and acquisition metadata inline.");
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  dotnet run --project airappmarket/tools/AirAppMarket.IndexBuilder -- [options]");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  --registry <path>                 Path to registry JSON.");
    Console.WriteLine("                                    Default: airappmarket/registry/official-plugins.json");
    Console.WriteLine("  --output <path>                   Path to generated market index JSON.");
    Console.WriteLine("                                    Default: airappmarket/index.json");
    Console.WriteLine("  --require-market-manifest         Fail when market-manifest.json release asset is missing.");
    Console.WriteLine("  --validate-registry-only          Validate registry structure without GitHub/network access.");
    Console.WriteLine("  --validate-release-package <path> Validate a local .laapp and market-manifest.json pair.");
    Console.WriteLine("  --validate-market-manifest <path> Validate standalone market-manifest.json fields.");
    Console.WriteLine("  --market-manifest <path>          Release manifest used with --validate-release-package.");
    Console.WriteLine("  --plugin-id <id>                  Optional expected id for local release validation.");
    Console.WriteLine("  --self-test                       Run built-in package security regressions.");
    Console.WriteLine("  --help                            Show help.");
}

internal sealed class CliOptions
{
    public string RegistryPath { get; private set; } = Path.Combine("airappmarket", "registry", "official-plugins.json");
    public string OutputPath { get; private set; } = Path.Combine("airappmarket", "index.json");
    public bool RequireMarketManifest { get; private set; }
    public bool ValidateRegistryOnly { get; private set; }
    public string? ReleasePackagePath { get; private set; }
    public string? StandaloneMarketManifestPath { get; private set; }
    public string? MarketManifestPath { get; private set; }
    public string? ExpectedPluginId { get; private set; }
    public bool RunSelfTest { get; private set; }
    public bool ShowHelp { get; private set; }

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
                case "--require-market-manifest":
                    options.RequireMarketManifest = true;
                    break;
                case "--validate-registry-only":
                    options.ValidateRegistryOnly = true;
                    break;
                case "--validate-release-package":
                    options.ReleasePackagePath = ReadValue(args, ref i, "--validate-release-package");
                    break;
                case "--validate-market-manifest":
                    options.StandaloneMarketManifestPath = ReadValue(args, ref i, "--validate-market-manifest");
                    break;
                case "--market-manifest":
                    options.MarketManifestPath = ReadValue(args, ref i, "--market-manifest");
                    break;
                case "--plugin-id":
                    options.ExpectedPluginId = ReadValue(args, ref i, "--plugin-id");
                    break;
                case "--self-test":
                    options.RunSelfTest = true;
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

    public static void RunPackageSecuritySelfTests()
    {
        var temporaryDirectory = Path.Combine(
            Path.GetTempPath(),
            "LanAirApp",
            "AirAppMarket.IndexBuilder.SelfTest",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporaryDirectory);

        try
        {
            var validPackagePath = Path.Combine(temporaryDirectory, "valid.laapp");
            WriteTestPackage(validPackagePath, CreateTestPluginManifest());
            _ = ReadPackageManifest(validPackagePath);

            ExpectPackageValidationFailure(
                temporaryDirectory,
                "parent-path-traversal",
                CreateTestPluginManifest(),
                "unsafe entry path",
                ("../escape.txt", "escape"));
            ExpectPackageValidationFailure(
                temporaryDirectory,
                "windows-absolute-path",
                CreateTestPluginManifest(),
                "unsafe entry path",
                ("C:/escape.txt", "escape"));
            ExpectPackageValidationFailure(
                temporaryDirectory,
                "duplicate-root-plugin-manifest",
                CreateTestPluginManifest(),
                "duplicate entry",
                ("PLUGIN.JSON", CreateTestPluginManifest()));
            ExpectPackageValidationFailure(
                temporaryDirectory,
                "ambiguous-entry-assembly",
                CreateTestPluginManifest(),
                "ambiguous entry assembly",
                ("nested/Test.Plugin.dll", "nested duplicate"));
            ExpectPackageValidationFailure(
                temporaryDirectory,
                "bundled-plugin-sdk",
                CreateTestPluginManifest(),
                "host-owned assemblies",
                ("lib/LanMountainDesktop.PluginSdk.dll", "host sdk"));
            ExpectPackageValidationFailure(
                temporaryDirectory,
                "bundled-avalonia",
                CreateTestPluginManifest(),
                "host-owned assemblies",
                ("Avalonia.Controls.dll", "host ui framework"));
            ExpectPackageValidationFailure(
                temporaryDirectory,
                "bundled-shared-contract",
                CreateTestPluginManifest("Test.SharedContract.dll"),
                "host-owned assemblies",
                ("contracts/Test.SharedContract.dll", "host shared contract"));

            Console.WriteLine(
                "AirAppMarket.IndexBuilder package security self-tests passed (1 valid + 7 invalid cases).");
        }
        finally
        {
            try
            {
                Directory.Delete(temporaryDirectory, recursive: true);
            }
            catch
            {
                // Ignore temporary self-test cleanup failures.
            }
        }
    }

    private static void ExpectPackageValidationFailure(
        string temporaryDirectory,
        string testName,
        string manifestJson,
        string expectedMessageFragment,
        params (string Path, string Content)[] extraEntries)
    {
        var packagePath = Path.Combine(temporaryDirectory, $"{testName}.laapp");
        WriteTestPackage(packagePath, manifestJson, extraEntries);

        try
        {
            _ = ReadPackageManifest(packagePath);
        }
        catch (InvalidOperationException ex) when (
            ex.Message.Contains(expectedMessageFragment, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        throw new InvalidOperationException(
            $"IndexBuilder package security self-test '{testName}' unexpectedly succeeded or returned the wrong failure.");
    }

    private static void WriteTestPackage(
        string packagePath,
        string manifestJson,
        params (string Path, string Content)[] extraEntries)
    {
        using var archive = ZipFile.Open(packagePath, ZipArchiveMode.Create);
        WriteTestArchiveEntry(archive, "plugin.json", manifestJson);
        WriteTestArchiveEntry(archive, "Test.Plugin.dll", "test plugin assembly");
        foreach (var extraEntry in extraEntries)
        {
            WriteTestArchiveEntry(archive, extraEntry.Path, extraEntry.Content);
        }
    }

    private static void WriteTestArchiveEntry(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(content);
    }

    private static string CreateTestPluginManifest(string? sharedContractAssemblyName = null)
    {
        var sharedContracts = new JsonArray();
        if (!string.IsNullOrWhiteSpace(sharedContractAssemblyName))
        {
            sharedContracts.Add(new JsonObject
            {
                ["id"] = "Test.SharedContract",
                ["version"] = "1.0.0",
                ["assemblyName"] = sharedContractAssemblyName
            });
        }

        return new JsonObject
        {
            ["id"] = "Test.Plugin",
            ["name"] = "Test Plugin",
            ["description"] = "IndexBuilder package security regression fixture.",
            ["author"] = "LanAirApp",
            ["version"] = "1.0.0",
            ["apiVersion"] = "5.0.0",
            ["entranceAssembly"] = "Test.Plugin.dll",
            ["sharedContracts"] = sharedContracts
        }.ToJsonString();
    }

    public static void ValidateLocalReleasePackage(
        string packagePath,
        string marketManifestPath,
        string? expectedPluginId)
    {
        if (!File.Exists(packagePath))
        {
            throw new FileNotFoundException($"Plugin package '{packagePath}' was not found.", packagePath);
        }

        if (!packagePath.EndsWith(".laapp", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Plugin package '{packagePath}' must use the .laapp extension.");
        }

        if (!File.Exists(marketManifestPath))
        {
            throw new FileNotFoundException(
                $"Market manifest '{marketManifestPath}' was not found.",
                marketManifestPath);
        }

        var packageManifest = ReadPackageManifest(packagePath);
        var packageInfo = ComputePackageInfo(packagePath);
        var marketMetadata = MarketManifestMetadata.Parse(File.ReadAllText(marketManifestPath));
        marketMetadata.ValidateStandalone();

        if (!string.IsNullOrWhiteSpace(expectedPluginId) &&
            !string.Equals(expectedPluginId.Trim(), packageManifest.Id, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Expected plugin id '{expectedPluginId}', but package '{packagePath}' declares '{packageManifest.Id}'.");
        }

        var releaseTag = $"v{packageManifest.Version}";
        var releaseAssetName = Path.GetFileName(packagePath);
        marketMetadata.ValidateAgainst(packageManifest, releaseTag, releaseAssetName, packageInfo);

        Console.WriteLine($"Validated local release package '{packagePath}'.");
        Console.WriteLine($"PluginId: {packageManifest.Id}");
        Console.WriteLine($"Version: {packageManifest.Version}");
        Console.WriteLine($"ApiVersion: {packageManifest.ApiVersion}");
        Console.WriteLine($"ReleaseTag: {releaseTag}");
        Console.WriteLine($"ReleaseAssetName: {releaseAssetName}");
        Console.WriteLine($"SHA256: {packageInfo.Sha256}");
        Console.WriteLine($"PackageSizeBytes: {packageInfo.PackageSizeBytes}");
        Console.WriteLine($"PackageSources: {string.Join(", ", marketMetadata.PackageSources.Select(source => source.Kind))}");
    }

    public async Task<MarketIndexDocument> BuildIndexAsync(
        RegistryDocument registry,
        bool requireMarketManifest,
        CancellationToken cancellationToken)
    {
        var plugins = new List<MarketPluginEntry>();
        foreach (var disabledPlugin in registry.Plugins.Where(plugin => !plugin.Enabled))
        {
            Console.Error.WriteLine(
                $"Skipping disabled plugin '{disabledPlugin.Id}': {disabledPlugin.DisabledReason}");
        }

        foreach (var plugin in registry.Plugins.Where(plugin => plugin.Enabled))
        {
            var built = await BuildPluginEntryAsync(registry, plugin, requireMarketManifest, cancellationToken);
            plugins.Add(built);
        }

        var contracts = new List<MarketContract>(registry.Contracts.Count);
        foreach (var contract in registry.Contracts
                     .OrderBy(contract => contract.Id, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(contract => contract.Version, StringComparer.OrdinalIgnoreCase))
        {
            await ValidatePublishedContractAsync(contract, cancellationToken);
            contracts.Add(new MarketContract
            {
                Id = contract.Id.Trim(),
                Version = contract.Version.Trim(),
                AssemblyName = contract.AssemblyName.Trim(),
                DownloadUrl = contract.DownloadUrl.Trim(),
                Sha256 = contract.Sha256.Trim().ToLowerInvariant(),
                PackageSizeBytes = contract.PackageSizeBytes
            });
        }

        return new MarketIndexDocument
        {
            SchemaVersion = "3.0.0",
            SourceId = registry.SourceId.Trim(),
            SourceName = registry.SourceName.Trim(),
            GeneratedAt = DateTimeOffset.UtcNow,
            Contracts = contracts,
            Plugins = plugins
                .OrderBy(plugin => plugin.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(plugin => plugin.PluginId, StringComparer.OrdinalIgnoreCase)
                .ToList()
        };
    }

    private async Task<MarketPluginEntry> BuildPluginEntryAsync(
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
            ValidateReleaseContract(
                plugin,
                registry.Contracts,
                release,
                packageAsset,
                packageManifest,
                packageInfo,
                marketMetadata);

            var minHostVersion = ResolveMinimumHostVersion(
                marketMetadata?.MinHostVersion,
                plugin.DefaultMinHostVersion,
                "0.8.6");
            var tags = MergeTags(plugin.Tags, marketMetadata?.Tags);
            var capabilityHints = MarketCapabilityHints.Merge(plugin.CapabilityHints, marketMetadata?.Capabilities);

            var releaseNotes = FirstNonEmpty(
                marketMetadata?.ReleaseNotes,
                release.Body,
                $"Release {release.TagName}")!;

            var publishedAt = release.PublishedAt ?? release.CreatedAt ?? DateTimeOffset.UtcNow;
            var updatedAt = release.UpdatedAt ?? publishedAt;
            var releaseTag = release.TagName.Trim();
            if (!releaseTag.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            {
                releaseTag = $"v{releaseTag}";
            }

            var packageSources = BuildPackageSources(repo, packageAsset.Name, packageAsset.BrowserDownloadUrl);

            return new MarketPluginEntry
            {
                PluginId = packageManifest.Id,
                Name = FirstNonEmpty(marketMetadata?.Name, packageManifest.Name)!,
                Description = FirstNonEmpty(marketMetadata?.Description, packageManifest.Description)!,
                Author = FirstNonEmpty(marketMetadata?.Author, packageManifest.Author)!,
                Version = packageManifest.Version,
                ApiVersion = packageManifest.ApiVersion,
                MinHostVersion = minHostVersion,
                EntranceAssembly = packageManifest.EntranceAssembly,
                IconUrl = FirstNonEmpty(marketMetadata?.IconUrl, plugin.IconUrl) ?? string.Empty,
                ReadmeUrl = FirstNonEmpty(marketMetadata?.ReadmeUrl, plugin.ReadmeUrl) ?? string.Empty,
                ProjectUrl = FirstNonEmpty(marketMetadata?.ProjectUrl, plugin.ProjectUrl, plugin.RepositoryUrl) ?? string.Empty,
                HomepageUrl = FirstNonEmpty(marketMetadata?.HomepageUrl, plugin.HomepageUrl, plugin.ProjectUrl, plugin.RepositoryUrl) ?? string.Empty,
                RepositoryUrl = FirstNonEmpty(marketMetadata?.RepositoryUrl, plugin.RepositoryUrl)!,
                ReleaseTag = releaseTag,
                ReleaseAssetName = packageAsset.Name,
                PublishedAt = publishedAt,
                UpdatedAt = updatedAt,
                PackageSizeBytes = packageInfo.PackageSizeBytes,
                Sha256 = packageInfo.Sha256,
                Md5 = packageInfo.Md5,
                ReleaseNotes = releaseNotes,
                Tags = tags,
                SharedContracts = packageManifest.SharedContracts
                    .Select(contract => new MarketSharedContract
                    {
                        Id = contract.Id,
                        Version = contract.Version,
                        AssemblyName = contract.AssemblyName
                    })
                    .ToList(),
                DesktopComponents = capabilityHints.DesktopComponents,
                SettingsSections = capabilityHints.SettingsSections,
                Exports = capabilityHints.Exports,
                MessageTypes = capabilityHints.MessageTypes,
                PackageSources = packageSources
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

    private static List<MarketPackageSource> BuildPackageSources(
        GitHubRepositoryIdentity repository,
        string releaseAssetName,
        string releaseDownloadUrl)
    {
        var encodedAssetName = Uri.EscapeDataString(releaseAssetName);
        return
        [
            new MarketPackageSource
            {
                Kind = "releaseAsset",
                Url = releaseDownloadUrl
            },
            new MarketPackageSource
            {
                Kind = "rawFallback",
                Url = $"https://raw.githubusercontent.com/{repository.Owner}/{repository.Name}/main/{encodedAssetName}"
            },
            new MarketPackageSource
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

    private static string ResolveMinimumHostVersion(params string?[] values)
    {
        Version? resolved = null;
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            var normalized = value.Trim();
            var parts = normalized.Split('.');
            if (parts.Length != 3 || !Version.TryParse(normalized, out var parsed))
            {
                throw new InvalidOperationException(
                    $"Minimum host version '{value}' must use major.minor.patch format.");
            }

            if (resolved is null || parsed > resolved)
            {
                resolved = parsed;
            }
        }

        return (resolved ?? new Version(0, 8, 6)).ToString(3);
    }

    private static MarketPluginManifestData ReadPackageManifest(string packagePath)
    {
        using var archive = ZipFile.OpenRead(packagePath);
        var seenEntries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var candidate in archive.Entries)
        {
            var normalizedPath = candidate.FullName.Replace('\\', '/');
            if (IsUnsafeArchivePath(normalizedPath))
            {
                throw new InvalidOperationException(
                    $"Package '{packagePath}' contains unsafe entry path '{candidate.FullName}'.");
            }

            if (!seenEntries.Add(normalizedPath))
            {
                throw new InvalidOperationException(
                    $"Package '{packagePath}' contains duplicate entry '{candidate.FullName}'.");
            }
        }

        var manifestEntries = archive.Entries
            .Where(candidate => string.Equals(candidate.FullName.Replace('\\', '/'), "plugin.json", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (manifestEntries.Length == 0)
        {
            throw new InvalidOperationException($"Package '{packagePath}' does not contain plugin.json.");
        }

        if (manifestEntries.Length > 1)
        {
            throw new InvalidOperationException($"Package '{packagePath}' contains multiple plugin.json files.");
        }

        using var stream = manifestEntries[0].Open();
        using var reader = new StreamReader(stream, Encoding.UTF8, true);
        var text = reader.ReadToEnd();
        var model = JsonSerializer.Deserialize<MarketPluginManifestData>(text, JsonSerializerOptionsProvider.CaseInsensitive)
            ?? throw new InvalidOperationException($"Package '{packagePath}' contains an invalid plugin.json.");
        model.Validate(packagePath);

        var rootEntranceAssemblyEntries = archive.Entries.Count(candidate =>
            string.Equals(
                candidate.FullName.Replace('\\', '/'),
                model.EntranceAssembly,
                StringComparison.OrdinalIgnoreCase));
        if (rootEntranceAssemblyEntries != 1)
        {
            throw new InvalidOperationException(
                $"Package '{packagePath}' must contain exactly one root entry assembly '{model.EntranceAssembly}'.");
        }

        var entranceAssemblyEntries = archive.Entries
            .Where(candidate => !string.IsNullOrWhiteSpace(candidate.Name))
            .Where(candidate => string.Equals(
                GetArchiveLeafName(candidate.FullName),
                model.EntranceAssembly,
                StringComparison.OrdinalIgnoreCase))
            .Select(candidate => candidate.FullName.Replace('\\', '/'))
            .ToArray();
        if (entranceAssemblyEntries.Length != 1)
        {
            throw new InvalidOperationException(
                $"Package '{packagePath}' contains an ambiguous entry assembly '{model.EntranceAssembly}': " +
                string.Join(", ", entranceAssemblyEntries));
        }

        var sharedContractAssemblyNames = model.SharedContracts
            .Select(contract => contract.AssemblyName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var forbiddenHostOwnedEntries = archive.Entries
            .Where(candidate => !string.IsNullOrWhiteSpace(candidate.Name))
            .Select(candidate => new
            {
                Path = candidate.FullName.Replace('\\', '/'),
                LeafName = GetArchiveLeafName(candidate.FullName)
            })
            .Where(candidate =>
                string.Equals(
                    candidate.LeafName,
                    "LanMountainDesktop.PluginSdk.dll",
                    StringComparison.OrdinalIgnoreCase) ||
                (candidate.LeafName.StartsWith("Avalonia", StringComparison.OrdinalIgnoreCase) &&
                 candidate.LeafName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)) ||
                sharedContractAssemblyNames.Contains(candidate.LeafName))
            .Select(candidate => candidate.Path)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (forbiddenHostOwnedEntries.Length > 0)
        {
            throw new InvalidOperationException(
                $"Package '{packagePath}' contains host-owned assemblies: " +
                string.Join(", ", forbiddenHostOwnedEntries));
        }

        return model;
    }

    private static bool IsUnsafeArchivePath(string normalizedPath)
    {
        if (string.IsNullOrWhiteSpace(normalizedPath) ||
            normalizedPath.StartsWith("/", StringComparison.Ordinal) ||
            Path.IsPathRooted(normalizedPath) ||
            (normalizedPath.Length >= 2 &&
             char.IsAsciiLetter(normalizedPath[0]) &&
             normalizedPath[1] == ':'))
        {
            return true;
        }

        var segments = normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Any(segment => segment is "." or "..");
    }

    private static string GetArchiveLeafName(string archivePath)
    {
        var normalizedPath = archivePath.Replace('\\', '/').TrimEnd('/');
        var separatorIndex = normalizedPath.LastIndexOf('/');
        return separatorIndex < 0 ? normalizedPath : normalizedPath[(separatorIndex + 1)..];
    }

    private static void ValidateReleaseContract(
        RegistryPlugin registryPlugin,
        IReadOnlyCollection<RegistryContract> registryContracts,
        GitHubRelease release,
        MarketReleaseAsset packageAsset,
        MarketPluginManifestData packageManifest,
        PackageInfo packageInfo,
        MarketManifestMetadata? marketMetadata)
    {
        if (!string.Equals(registryPlugin.Id.Trim(), packageManifest.Id, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Registry plugin id '{registryPlugin.Id}' does not match package manifest id '{packageManifest.Id}'.");
        }

        foreach (var contract in packageManifest.SharedContracts)
        {
            if (!registryContracts.Any(candidate =>
                    string.Equals(candidate.Id, contract.Id, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(candidate.Version, contract.Version, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(candidate.AssemblyName, contract.AssemblyName, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException(
                    $"Package '{packageManifest.Id}' references shared contract '{contract.Id}@{contract.Version}' that is not published by the market registry.");
            }
        }

        var expectedReleaseTag = $"v{packageManifest.Version}";
        var normalizedReleaseTag = release.TagName.Trim();
        if (!string.Equals(normalizedReleaseTag, expectedReleaseTag, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Release tag '{release.TagName}' for '{packageManifest.Id}' must match package version '{packageManifest.Version}' as '{expectedReleaseTag}'.");
        }

        var expectedAssetName = $"{packageManifest.Id}.{packageManifest.Version}.laapp";
        if (!string.Equals(packageAsset.Name, expectedAssetName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Release asset for '{packageManifest.Id}' must be named '{expectedAssetName}', but found '{packageAsset.Name}'.");
        }

        if (packageAsset.Size > 0 && packageAsset.Size != packageInfo.PackageSizeBytes)
        {
            throw new InvalidOperationException(
                $"GitHub reports {packageAsset.Size} bytes for '{packageAsset.Name}', but the downloaded package contains {packageInfo.PackageSizeBytes} bytes.");
        }

        marketMetadata?.ValidateAgainst(
            packageManifest,
            normalizedReleaseTag,
            packageAsset.Name,
            packageInfo);
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

    private async Task ValidatePublishedContractAsync(
        RegistryContract contract,
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(
            contract.DownloadUrl,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();
        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var actualSha256 = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        if (bytes.LongLength != contract.PackageSizeBytes ||
            !string.Equals(actualSha256, contract.Sha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Published shared contract '{contract.Id}@{contract.Version}' does not match registry integrity metadata. " +
                $"Expected size/hash '{contract.PackageSizeBytes}/{contract.Sha256}', actual '{bytes.LongLength}/{actualSha256}'.");
        }

        Console.WriteLine(
            $"Verified shared contract '{contract.Id}@{contract.Version}' ({bytes.LongLength} bytes, SHA-256 {actualSha256}).");
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

    public static readonly JsonSerializerOptions StrictRegistry = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
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
        var model = JsonSerializer.Deserialize<RegistryDocument>(json, JsonSerializerOptionsProvider.StrictRegistry)
            ?? throw new InvalidOperationException($"Failed to parse registry '{path}'.");
        model.Validate(path);
        return model;
    }

    private void Validate(string sourceName)
    {
        if (!string.Equals(SchemaVersion?.Trim(), "1.0.0", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Registry '{sourceName}' must use schemaVersion '1.0.0'.");
        }

        if (string.IsNullOrWhiteSpace(SourceId) || string.IsNullOrWhiteSpace(SourceName))
        {
            throw new InvalidOperationException($"Registry '{sourceName}' is missing source metadata.");
        }

        if (Plugins.Count == 0)
        {
            throw new InvalidOperationException($"Registry '{sourceName}' does not declare plugins.");
        }

        if (!Plugins.Any(plugin => plugin.Enabled))
        {
            throw new InvalidOperationException($"Registry '{sourceName}' does not enable any plugins.");
        }

        if (string.IsNullOrWhiteSpace(DefaultMarketManifestAssetName) ||
            !string.Equals(Path.GetFileName(DefaultMarketManifestAssetName), DefaultMarketManifestAssetName, StringComparison.Ordinal) ||
            !DefaultMarketManifestAssetName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Registry '{sourceName}' declares invalid defaultMarketManifestAssetName '{DefaultMarketManifestAssetName}'.");
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
    public bool Enabled { get; init; } = true;
    public string? DisabledReason { get; init; }
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

        if (!Enabled && string.IsNullOrWhiteSpace(DisabledReason))
        {
            throw new InvalidOperationException(
                $"Registry '{sourceName}' disabled plugin '{Id}' must declare disabledReason.");
        }

        if (Id.Any(character => !(char.IsLetterOrDigit(character) || character is '.' or '_' or '-')) ||
            Id.Contains("..", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Registry '{sourceName}' has invalid plugin id '{Id}'.");
        }

        ValidateOptionalHttpsUrl(ProjectUrl, nameof(ProjectUrl), sourceName);
        ValidateOptionalHttpsUrl(ReadmeUrl, nameof(ReadmeUrl), sourceName);
        ValidateOptionalHttpsUrl(HomepageUrl, nameof(HomepageUrl), sourceName);
        ValidateOptionalHttpsUrl(IconUrl, nameof(IconUrl), sourceName);

        if (!string.IsNullOrWhiteSpace(DefaultMinHostVersion) && !IsThreePartVersion(DefaultMinHostVersion))
        {
            throw new InvalidOperationException(
                $"Registry '{sourceName}' plugin '{Id}' declares invalid defaultMinHostVersion '{DefaultMinHostVersion}'.");
        }

        if (!string.IsNullOrWhiteSpace(MarketManifestAssetName) &&
            (!string.Equals(Path.GetFileName(MarketManifestAssetName), MarketManifestAssetName, StringComparison.Ordinal) ||
             !MarketManifestAssetName.EndsWith(".json", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                $"Registry '{sourceName}' plugin '{Id}' declares invalid marketManifestAssetName '{MarketManifestAssetName}'.");
        }

        if (!string.IsNullOrWhiteSpace(ReleaseAssetName) &&
            (!string.Equals(Path.GetFileName(ReleaseAssetName), ReleaseAssetName, StringComparison.Ordinal) ||
             !ReleaseAssetName.EndsWith(".laapp", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                $"Registry '{sourceName}' plugin '{Id}' declares invalid releaseAssetName '{ReleaseAssetName}'.");
        }

        ValidateDistinctValues(Tags, nameof(Tags), sourceName);
        ValidateDistinctValues(CapabilityHints.DesktopComponents, "capabilityHints.desktopComponents", sourceName);
        ValidateDistinctValues(CapabilityHints.SettingsSections, "capabilityHints.settingsSections", sourceName);
        ValidateDistinctValues(CapabilityHints.Exports, "capabilityHints.exports", sourceName);
        ValidateDistinctValues(CapabilityHints.MessageTypes, "capabilityHints.messageTypes", sourceName);
    }

    private void ValidateOptionalHttpsUrl(string? value, string propertyName, string sourceName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException(
                $"Registry '{sourceName}' plugin '{Id}' declares invalid HTTPS URL '{value}' for '{propertyName}'.");
        }
    }

    private void ValidateDistinctValues(IReadOnlyCollection<string> values, string propertyName, string sourceName)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value) || !seen.Add(value.Trim()))
            {
                throw new InvalidOperationException(
                    $"Registry '{sourceName}' plugin '{Id}' contains a blank or duplicate value in '{propertyName}'.");
            }
        }
    }

    private static bool IsThreePartVersion(string value)
    {
        var parts = value.Trim().Split('.');
        return parts.Length == 3 && parts.All(part => int.TryParse(part, out var number) && number >= 0);
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

        var versionParts = Version.Split('.');
        if (versionParts.Length != 3 || versionParts.Any(part => !int.TryParse(part, out var number) || number < 0))
        {
            throw new InvalidOperationException(
                $"Registry '{sourceName}' declares invalid version '{Version}' for contract '{Id}'.");
        }

        if (!string.Equals(Path.GetFileName(AssemblyName), AssemblyName, StringComparison.Ordinal) ||
            !AssemblyName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Registry '{sourceName}' declares invalid assemblyName '{AssemblyName}' for contract '{Id}'.");
        }

        if (!Uri.TryCreate(DownloadUrl, UriKind.Absolute, out var downloadUri) ||
            downloadUri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException(
                $"Registry '{sourceName}' declares invalid HTTPS downloadUrl for contract '{Id}@{Version}'.");
        }

        if (Sha256.Length != 64 || Sha256.Any(character => !Uri.IsHexDigit(character)))
        {
            throw new InvalidOperationException(
                $"Registry '{sourceName}' declares invalid SHA-256 for contract '{Id}@{Version}'.");
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
    public string? SchemaVersion { get; init; }
    public string? PluginId { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? Author { get; init; }
    public string? Version { get; init; }
    public string? ApiVersion { get; init; }
    public string? EntranceAssembly { get; init; }
    public string? MinHostVersion { get; init; }
    public string? IconUrl { get; init; }
    public string? ProjectUrl { get; init; }
    public string? ReadmeUrl { get; init; }
    public string? HomepageUrl { get; init; }
    public string? RepositoryUrl { get; init; }
    public string? ReleaseNotes { get; init; }
    public string? ReleaseTag { get; init; }
    public string? ReleaseAssetName { get; init; }
    public string? Sha256 { get; init; }
    public long? PackageSizeBytes { get; init; }
    public List<string> Tags { get; init; } = [];
    public MarketCapabilityHints Capabilities { get; init; } = new();
    public List<MarketManifestPackageSource> PackageSources { get; init; } = [];

    public static MarketManifestMetadata Parse(string json)
    {
        using var doc = JsonDocument.Parse(json, new JsonDocumentOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip
        });
        var root = doc.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException("market-manifest.json must contain a JSON object.");
        }

        var manifestElement = TryGetObject(root, "manifest");
        var compatibilityElement = TryGetObject(root, "compatibility");
        var repositoryElement = TryGetObject(root, "repository");
        var publicationElement = TryGetObject(root, "publication");
        var capabilitiesElement = TryGetObject(root, "capabilities");

        return new MarketManifestMetadata
        {
            SchemaVersion = FirstString(root, "schemaVersion"),
            PluginId = FirstString(manifestElement, "id") ?? FirstString(root, "pluginId"),
            Name = FirstString(manifestElement, "name")
                ?? FirstString(root, "displayName")
                ?? FirstString(root, "name"),
            Description = FirstString(manifestElement, "description") ?? FirstString(root, "description"),
            Author = FirstString(manifestElement, "author") ?? FirstString(root, "author"),
            Version = FirstString(manifestElement, "version") ?? FirstString(root, "version"),
            ApiVersion = FirstString(manifestElement, "apiVersion")
                ?? FirstString(compatibilityElement, "apiVersion")
                ?? FirstString(compatibilityElement, "pluginApiVersion")
                ?? FirstString(root, "apiVersion"),
            EntranceAssembly = FirstString(manifestElement, "entranceAssembly")
                ?? FirstString(root, "entranceAssembly"),
            MinHostVersion = FirstString(compatibilityElement, "minHostVersion") ?? FirstString(root, "minHostVersion"),
            IconUrl = FirstString(repositoryElement, "iconUrl") ?? FirstString(root, "iconUrl"),
            ProjectUrl = FirstString(repositoryElement, "projectUrl") ?? FirstString(root, "projectUrl"),
            ReadmeUrl = FirstString(repositoryElement, "readmeUrl") ?? FirstString(root, "readmeUrl"),
            HomepageUrl = FirstString(repositoryElement, "homepageUrl") ?? FirstString(root, "homepageUrl"),
            RepositoryUrl = FirstString(repositoryElement, "repositoryUrl") ?? FirstString(root, "repositoryUrl"),
            ReleaseNotes = FirstString(repositoryElement, "releaseNotes")
                ?? FirstString(publicationElement, "releaseNotes")
                ?? FirstString(root, "releaseNotes"),
            ReleaseTag = FirstString(publicationElement, "releaseTag")
                ?? FirstString(manifestElement, "releaseTag")
                ?? FirstString(root, "releaseTag"),
            ReleaseAssetName = FirstString(publicationElement, "releaseAssetName")
                ?? FirstString(manifestElement, "releaseAssetName")
                ?? FirstString(root, "releaseAssetName"),
            Sha256 = FirstString(publicationElement, "sha256") ?? FirstString(root, "sha256"),
            PackageSizeBytes = FirstInt64(publicationElement, "packageSizeBytes")
                ?? FirstInt64(root, "packageSizeBytes"),
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
            },
            PackageSources = ReadPackageSources(publicationElement)
        };
    }

    public void ValidateAgainst(
        MarketPluginManifestData packageManifest,
        string releaseTag,
        string releaseAssetName,
        PackageInfo packageInfo)
    {
        ValidateStandalone();
        if (!string.Equals(SchemaVersion, "2.0.0", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"market-manifest.json for '{packageManifest.Id}' must use schemaVersion '2.0.0'.");
        }

        EnsureOptionalMatch(PluginId, packageManifest.Id, "plugin id", packageManifest.Id);
        EnsureOptionalMatch(Version, packageManifest.Version, "version", packageManifest.Id);
        EnsureOptionalMatch(ApiVersion, packageManifest.ApiVersion, "apiVersion", packageManifest.Id);
        EnsureOptionalMatch(EntranceAssembly, packageManifest.EntranceAssembly, "entranceAssembly", packageManifest.Id);
        EnsureOptionalMatch(ReleaseTag, releaseTag, "releaseTag", packageManifest.Id);
        EnsureOptionalMatch(ReleaseAssetName, releaseAssetName, "releaseAssetName", packageManifest.Id);

        if (!string.IsNullOrWhiteSpace(Sha256) &&
            !string.Equals(Sha256.Trim(), packageInfo.Sha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"market-manifest.json for '{packageManifest.Id}' declares SHA-256 '{Sha256}', but the package SHA-256 is '{packageInfo.Sha256}'.");
        }

        if (PackageSizeBytes is { } packageSize && packageSize != packageInfo.PackageSizeBytes)
        {
            throw new InvalidOperationException(
                $"market-manifest.json for '{packageManifest.Id}' declares packageSizeBytes '{packageSize}', but the package contains '{packageInfo.PackageSizeBytes}' bytes.");
        }

        if (PackageSources.Count > 0)
        {
            var expectedOrder = new[] { "releaseAsset", "rawFallback", "workspaceLocal" };
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var previousOrder = -1;
            foreach (var source in PackageSources)
            {
                var order = Array.IndexOf(expectedOrder, source.Kind);
                if (order < 0 || order < previousOrder || !seen.Add(source.Kind))
                {
                    throw new InvalidOperationException(
                        $"market-manifest.json for '{packageManifest.Id}' must declare unique packageSources in releaseAsset -> rawFallback -> workspaceLocal order.");
                }

                if (string.IsNullOrWhiteSpace(source.Url))
                {
                    throw new InvalidOperationException(
                        $"market-manifest.json for '{packageManifest.Id}' contains an empty package source URL.");
                }

                previousOrder = order;
            }
        }
    }

    public void ValidateStandalone()
    {
        var pluginId = RequireMetadata(PluginId, "manifest.id/pluginId");
        _ = RequireMetadata(Name, "manifest.name/displayName");
        _ = RequireMetadata(Description, "manifest.description");
        _ = RequireMetadata(Author, "manifest.author");
        var version = RequireThreePartVersion(Version, "manifest.version", pluginId);
        var apiVersion = RequireThreePartVersion(ApiVersion, "manifest.apiVersion/compatibility.apiVersion", pluginId);
        if (System.Version.Parse(apiVersion).Major != 5)
        {
            throw new InvalidOperationException(
                $"market-manifest.json for '{pluginId}' targets PluginSdk API '{apiVersion}', but production requires API major 5.");
        }

        _ = RequireThreePartVersion(MinHostVersion, "compatibility.minHostVersion", pluginId);
        var entranceAssembly = EntranceAssembly?.Trim();
        if (!string.IsNullOrWhiteSpace(entranceAssembly) &&
            (!string.Equals(Path.GetFileName(entranceAssembly), entranceAssembly, StringComparison.Ordinal) ||
             !entranceAssembly.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                $"market-manifest.json for '{pluginId}' declares invalid entranceAssembly '{entranceAssembly}'.");
        }

        if (!string.Equals(SchemaVersion, "2.0.0", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"market-manifest.json for '{pluginId}' must use schemaVersion '2.0.0'.");
        }

        var releaseTag = RequireMetadata(ReleaseTag, "publication.releaseTag");
        var expectedReleaseTag = $"v{version}";
        if (!string.Equals(releaseTag, expectedReleaseTag, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"market-manifest.json for '{pluginId}' releaseTag must be '{expectedReleaseTag}', but found '{releaseTag}'.");
        }

        var releaseAssetName = RequireMetadata(ReleaseAssetName, "publication.releaseAssetName");
        var expectedAssetName = $"{pluginId}.{version}.laapp";
        if (!string.Equals(releaseAssetName, expectedAssetName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"market-manifest.json for '{pluginId}' releaseAssetName must be '{expectedAssetName}', but found '{releaseAssetName}'.");
        }

        var sha256 = RequireMetadata(Sha256, "publication.sha256");
        if (sha256.Length != 64 || sha256.Any(character => !Uri.IsHexDigit(character)))
        {
            throw new InvalidOperationException(
                $"market-manifest.json for '{pluginId}' declares invalid publication.sha256 '{sha256}'.");
        }

        if (PackageSizeBytes is not { } packageSize || packageSize <= 0)
        {
            throw new InvalidOperationException(
                $"market-manifest.json for '{pluginId}' must declare positive publication.packageSizeBytes.");
        }

        var repositoryUrl = RequireMetadata(RepositoryUrl, "repository.repositoryUrl");
        _ = GitHubRepositoryIdentity.Parse(repositoryUrl);

        var expectedOrder = new[] { "releaseAsset", "rawFallback", "workspaceLocal" };
        if (PackageSources.Count != expectedOrder.Length)
        {
            throw new InvalidOperationException(
                $"market-manifest.json for '{pluginId}' must declare exactly three canonical package sources.");
        }

        for (var index = 0; index < expectedOrder.Length; index++)
        {
            if (!string.Equals(PackageSources[index].Kind, expectedOrder[index], StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(PackageSources[index].Url))
            {
                throw new InvalidOperationException(
                    $"market-manifest.json for '{pluginId}' packageSources[{index}] must be '{expectedOrder[index]}' with a non-empty URL.");
            }
        }
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

    private static long? FirstInt64(JsonElement? source, string propertyName)
    {
        if (!source.HasValue ||
            !source.Value.TryGetProperty(propertyName, out var property) ||
            property.ValueKind != JsonValueKind.Number ||
            !property.TryGetInt64(out var value))
        {
            return null;
        }

        return value;
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

    private static List<MarketManifestPackageSource> ReadPackageSources(JsonElement? publication)
    {
        if (!publication.HasValue ||
            !publication.Value.TryGetProperty("packageSources", out var sources) ||
            sources.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var result = new List<MarketManifestPackageSource>();
        foreach (var source in sources.EnumerateArray())
        {
            if (source.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException("market-manifest.json publication.packageSources must contain objects.");
            }

            result.Add(new MarketManifestPackageSource(
                FirstString(source, "kind") ?? string.Empty,
                FirstString(source, "url") ?? FirstString(source, "path") ?? string.Empty));
        }

        return result;
    }

    private static void EnsureOptionalMatch(
        string? declaredValue,
        string actualValue,
        string fieldName,
        string pluginId)
    {
        if (!string.IsNullOrWhiteSpace(declaredValue) &&
            !string.Equals(declaredValue.Trim(), actualValue, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"market-manifest.json for '{pluginId}' declares {fieldName} '{declaredValue}', but the package/release declares '{actualValue}'.");
        }
    }

    private static string RequireMetadata(string? value, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"market-manifest.json is missing required property '{propertyName}'.");
        }

        return value.Trim();
    }

    private static string RequireThreePartVersion(string? value, string propertyName, string pluginId)
    {
        var normalized = RequireMetadata(value, propertyName);
        var parts = normalized.Split('.');
        if (parts.Length != 3 ||
            parts.Any(part => !int.TryParse(part, out var number) || number < 0) ||
            !System.Version.TryParse(normalized, out _))
        {
            throw new InvalidOperationException(
                $"market-manifest.json for '{pluginId}' declares invalid version '{normalized}' for '{propertyName}'.");
        }

        return normalized;
    }
}

internal sealed record MarketManifestPackageSource(string Kind, string Url);

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

    [JsonPropertyName("updated_at")]
    public DateTimeOffset? UpdatedAt { get; init; }

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

        foreach (var asset in Assets)
        {
            if (string.IsNullOrWhiteSpace(asset.Name) ||
                asset.Size <= 0 ||
                !Uri.TryCreate(asset.BrowserDownloadUrl, UriKind.Absolute, out var downloadUri) ||
                downloadUri.Scheme != Uri.UriSchemeHttps)
            {
                throw new InvalidOperationException(
                    $"Latest release '{TagName}' for '{repository.Owner}/{repository.Name}' contains invalid asset metadata.");
            }
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
            uri.Scheme != Uri.UriSchemeHttps ||
            !string.Equals(uri.Host, "github.com", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Unsupported repository url '{repositoryUrl}'.");
        }

        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length != 2 ||
            string.IsNullOrWhiteSpace(segments[0]) ||
            string.IsNullOrWhiteSpace(segments[1]) ||
            uri.Query.Length > 0 ||
            uri.Fragment.Length > 0)
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
    private static readonly System.Text.RegularExpressions.Regex VersionPattern = new(
        "^[0-9]+\\.[0-9]+\\.[0-9]+$",
        System.Text.RegularExpressions.RegexOptions.CultureInvariant |
        System.Text.RegularExpressions.RegexOptions.Compiled);

    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string ApiVersion { get; init; } = string.Empty;
    public string EntranceAssembly { get; init; } = string.Empty;
    public List<MarketSharedContract> SharedContracts { get; init; } = [];

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

        if (!VersionPattern.IsMatch(Version) || !System.Version.TryParse(Version, out _))
        {
            throw new InvalidOperationException($"Package '{packagePath}' declares invalid version '{Version}'. Expected major.minor.patch.");
        }

        if (!VersionPattern.IsMatch(ApiVersion) || !System.Version.TryParse(ApiVersion, out var pluginApiVersion))
        {
            throw new InvalidOperationException($"Package '{packagePath}' declares invalid apiVersion '{ApiVersion}'.");
        }

        var productionApiVersion = System.Version.Parse("5.0.0");
        if (pluginApiVersion.Major != productionApiVersion.Major)
        {
            throw new InvalidOperationException(
                $"Package '{packagePath}' targets PluginSdk API '{ApiVersion}', but the production market requires API major '{productionApiVersion.Major}' ({productionApiVersion}).");
        }

        if (!string.Equals(Path.GetFileName(EntranceAssembly), EntranceAssembly, StringComparison.Ordinal) ||
            !EntranceAssembly.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Package '{packagePath}' declares invalid root entranceAssembly '{EntranceAssembly}'.");
        }

        var contractKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var contract in SharedContracts)
        {
            if (string.IsNullOrWhiteSpace(contract.Id) ||
                !VersionPattern.IsMatch(contract.Version) ||
                string.IsNullOrWhiteSpace(contract.AssemblyName) ||
                contract.AssemblyName.Contains('/') ||
                contract.AssemblyName.Contains('\\') ||
                !contract.AssemblyName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Package '{packagePath}' contains an invalid shared contract declaration.");
            }

            var key = $"{contract.Id}@{contract.Version}";
            if (!contractKeys.Add(key))
            {
                throw new InvalidOperationException(
                    $"Package '{packagePath}' contains duplicate shared contract '{key}'.");
            }
        }
    }
}

// ---- Self-contained v3 index output models ----

internal sealed class MarketIndexDocument
{
    [JsonPropertyName("schemaVersion")]
    public string SchemaVersion { get; init; } = string.Empty;

    [JsonPropertyName("sourceId")]
    public string SourceId { get; init; } = string.Empty;

    [JsonPropertyName("sourceName")]
    public string SourceName { get; init; } = string.Empty;

    [JsonPropertyName("generatedAt")]
    public DateTimeOffset GeneratedAt { get; set; }

    [JsonPropertyName("contracts")]
    public List<MarketContract> Contracts { get; init; } = [];

    [JsonPropertyName("plugins")]
    public List<MarketPluginEntry> Plugins { get; init; } = [];
}

internal sealed class MarketContract
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

internal sealed class MarketPluginEntry
{
    [JsonPropertyName("pluginId")]
    public string PluginId { get; init; } = string.Empty;

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

    [JsonPropertyName("minHostVersion")]
    public string MinHostVersion { get; init; } = string.Empty;

    [JsonPropertyName("entranceAssembly")]
    public string EntranceAssembly { get; init; } = string.Empty;

    [JsonPropertyName("iconUrl")]
    public string IconUrl { get; init; } = string.Empty;

    [JsonPropertyName("readmeUrl")]
    public string ReadmeUrl { get; init; } = string.Empty;

    [JsonPropertyName("projectUrl")]
    public string ProjectUrl { get; init; } = string.Empty;

    [JsonPropertyName("homepageUrl")]
    public string HomepageUrl { get; init; } = string.Empty;

    [JsonPropertyName("repositoryUrl")]
    public string RepositoryUrl { get; init; } = string.Empty;

    [JsonPropertyName("releaseTag")]
    public string ReleaseTag { get; init; } = string.Empty;

    [JsonPropertyName("releaseAssetName")]
    public string ReleaseAssetName { get; init; } = string.Empty;

    [JsonPropertyName("sha256")]
    public string Sha256 { get; init; } = string.Empty;

    [JsonPropertyName("md5")]
    public string Md5 { get; init; } = string.Empty;

    [JsonPropertyName("packageSizeBytes")]
    public long PackageSizeBytes { get; init; }

    [JsonPropertyName("publishedAt")]
    public DateTimeOffset PublishedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset UpdatedAt { get; init; }

    [JsonPropertyName("releaseNotes")]
    public string ReleaseNotes { get; init; } = string.Empty;

    [JsonPropertyName("tags")]
    public List<string> Tags { get; init; } = [];

    [JsonPropertyName("sharedContracts")]
    public List<MarketSharedContract> SharedContracts { get; init; } = [];

    [JsonPropertyName("desktopComponents")]
    public List<string> DesktopComponents { get; init; } = [];

    [JsonPropertyName("settingsSections")]
    public List<string> SettingsSections { get; init; } = [];

    [JsonPropertyName("exports")]
    public List<string> Exports { get; init; } = [];

    [JsonPropertyName("messageTypes")]
    public List<string> MessageTypes { get; init; } = [];

    [JsonPropertyName("packageSources")]
    public List<MarketPackageSource> PackageSources { get; init; } = [];
}

internal sealed class MarketSharedContract
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; init; } = string.Empty;

    [JsonPropertyName("assemblyName")]
    public string AssemblyName { get; init; } = string.Empty;
}

internal sealed class MarketPackageSource
{
    [JsonPropertyName("kind")]
    public string Kind { get; init; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; init; } = string.Empty;
}
