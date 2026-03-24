using System.Text.Json;
using System.Text.Json.Serialization;

return await RunAsync(args);

static Task<int> RunAsync(string[] args)
{
    try
    {
        var indexPath = args.Length > 0
            ? Path.GetFullPath(args[0])
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "index.json"));
        var schemaPath = args.Length > 1
            ? Path.GetFullPath(args[1])
            : Path.GetFullPath(Path.Combine(Path.GetDirectoryName(indexPath)!, "schema", "airappmarket-index.schema.json"));

        if (!File.Exists(indexPath))
        {
            throw new FileNotFoundException($"Market index '{indexPath}' was not found.", indexPath);
        }

        if (!File.Exists(schemaPath))
        {
            throw new FileNotFoundException($"Market schema '{schemaPath}' was not found.", schemaPath);
        }

        JsonDocument.Parse(File.ReadAllText(schemaPath));
        var document = MarketIndexV2.Load(File.ReadAllText(indexPath), indexPath);

        Console.WriteLine($"Validated '{indexPath}'.");
        Console.WriteLine($"SchemaVersion: {document.SchemaVersion}");
        Console.WriteLine($"Source: {document.SourceName} ({document.SourceId})");
        Console.WriteLine($"Contracts: {document.Contracts.Count}");
        Console.WriteLine($"Plugins: {document.Plugins.Count}");
        return Task.FromResult(0);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine(ex.Message);
        return Task.FromResult(1);
    }
}

internal static class MarketValidation
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static string Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public static string Require(string? value, string propertyName, string sourceName)
    {
        var normalized = Normalize(value);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException($"Market index '{sourceName}' is missing required property '{propertyName}'.");
        }

        return normalized;
    }

    public static string NormalizeUrl(string? value, string propertyName, string sourceName)
    {
        var normalized = Require(value, propertyName, sourceName);
        if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException(
                $"Market index '{sourceName}' declares invalid URL '{normalized}' for '{propertyName}'.");
        }

        return normalized;
    }

    public static string NormalizeVersion(string? value, string propertyName, string sourceName)
    {
        var normalized = Require(value, propertyName, sourceName);
        if (!TryParseVersion(normalized, out _))
        {
            throw new InvalidOperationException(
                $"Market index '{sourceName}' declares invalid version '{normalized}' for '{propertyName}'.");
        }

        return normalized;
    }

    public static string NormalizeSha256(string? value, string propertyName, string sourceName)
    {
        var normalized = Require(value, propertyName, sourceName).ToLowerInvariant();
        if (normalized.Length != 64 || normalized.Any(ch => !Uri.IsHexDigit(ch)))
        {
            throw new InvalidOperationException(
                $"Market index '{sourceName}' declares invalid sha256 '{normalized}' for '{propertyName}'.");
        }

        return normalized;
    }

    public static string NormalizeMd5(string? value, string propertyName, string sourceName)
    {
        var normalized = Require(value, propertyName, sourceName).ToLowerInvariant();
        if (normalized.Length != 32 || normalized.Any(ch => !Uri.IsHexDigit(ch)))
        {
            throw new InvalidOperationException(
                $"Market index '{sourceName}' declares invalid md5 '{normalized}' for '{propertyName}'.");
        }

        return normalized;
    }

    public static string NormalizeReleaseTag(string? value, string propertyName, string sourceName)
    {
        var normalized = Require(value, propertyName, sourceName);
        if (!normalized.StartsWith("v", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Market index '{sourceName}' declares invalid release tag '{normalized}' for '{propertyName}'. Expected format 'v1.2.3'.");
        }

        if (!TryParseVersion(normalized[1..], out _))
        {
            throw new InvalidOperationException(
                $"Market index '{sourceName}' declares invalid release tag '{normalized}' for '{propertyName}'.");
        }

        return normalized;
    }

    public static bool TryParseVersion(string? value, out Version? version)
    {
        version = null;
        var normalized = Normalize(value);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        var separatorIndex = normalized.IndexOfAny(['-', '+', ' ']);
        if (separatorIndex > 0)
        {
            normalized = normalized[..separatorIndex];
        }

        if (!Version.TryParse(normalized, out var parsed))
        {
            return false;
        }

        version = new Version(
            Math.Max(0, parsed.Major),
            Math.Max(0, parsed.Minor),
            Math.Max(0, parsed.Build));
        return true;
    }

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

    public static MarketIndexV2 Load(string json, string sourceName)
    {
        var document = JsonSerializer.Deserialize<MarketIndexV2>(
            json.TrimStart('\uFEFF'),
            MarketValidation.JsonOptions) ?? throw new InvalidOperationException($"Failed to parse market index '{sourceName}'.");

        return document.ValidateAndNormalize(sourceName);
    }

    private MarketIndexV2 ValidateAndNormalize(string sourceName)
    {
        if (!string.Equals(MarketValidation.Require(SchemaVersion, nameof(SchemaVersion), sourceName), "2.0.0", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Market index '{sourceName}' must use schemaVersion '2.0.0'.");
        }

        var normalizedContracts = ValidateContracts(sourceName);
        var normalizedPlugins = ValidatePlugins(sourceName);

        return new MarketIndexV2
        {
            SchemaVersion = "2.0.0",
            SourceId = MarketValidation.Require(SourceId, nameof(SourceId), sourceName),
            SourceName = MarketValidation.Require(SourceName, nameof(SourceName), sourceName),
            GeneratedAt = GeneratedAt == default
                ? throw new InvalidOperationException($"Market index '{sourceName}' is missing a valid generatedAt timestamp.")
                : GeneratedAt,
            Contracts = normalizedContracts,
            Plugins = normalizedPlugins
        };
    }

    private List<MarketContractV2> ValidateContracts(string sourceName)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var normalized = new List<MarketContractV2>(Contracts.Count);

        foreach (var contract in Contracts)
        {
            var normalizedContract = contract.ValidateAndNormalize(sourceName);
            var key = $"{normalizedContract.Id}@{normalizedContract.Version}";
            if (!seen.Add(key))
            {
                throw new InvalidOperationException($"Market index '{sourceName}' contains duplicate contract '{key}'.");
            }
            normalized.Add(normalizedContract);
        }

        return normalized;
    }

    private List<MarketPluginV2> ValidatePlugins(string sourceName)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var normalized = new List<MarketPluginV2>(Plugins.Count);

        foreach (var plugin in Plugins)
        {
            var normalizedPlugin = plugin.ValidateAndNormalize(sourceName);
            if (!seen.Add(normalizedPlugin.PluginId))
            {
                throw new InvalidOperationException(
                    $"Market index '{sourceName}' contains duplicate plugin id '{normalizedPlugin.PluginId}'.");
            }
            normalized.Add(normalizedPlugin);
        }

        return normalized;
    }
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

    public MarketContractV2 ValidateAndNormalize(string sourceName)
    {
        if (PackageSizeBytes <= 0)
        {
            throw new InvalidOperationException(
                $"Market index '{sourceName}' declares invalid packageSizeBytes '{PackageSizeBytes}' for contract '{Id}'.");
        }

        return new MarketContractV2
        {
            Id = MarketValidation.Require(Id, nameof(Id), sourceName),
            Version = MarketValidation.NormalizeVersion(Version, nameof(Version), sourceName),
            AssemblyName = MarketValidation.Require(AssemblyName, nameof(AssemblyName), sourceName),
            DownloadUrl = MarketValidation.NormalizeUrl(DownloadUrl, nameof(DownloadUrl), sourceName),
            Sha256 = MarketValidation.NormalizeSha256(Sha256, nameof(Sha256), sourceName),
            PackageSizeBytes = PackageSizeBytes
        };
    }
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

    public MarketPluginV2 ValidateAndNormalize(string sourceName)
    {
        var pluginId = MarketValidation.Require(PluginId, nameof(PluginId), sourceName);
        var manifest = Manifest.ValidateAndNormalize(sourceName);
        var compatibility = Compatibility.ValidateAndNormalize(sourceName);
        var repository = Repository.ValidateAndNormalize(sourceName);
        var publication = Publication.ValidateAndNormalize(sourceName, pluginId);
        var capabilities = Capabilities.ValidateAndNormalize(sourceName, pluginId);

        if (!string.Equals(pluginId, manifest.Id, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Market index '{sourceName}' plugin '{pluginId}' has manifest id '{manifest.Id}' mismatch.");
        }

        if (!string.Equals(manifest.ApiVersion, compatibility.PluginApiVersion, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Market index '{sourceName}' plugin '{pluginId}' declares api mismatch between manifest ('{manifest.ApiVersion}') and compatibility ('{compatibility.PluginApiVersion}').");
        }

        var manifestContractSet = manifest.SharedContracts
            .Select(contract => $"{contract.Id}@{contract.Version}@{contract.AssemblyName}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var capabilitiesContractSet = capabilities.SharedContracts
            .Select(contract => $"{contract.Id}@{contract.Version}@{contract.AssemblyName}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!manifestContractSet.SetEquals(capabilitiesContractSet))
        {
            throw new InvalidOperationException(
                $"Market index '{sourceName}' plugin '{pluginId}' capabilities.sharedContracts must match manifest.sharedContracts.");
        }

        return new MarketPluginV2
        {
            PluginId = pluginId,
            Manifest = manifest,
            Compatibility = compatibility,
            Repository = repository,
            Publication = publication,
            Capabilities = capabilities
        };
    }
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
    public List<MarketSharedContractRefV2> SharedContracts { get; init; } = [];

    public MarketPluginManifestV2 ValidateAndNormalize(string sourceName)
    {
        var normalizedContracts = NormalizeContractRefs(SharedContracts, sourceName, nameof(SharedContracts));

        return new MarketPluginManifestV2
        {
            Id = MarketValidation.Require(Id, nameof(Id), sourceName),
            Name = MarketValidation.Require(Name, nameof(Name), sourceName),
            Description = MarketValidation.Require(Description, nameof(Description), sourceName),
            Author = MarketValidation.Require(Author, nameof(Author), sourceName),
            Version = MarketValidation.NormalizeVersion(Version, nameof(Version), sourceName),
            ApiVersion = MarketValidation.NormalizeVersion(ApiVersion, nameof(ApiVersion), sourceName),
            EntranceAssembly = MarketValidation.Require(EntranceAssembly, nameof(EntranceAssembly), sourceName),
            SharedContracts = normalizedContracts
        };
    }

    private static List<MarketSharedContractRefV2> NormalizeContractRefs(
        IReadOnlyCollection<MarketSharedContractRefV2> contracts,
        string sourceName,
        string propertyName)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var normalized = new List<MarketSharedContractRefV2>(contracts.Count);
        foreach (var contract in contracts)
        {
            var normalizedContract = contract.ValidateAndNormalize(sourceName);
            var key = $"{normalizedContract.Id}@{normalizedContract.Version}@{normalizedContract.AssemblyName}";
            if (!seen.Add(key))
            {
                throw new InvalidOperationException(
                    $"Market index '{sourceName}' contains duplicate shared contract '{key}' in '{propertyName}'.");
            }

            normalized.Add(normalizedContract);
        }

        return normalized;
    }
}

internal sealed class MarketPluginCompatibilityV2
{
    [JsonPropertyName("minHostVersion")]
    public string MinHostVersion { get; init; } = string.Empty;

    [JsonPropertyName("pluginApiVersion")]
    public string PluginApiVersion { get; init; } = string.Empty;

    public MarketPluginCompatibilityV2 ValidateAndNormalize(string sourceName)
    {
        return new MarketPluginCompatibilityV2
        {
            MinHostVersion = MarketValidation.NormalizeVersion(MinHostVersion, nameof(MinHostVersion), sourceName),
            PluginApiVersion = MarketValidation.NormalizeVersion(PluginApiVersion, nameof(PluginApiVersion), sourceName)
        };
    }
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

    public MarketPluginRepositoryV2 ValidateAndNormalize(string sourceName)
    {
        var normalizedTags = Tags
            .Select(MarketValidation.Normalize)
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(tag => tag, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (normalizedTags.Count != Tags.Count(tag => !string.IsNullOrWhiteSpace(tag)))
        {
            throw new InvalidOperationException(
                $"Market index '{sourceName}' contains duplicate or blank tags in repository metadata.");
        }

        return new MarketPluginRepositoryV2
        {
            IconUrl = MarketValidation.NormalizeUrl(IconUrl, nameof(IconUrl), sourceName),
            ProjectUrl = MarketValidation.NormalizeUrl(ProjectUrl, nameof(ProjectUrl), sourceName),
            ReadmeUrl = MarketValidation.NormalizeUrl(ReadmeUrl, nameof(ReadmeUrl), sourceName),
            HomepageUrl = MarketValidation.NormalizeUrl(HomepageUrl, nameof(HomepageUrl), sourceName),
            RepositoryUrl = MarketValidation.NormalizeUrl(RepositoryUrl, nameof(RepositoryUrl), sourceName),
            Tags = normalizedTags,
            ReleaseNotes = MarketValidation.Require(ReleaseNotes, nameof(ReleaseNotes), sourceName)
        };
    }
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
    public List<MarketPackageSourceV2> PackageSources { get; init; } = [];

    public MarketPluginPublicationV2 ValidateAndNormalize(string sourceName, string pluginId)
    {
        if (PublishedAt == default || UpdatedAt == default)
        {
            throw new InvalidOperationException(
                $"Market index '{sourceName}' plugin '{pluginId}' is missing valid publish timestamps.");
        }

        if (PackageSizeBytes <= 0)
        {
            throw new InvalidOperationException(
                $"Market index '{sourceName}' plugin '{pluginId}' declares invalid packageSizeBytes '{PackageSizeBytes}'.");
        }

        var normalizedSources = new List<MarketPackageSourceV2>(PackageSources.Count);
        var seenKinds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var source in PackageSources)
        {
            var normalizedSource = source.ValidateAndNormalize(sourceName, pluginId);
            if (!seenKinds.Add(normalizedSource.Kind))
            {
                throw new InvalidOperationException(
                    $"Market index '{sourceName}' plugin '{pluginId}' contains duplicate package source kind '{normalizedSource.Kind}'.");
            }
            normalizedSources.Add(normalizedSource);
        }

        var requiredOrder = new[] { "releaseAsset", "rawFallback", "workspaceLocal" };
        if (normalizedSources.Count != requiredOrder.Length)
        {
            throw new InvalidOperationException(
                $"Market index '{sourceName}' plugin '{pluginId}' must provide exactly these package sources in order: {string.Join(", ", requiredOrder)}.");
        }

        for (var i = 0; i < requiredOrder.Length; i++)
        {
            if (!string.Equals(normalizedSources[i].Kind, requiredOrder[i], StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Market index '{sourceName}' plugin '{pluginId}' packageSources[{i}] must be '{requiredOrder[i]}'.");
            }
        }

        return new MarketPluginPublicationV2
        {
            ReleaseTag = MarketValidation.NormalizeReleaseTag(ReleaseTag, nameof(ReleaseTag), sourceName),
            ReleaseAssetName = MarketValidation.Require(ReleaseAssetName, nameof(ReleaseAssetName), sourceName),
            PublishedAt = PublishedAt,
            UpdatedAt = UpdatedAt,
            PackageSizeBytes = PackageSizeBytes,
            Sha256 = MarketValidation.NormalizeSha256(Sha256, nameof(Sha256), sourceName),
            Md5 = MarketValidation.NormalizeMd5(Md5, nameof(Md5), sourceName),
            PackageSources = normalizedSources
        };
    }
}

internal sealed class MarketPluginCapabilitiesV2
{
    [JsonPropertyName("sharedContracts")]
    public List<MarketSharedContractRefV2> SharedContracts { get; init; } = [];

    [JsonPropertyName("desktopComponents")]
    public List<string> DesktopComponents { get; init; } = [];

    [JsonPropertyName("settingsSections")]
    public List<string> SettingsSections { get; init; } = [];

    [JsonPropertyName("exports")]
    public List<string> Exports { get; init; } = [];

    [JsonPropertyName("messageTypes")]
    public List<string> MessageTypes { get; init; } = [];

    public MarketPluginCapabilitiesV2 ValidateAndNormalize(string sourceName, string pluginId)
    {
        var sharedContracts = SharedContracts
            .Select(contract => contract.ValidateAndNormalize(sourceName))
            .ToList();

        var normalizedComponents = NormalizeDistinctStrings(DesktopComponents, sourceName, pluginId, nameof(DesktopComponents));
        var normalizedSections = NormalizeDistinctStrings(SettingsSections, sourceName, pluginId, nameof(SettingsSections));
        var normalizedExports = NormalizeDistinctStrings(Exports, sourceName, pluginId, nameof(Exports));
        var normalizedMessages = NormalizeDistinctStrings(MessageTypes, sourceName, pluginId, nameof(MessageTypes));

        return new MarketPluginCapabilitiesV2
        {
            SharedContracts = sharedContracts,
            DesktopComponents = normalizedComponents,
            SettingsSections = normalizedSections,
            Exports = normalizedExports,
            MessageTypes = normalizedMessages
        };
    }

    private static List<string> NormalizeDistinctStrings(
        IReadOnlyCollection<string> values,
        string sourceName,
        string pluginId,
        string propertyName)
    {
        var normalized = values
            .Select(MarketValidation.Normalize)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalized.Count != values.Count(value => !string.IsNullOrWhiteSpace(value)))
        {
            throw new InvalidOperationException(
                $"Market index '{sourceName}' plugin '{pluginId}' contains duplicate or blank entries in '{propertyName}'.");
        }

        return normalized;
    }
}

internal sealed class MarketPackageSourceV2
{
    [JsonPropertyName("kind")]
    public string Kind { get; init; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; init; } = string.Empty;

    [JsonPropertyName("sha256")]
    public string? Sha256 { get; init; }

    [JsonPropertyName("packageSizeBytes")]
    public long? PackageSizeBytes { get; init; }

    public MarketPackageSourceV2 ValidateAndNormalize(string sourceName, string pluginId)
    {
        var normalizedKind = MarketValidation.Require(Kind, nameof(Kind), sourceName);
        if (normalizedKind is not ("releaseAsset" or "rawFallback" or "workspaceLocal"))
        {
            throw new InvalidOperationException(
                $"Market index '{sourceName}' plugin '{pluginId}' declares unsupported package source kind '{normalizedKind}'.");
        }

        if (PackageSizeBytes is { } packageSizeBytes && packageSizeBytes <= 0)
        {
            throw new InvalidOperationException(
                $"Market index '{sourceName}' plugin '{pluginId}' declares invalid packageSizeBytes '{PackageSizeBytes}' for source '{normalizedKind}'.");
        }

        var normalizedUrl = NormalizeSourceUrl(Url, normalizedKind, sourceName, pluginId);

        return new MarketPackageSourceV2
        {
            Kind = normalizedKind,
            Url = normalizedUrl,
            Sha256 = string.IsNullOrWhiteSpace(Sha256) ? null : MarketValidation.NormalizeSha256(Sha256, nameof(Sha256), sourceName),
            PackageSizeBytes = PackageSizeBytes
        };
    }

    private static string NormalizeSourceUrl(
        string? rawUrl,
        string kind,
        string sourceName,
        string pluginId)
    {
        var normalized = MarketValidation.Require(rawUrl, nameof(Url), sourceName);
        if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException(
                $"Market index '{sourceName}' plugin '{pluginId}' declares invalid URL '{normalized}' for package source '{kind}'.");
        }

        if (kind == "workspaceLocal")
        {
            if (!string.Equals(uri.Scheme, "workspace", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Market index '{sourceName}' plugin '{pluginId}' package source '{kind}' must use workspace:// URL.");
            }
            return normalized;
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException(
                $"Market index '{sourceName}' plugin '{pluginId}' package source '{kind}' must use http(s) URL.");
        }

        return normalized;
    }
}

internal sealed class MarketSharedContractRefV2
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; init; } = string.Empty;

    [JsonPropertyName("assemblyName")]
    public string AssemblyName { get; init; } = string.Empty;

    public MarketSharedContractRefV2 ValidateAndNormalize(string sourceName)
    {
        return new MarketSharedContractRefV2
        {
            Id = MarketValidation.Require(Id, nameof(Id), sourceName),
            Version = MarketValidation.NormalizeVersion(Version, nameof(Version), sourceName),
            AssemblyName = MarketValidation.Require(AssemblyName, nameof(AssemblyName), sourceName)
        };
    }
}
