using System.Text.Json;
using System.Text.Json.Serialization;

return RunAsync(args);

static int RunAsync(string[] args)
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
        var index = MarketIndex.Load(File.ReadAllText(indexPath), indexPath);

        Console.WriteLine($"Validated '{indexPath}'.");
        Console.WriteLine($"SchemaVersion: {index.SchemaVersion}");
        Console.WriteLine($"Source: {index.SourceName} ({index.SourceId})");
        Console.WriteLine($"AirApp SDK API: {AirAppMarketPolicy.ApiVersion}");
        Console.WriteLine($"Contracts: {index.Contracts.Count}");
        Console.WriteLine($"AirApps: {index.AirApps.Count}");
        return 0;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine(ex.Message);
        return 1;
    }
}

/// <summary>
/// Market publishing policy. Keep in sync with the host's <c>AirAppSdkInfo.ApiVersion</c> and
/// <c>AirAppMarketSchema.Version</c>.
/// </summary>
internal static class AirAppMarketPolicy
{
    /// <summary>The only market index schema the host understands.</summary>
    public const string SchemaVersion = "3.0.0";

    /// <summary>
    /// The single AirApp SDK API version the official market publishes for. An entry whose major
    /// differs is refused by every shipped host, so it must never reach the index.
    /// </summary>
    public const string ApiVersion = "1.0.0";

    public static readonly Version ApiVersionParsed = Version.Parse(ApiVersion);
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

    public static string NormalizeOptionalUrl(string? value, string propertyName, string sourceName)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : NormalizeUrl(value, propertyName, sourceName);
    }

    /// <summary>
    /// The host rebuilds release and raw URLs from this value, so it must be a bare
    /// <c>https://github.com/{owner}/{repository}</c> root.
    /// </summary>
    public static string NormalizeGitHubRepositoryUrl(string? value, string propertyName, string sourceName)
    {
        var normalized = NormalizeUrl(value, propertyName, sourceName);
        var uri = new Uri(normalized);
        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (!string.Equals(uri.Host, "github.com", StringComparison.OrdinalIgnoreCase) || segments.Length != 2)
        {
            throw new InvalidOperationException(
                $"Market index '{sourceName}' declares '{normalized}' for '{propertyName}'. Expected 'https://github.com/{{owner}}/{{repository}}'.");
        }

        return normalized;
    }

    public static Version NormalizeVersion(string? value, string propertyName, string sourceName)
    {
        var normalized = Require(value, propertyName, sourceName);
        if (!TryParseVersion(normalized, out var parsed) || parsed is null)
        {
            throw new InvalidOperationException(
                $"Market index '{sourceName}' declares invalid version '{normalized}' for '{propertyName}'.");
        }

        return parsed;
    }

    public static string NormalizeHex(string? value, int length, string propertyName, string sourceName)
    {
        var normalized = Require(value, propertyName, sourceName).ToLowerInvariant();
        if (normalized.Length != length || normalized.Any(ch => !Uri.IsHexDigit(ch)))
        {
            throw new InvalidOperationException(
                $"Market index '{sourceName}' declares invalid {propertyName} '{normalized}'. Expected {length} hex digits.");
        }

        return normalized;
    }

    public static string NormalizeOptionalHex(string? value, int length, string propertyName, string sourceName)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : NormalizeHex(value, length, propertyName, sourceName);
    }

    public static string NormalizeReleaseTag(string? value, string propertyName, string sourceName)
    {
        var normalized = Require(value, propertyName, sourceName);
        if (!normalized.StartsWith('v') || !TryParseVersion(normalized[1..], out _))
        {
            throw new InvalidOperationException(
                $"Market index '{sourceName}' declares invalid release tag '{normalized}' for '{propertyName}'. Expected format 'v1.2.3'.");
        }

        return normalized;
    }

    public static List<string> NormalizeDistinct(
        IReadOnlyCollection<string> values,
        string propertyName,
        string sourceName)
    {
        var present = values.Where(value => !string.IsNullOrWhiteSpace(value)).ToList();
        var normalized = present
            .Select(Normalize)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalized.Count != present.Count)
        {
            throw new InvalidOperationException(
                $"Market index '{sourceName}' contains duplicate entries in '{propertyName}'.");
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

/// <summary>
/// The self-contained flat market index (schemaVersion 3.0.0). Mirrors the host's
/// <c>AirAppMarketIndexDocument</c>: every AirApp entry carries all of its display and acquisition
/// metadata inline, and the wire format keeps the historical <c>plugins</c>/<c>pluginId</c> keys.
/// </summary>
internal sealed class MarketIndex
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
    public List<MarketContract> Contracts { get; init; } = [];

    [JsonPropertyName("plugins")]
    public List<MarketAirApp> AirApps { get; init; } = [];

    public static MarketIndex Load(string json, string sourceName)
    {
        var document = JsonSerializer.Deserialize<MarketIndex>(json.TrimStart('﻿'), MarketValidation.JsonOptions)
            ?? throw new InvalidOperationException($"Failed to parse market index '{sourceName}'.");

        return document.ValidateAndNormalize(sourceName);
    }

    private MarketIndex ValidateAndNormalize(string sourceName)
    {
        var schemaVersion = MarketValidation.Require(SchemaVersion, nameof(SchemaVersion), sourceName);
        if (!string.Equals(schemaVersion, AirAppMarketPolicy.SchemaVersion, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Market index '{sourceName}' uses schemaVersion '{schemaVersion}', but the host only supports '{AirAppMarketPolicy.SchemaVersion}'.");
        }

        if (GeneratedAt == default)
        {
            throw new InvalidOperationException($"Market index '{sourceName}' is missing a valid generatedAt timestamp.");
        }

        var contracts = new List<MarketContract>(Contracts.Count);
        var contractKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var contract in Contracts)
        {
            var normalized = contract.ValidateAndNormalize(sourceName);
            if (!contractKeys.Add($"{normalized.Id}@{normalized.Version}"))
            {
                throw new InvalidOperationException(
                    $"Market index '{sourceName}' contains duplicate shared contract '{normalized.Id}@{normalized.Version}'.");
            }

            contracts.Add(normalized);
        }

        var airApps = new List<MarketAirApp>(AirApps.Count);
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var airApp in AirApps)
        {
            var normalized = airApp.ValidateAndNormalize(sourceName);
            if (!seenIds.Add(normalized.AirAppId))
            {
                throw new InvalidOperationException(
                    $"Market index '{sourceName}' contains duplicate AirApp id '{normalized.AirAppId}'.");
            }

            // The host resolves shared contracts against this index's contracts list, so a dangling
            // reference means the AirApp installs and then fails to load.
            foreach (var reference in normalized.SharedContracts)
            {
                if (!contractKeys.Contains($"{reference.Id}@{reference.Version}"))
                {
                    throw new InvalidOperationException(
                        $"Market index '{sourceName}' AirApp '{normalized.AirAppId}' requires shared contract " +
                        $"'{reference.Id}@{reference.Version}', which the index does not publish.");
                }
            }

            airApps.Add(normalized);
        }

        return new MarketIndex
        {
            SchemaVersion = AirAppMarketPolicy.SchemaVersion,
            SourceId = MarketValidation.Require(SourceId, nameof(SourceId), sourceName),
            SourceName = MarketValidation.Require(SourceName, nameof(SourceName), sourceName),
            GeneratedAt = GeneratedAt,
            Contracts = contracts,
            AirApps = airApps
        };
    }
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

    public MarketContract ValidateAndNormalize(string sourceName)
    {
        if (PackageSizeBytes <= 0)
        {
            throw new InvalidOperationException(
                $"Market index '{sourceName}' declares invalid packageSizeBytes '{PackageSizeBytes}' for contract '{Id}'.");
        }

        return new MarketContract
        {
            Id = MarketValidation.Require(Id, nameof(Id), sourceName),
            Version = MarketValidation.NormalizeVersion(Version, nameof(Version), sourceName).ToString(),
            AssemblyName = MarketValidation.Require(AssemblyName, nameof(AssemblyName), sourceName),
            DownloadUrl = MarketValidation.NormalizeUrl(DownloadUrl, nameof(DownloadUrl), sourceName),
            Sha256 = MarketValidation.NormalizeHex(Sha256, 64, nameof(Sha256), sourceName),
            PackageSizeBytes = PackageSizeBytes
        };
    }
}

internal sealed class MarketSharedContractReference
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; init; } = string.Empty;

    [JsonPropertyName("assemblyName")]
    public string AssemblyName { get; init; } = string.Empty;

    public MarketSharedContractReference ValidateAndNormalize(string sourceName)
    {
        return new MarketSharedContractReference
        {
            Id = MarketValidation.Require(Id, nameof(Id), sourceName),
            Version = MarketValidation.NormalizeVersion(Version, nameof(Version), sourceName).ToString(),
            AssemblyName = MarketValidation.Require(AssemblyName, nameof(AssemblyName), sourceName)
        };
    }
}

internal sealed class MarketPackageSource
{
    [JsonPropertyName("kind")]
    public string Kind { get; init; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; init; } = string.Empty;

    public MarketPackageSource ValidateAndNormalize(string sourceName, string airAppId)
    {
        var kind = MarketValidation.Require(Kind, nameof(Kind), sourceName);
        if (kind is not ("releaseAsset" or "rawFallback" or "workspaceLocal"))
        {
            throw new InvalidOperationException(
                $"Market index '{sourceName}' AirApp '{airAppId}' declares unsupported package source kind '{kind}'.");
        }

        var url = MarketValidation.Require(Url, nameof(Url), sourceName);
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException(
                $"Market index '{sourceName}' AirApp '{airAppId}' declares invalid URL '{url}' for package source '{kind}'.");
        }

        if (kind == "workspaceLocal")
        {
            if (!string.Equals(uri.Scheme, "workspace", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Market index '{sourceName}' AirApp '{airAppId}' package source '{kind}' must use a workspace:// URL.");
            }
        }
        else if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException(
                $"Market index '{sourceName}' AirApp '{airAppId}' package source '{kind}' must use an http(s) URL.");
        }

        return new MarketPackageSource { Kind = kind, Url = url };
    }
}

internal sealed class MarketAirApp
{
    [JsonPropertyName("pluginId")]
    public string AirAppId { get; init; } = string.Empty;

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
    public List<MarketSharedContractReference> SharedContracts { get; init; } = [];

    [JsonPropertyName("packageSources")]
    public List<MarketPackageSource> PackageSources { get; init; } = [];

    [JsonPropertyName("desktopComponents")]
    public List<string> DesktopComponents { get; init; } = [];

    [JsonPropertyName("settingsSections")]
    public List<string> SettingsSections { get; init; } = [];

    [JsonPropertyName("exports")]
    public List<string> Exports { get; init; } = [];

    [JsonPropertyName("messageTypes")]
    public List<string> MessageTypes { get; init; } = [];

    public MarketAirApp ValidateAndNormalize(string sourceName)
    {
        var airAppId = MarketValidation.Require(AirAppId, nameof(AirAppId), sourceName);

        var apiVersion = MarketValidation.NormalizeVersion(ApiVersion, nameof(ApiVersion), sourceName);
        if (apiVersion.Major != AirAppMarketPolicy.ApiVersionParsed.Major)
        {
            throw new InvalidOperationException(
                $"Market index '{sourceName}' AirApp '{airAppId}' declares apiVersion '{ApiVersion}', " +
                $"but the market publishes for AirApp SDK API '{AirAppMarketPolicy.ApiVersion}'. " +
                "The major version must match or no shipped host can install it.");
        }

        if (PackageSizeBytes < 0)
        {
            throw new InvalidOperationException(
                $"Market index '{sourceName}' AirApp '{airAppId}' declares invalid packageSizeBytes '{PackageSizeBytes}'.");
        }

        return new MarketAirApp
        {
            AirAppId = airAppId,
            Name = MarketValidation.Require(Name, nameof(Name), sourceName),
            Description = MarketValidation.Require(Description, nameof(Description), sourceName),
            Author = MarketValidation.Require(Author, nameof(Author), sourceName),
            Version = MarketValidation.NormalizeVersion(Version, nameof(Version), sourceName).ToString(),
            ApiVersion = apiVersion.ToString(),
            MinHostVersion = string.IsNullOrWhiteSpace(MinHostVersion)
                ? string.Empty
                : MarketValidation.NormalizeVersion(MinHostVersion, nameof(MinHostVersion), sourceName).ToString(),
            EntranceAssembly = MarketValidation.Require(EntranceAssembly, nameof(EntranceAssembly), sourceName),
            IconUrl = MarketValidation.NormalizeOptionalUrl(IconUrl, nameof(IconUrl), sourceName),
            ReadmeUrl = MarketValidation.NormalizeOptionalUrl(ReadmeUrl, nameof(ReadmeUrl), sourceName),
            ProjectUrl = MarketValidation.NormalizeOptionalUrl(ProjectUrl, nameof(ProjectUrl), sourceName),
            HomepageUrl = MarketValidation.NormalizeOptionalUrl(HomepageUrl, nameof(HomepageUrl), sourceName),
            RepositoryUrl = MarketValidation.NormalizeGitHubRepositoryUrl(RepositoryUrl, nameof(RepositoryUrl), sourceName),
            ReleaseTag = MarketValidation.NormalizeReleaseTag(ReleaseTag, nameof(ReleaseTag), sourceName),
            ReleaseAssetName = MarketValidation.Require(ReleaseAssetName, nameof(ReleaseAssetName), sourceName),
            Sha256 = MarketValidation.NormalizeOptionalHex(Sha256, 64, nameof(Sha256), sourceName),
            Md5 = MarketValidation.NormalizeOptionalHex(Md5, 32, nameof(Md5), sourceName),
            PackageSizeBytes = PackageSizeBytes,
            PublishedAt = PublishedAt,
            UpdatedAt = UpdatedAt,
            ReleaseNotes = MarketValidation.Normalize(ReleaseNotes),
            Tags = MarketValidation.NormalizeDistinct(Tags, nameof(Tags), sourceName),
            SharedContracts = NormalizeSharedContracts(sourceName, airAppId),
            PackageSources = NormalizePackageSources(sourceName, airAppId),
            DesktopComponents = MarketValidation.NormalizeDistinct(DesktopComponents, nameof(DesktopComponents), sourceName),
            SettingsSections = MarketValidation.NormalizeDistinct(SettingsSections, nameof(SettingsSections), sourceName),
            Exports = MarketValidation.NormalizeDistinct(Exports, nameof(Exports), sourceName),
            MessageTypes = MarketValidation.NormalizeDistinct(MessageTypes, nameof(MessageTypes), sourceName)
        };
    }

    private List<MarketSharedContractReference> NormalizeSharedContracts(string sourceName, string airAppId)
    {
        var normalized = new List<MarketSharedContractReference>(SharedContracts.Count);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var reference in SharedContracts)
        {
            var normalizedReference = reference.ValidateAndNormalize(sourceName);
            if (!seen.Add($"{normalizedReference.Id}@{normalizedReference.Version}"))
            {
                throw new InvalidOperationException(
                    $"Market index '{sourceName}' AirApp '{airAppId}' declares duplicate shared contract " +
                    $"'{normalizedReference.Id}@{normalizedReference.Version}'.");
            }

            normalized.Add(normalizedReference);
        }

        return normalized;
    }

    private List<MarketPackageSource> NormalizePackageSources(string sourceName, string airAppId)
    {
        var normalized = PackageSources
            .Select(source => source.ValidateAndNormalize(sourceName, airAppId))
            .ToList();

        // The host walks these in order and falls back to the next one, so the order is part of the contract.
        var requiredOrder = new[] { "releaseAsset", "rawFallback", "workspaceLocal" };
        if (normalized.Count != requiredOrder.Length ||
            !normalized.Select(source => source.Kind).SequenceEqual(requiredOrder, StringComparer.Ordinal))
        {
            throw new InvalidOperationException(
                $"Market index '{sourceName}' AirApp '{airAppId}' must provide exactly these package sources in order: {string.Join(", ", requiredOrder)}.");
        }

        return normalized;
    }
}
