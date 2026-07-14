using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

return await ValidatorCli.RunAsync(args);

internal static class ValidatorCli
{
    public static Task<int> RunAsync(string[] args)
    {
        try
        {
            var options = CliOptions.Parse(args);
            if (options.ShowHelp)
            {
                PrintHelp();
                return Task.FromResult(0);
            }

            if (options.RunSelfTests)
            {
                ValidatorSelfTests.Run(options.ExpectedApiVersion);
                return Task.FromResult(0);
            }

            var indexPath = Path.GetFullPath(options.IndexPath);
            var schemaPath = Path.GetFullPath(options.SchemaPath ?? Path.Combine(
                Path.GetDirectoryName(indexPath)!,
                "schema",
                "airappmarket-index.schema.json"));

            if (!File.Exists(indexPath))
            {
                throw new FileNotFoundException($"Market index '{indexPath}' was not found.", indexPath);
            }

            if (!File.Exists(schemaPath))
            {
                throw new FileNotFoundException($"Market schema '{schemaPath}' was not found.", schemaPath);
            }

            MarketSchemaContract.Validate(File.ReadAllText(schemaPath), schemaPath);
            var index = MarketIndexDocument.Load(
                File.ReadAllText(indexPath),
                indexPath,
                options.ExpectedApiVersion);

            Console.WriteLine($"Validated '{indexPath}'.");
            Console.WriteLine($"SchemaVersion: {index.SchemaVersion}");
            Console.WriteLine($"Source: {index.SourceName} ({index.SourceId})");
            Console.WriteLine($"GeneratedAt: {index.GeneratedAt:O}");
            Console.WriteLine($"ExpectedPluginApi: {options.ExpectedApiVersion} (major-compatible)");
            Console.WriteLine($"Contracts: {index.Contracts.Count}");
            Console.WriteLine($"Plugins: {index.Plugins.Count}");
            Console.WriteLine($"VerifiedPackages: {index.Plugins.Count(plugin => plugin.PackageSizeBytes > 0 && plugin.Sha256.Length == 64)}");
            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return Task.FromResult(1);
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine("AirAppMarket.Validator");
        Console.WriteLine();
        Console.WriteLine("Validates the production flat AirAppMarket index (schemaVersion 3.0.0).");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  dotnet run --project airappmarket/tools/AirAppMarket.Validator -- [index] [schema] [options]");
        Console.WriteLine("  dotnet run --project airappmarket/tools/AirAppMarket.Validator -- --self-test [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --expected-api-version <version>  Required compatible PluginSdk API. Default: 5.0.0");
        Console.WriteLine("  --self-test                       Run built-in positive and negative regressions.");
        Console.WriteLine("  --help                            Show help.");
    }
}

internal sealed class CliOptions
{
    public string IndexPath { get; private set; } = Path.Combine("airappmarket", "index.json");
    public string? SchemaPath { get; private set; }
    public string ExpectedApiVersion { get; private set; } = "5.0.0";
    public bool RunSelfTests { get; private set; }
    public bool ShowHelp { get; private set; }

    public static CliOptions Parse(string[] args)
    {
        var options = new CliOptions();
        var positionals = new List<string>();

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--expected-api-version":
                    options.ExpectedApiVersion = ReadValue(args, ref i, "--expected-api-version");
                    break;
                case "--self-test":
                    options.RunSelfTests = true;
                    break;
                case "--help":
                case "-h":
                    options.ShowHelp = true;
                    break;
                default:
                    if (args[i].StartsWith("-", StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException($"Unknown option '{args[i]}'.");
                    }

                    positionals.Add(args[i]);
                    break;
            }
        }

        if (positionals.Count > 2)
        {
            throw new InvalidOperationException("Expected at most two positional arguments: index path and schema path.");
        }

        if (positionals.Count > 0)
        {
            options.IndexPath = positionals[0];
        }

        if (positionals.Count > 1)
        {
            options.SchemaPath = positionals[1];
        }

        _ = MarketValidation.ParseVersion(options.ExpectedApiVersion, "expected API version", "command line");
        return options;
    }

    private static string ReadValue(string[] args, ref int index, string option)
    {
        if (index + 1 >= args.Length)
        {
            throw new InvalidOperationException($"Option '{option}' requires a value.");
        }

        index++;
        return args[index];
    }
}

internal static class MarketSchemaContract
{
    private static readonly string[] RequiredPluginProperties =
    [
        "pluginId",
        "name",
        "description",
        "author",
        "version",
        "apiVersion",
        "minHostVersion",
        "entranceAssembly",
        "repositoryUrl",
        "releaseTag",
        "releaseAssetName",
        "sha256",
        "packageSizeBytes",
        "packageSources"
    ];

    public static void Validate(string json, string sourceName)
    {
        using var document = JsonDocument.Parse(json, MarketValidation.DocumentOptions);
        var root = document.RootElement;

        var schemaVersion = root
            .GetProperty("properties")
            .GetProperty("schemaVersion")
            .GetProperty("const")
            .GetString();
        if (!string.Equals(schemaVersion, MarketValidation.SchemaVersion, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Market schema '{sourceName}' must declare schemaVersion const '{MarketValidation.SchemaVersion}'.");
        }

        var plugin = root.GetProperty("$defs").GetProperty("plugin");
        if (!plugin.TryGetProperty("additionalProperties", out var additionalProperties) ||
            additionalProperties.ValueKind != JsonValueKind.False)
        {
            throw new InvalidOperationException($"Market schema '{sourceName}' must disallow unknown plugin properties.");
        }

        var required = plugin.GetProperty("required")
            .EnumerateArray()
            .Select(value => value.GetString())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToHashSet(StringComparer.Ordinal);
        var missing = RequiredPluginProperties.Where(property => !required.Contains(property)).ToArray();
        if (missing.Length > 0)
        {
            throw new InvalidOperationException(
                $"Market schema '{sourceName}' does not require integrity-critical plugin properties: {string.Join(", ", missing)}.");
        }

        var sourceKinds = root
            .GetProperty("$defs")
            .GetProperty("packageSource")
            .GetProperty("properties")
            .GetProperty("kind")
            .GetProperty("enum")
            .EnumerateArray()
            .Select(value => value.GetString())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToArray();
        if (!sourceKinds.SequenceEqual(MarketValidation.PackageSourceKinds, StringComparer.Ordinal))
        {
            throw new InvalidOperationException(
                $"Market schema '{sourceName}' package source kinds must be: {string.Join(", ", MarketValidation.PackageSourceKinds)}.");
        }
    }
}

internal static class MarketValidation
{
    public const string SchemaVersion = "3.0.0";

    public static readonly string[] PackageSourceKinds =
    [
        "releaseAsset",
        "rawFallback",
        "workspaceLocal"
    ];

    public static readonly JsonDocumentOptions DocumentOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip
    };

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    private static readonly Regex VersionPattern = new(
        "^[0-9]+\\.[0-9]+\\.[0-9]+$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex PluginIdPattern = new(
        "^[A-Za-z0-9][A-Za-z0-9._-]*$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static string Require(string? value, string propertyName, string sourceName)
    {
        var normalized = Normalize(value);
        if (normalized.Length == 0)
        {
            throw new InvalidOperationException(
                $"Market index '{sourceName}' is missing required property '{propertyName}'.");
        }

        return normalized;
    }

    public static string Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public static Version ParseVersion(string? value, string propertyName, string sourceName)
    {
        var normalized = Require(value, propertyName, sourceName);
        if (!VersionPattern.IsMatch(normalized) || !Version.TryParse(normalized, out var parsed))
        {
            throw new InvalidOperationException(
                $"Market index '{sourceName}' declares invalid semantic version '{normalized}' for '{propertyName}'. Expected 'major.minor.patch'.");
        }

        return parsed;
    }

    public static string NormalizeVersion(string? value, string propertyName, string sourceName)
    {
        var normalized = Require(value, propertyName, sourceName);
        _ = ParseVersion(normalized, propertyName, sourceName);
        return normalized;
    }

    public static string NormalizePluginId(string? value, string propertyName, string sourceName)
    {
        var normalized = Require(value, propertyName, sourceName);
        if (!PluginIdPattern.IsMatch(normalized) || normalized.Contains("..", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Market index '{sourceName}' declares invalid plugin id '{normalized}'. Use letters, digits, '.', '_' or '-' without whitespace.");
        }

        return normalized;
    }

    public static string NormalizeSha256(string? value, string propertyName, string sourceName)
    {
        var normalized = Require(value, propertyName, sourceName).ToLowerInvariant();
        if (normalized.Length != 64 || normalized.Any(character => !Uri.IsHexDigit(character)))
        {
            throw new InvalidOperationException(
                $"Market index '{sourceName}' declares invalid SHA-256 '{normalized}' for '{propertyName}'.");
        }

        return normalized;
    }

    public static string NormalizeOptionalMd5(string? value, string propertyName, string sourceName)
    {
        var normalized = Normalize(value).ToLowerInvariant();
        if (normalized.Length == 0)
        {
            return string.Empty;
        }

        if (normalized.Length != 32 || normalized.Any(character => !Uri.IsHexDigit(character)))
        {
            throw new InvalidOperationException(
                $"Market index '{sourceName}' declares invalid MD5 '{normalized}' for '{propertyName}'.");
        }

        return normalized;
    }

    public static string NormalizeHttpUrl(string? value, string propertyName, string sourceName, bool optional = false)
    {
        var normalized = Normalize(value);
        if (optional && normalized.Length == 0)
        {
            return string.Empty;
        }

        normalized = Require(normalized, propertyName, sourceName);
        if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException(
                $"Market index '{sourceName}' declares invalid HTTPS URL '{normalized}' for '{propertyName}'.");
        }

        return normalized;
    }

    public static GitHubRepositoryIdentity NormalizeGitHubRepositoryUrl(
        string? value,
        string propertyName,
        string sourceName)
    {
        var normalized = NormalizeHttpUrl(value, propertyName, sourceName);
        if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps ||
            !string.Equals(uri.Host, "github.com", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Market index '{sourceName}' declares non-GitHub repository URL '{normalized}' for '{propertyName}'.");
        }

        var segments = uri.AbsolutePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length != 2 || uri.Query.Length > 0 || uri.Fragment.Length > 0)
        {
            throw new InvalidOperationException(
                $"Market index '{sourceName}' repository URL '{normalized}' must point to a GitHub repository root.");
        }

        return new GitHubRepositoryIdentity(segments[0], segments[1], $"https://github.com/{segments[0]}/{segments[1]}");
    }

    public static string NormalizeAssemblyName(string? value, string propertyName, string sourceName)
    {
        var normalized = Require(value, propertyName, sourceName);
        if (!string.Equals(Path.GetFileName(normalized), normalized, StringComparison.Ordinal) ||
            !normalized.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Market index '{sourceName}' declares invalid assembly name '{normalized}' for '{propertyName}'.");
        }

        return normalized;
    }

    public static string NormalizePackageFileName(string? value, string propertyName, string sourceName)
    {
        var normalized = Require(value, propertyName, sourceName);
        if (!string.Equals(Path.GetFileName(normalized), normalized, StringComparison.Ordinal) ||
            !normalized.EndsWith(".laapp", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Market index '{sourceName}' declares invalid package file name '{normalized}' for '{propertyName}'.");
        }

        return normalized;
    }

    public static List<string> NormalizeDistinctStrings(
        IReadOnlyCollection<string>? values,
        string propertyName,
        string sourceName,
        string pluginId)
    {
        var sourceValues = values ?? [];
        var normalized = new List<string>(sourceValues.Count);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var value in sourceValues)
        {
            var item = Normalize(value);
            if (item.Length == 0 || !seen.Add(item))
            {
                throw new InvalidOperationException(
                    $"Market index '{sourceName}' plugin '{pluginId}' contains a blank or duplicate value in '{propertyName}'.");
            }

            normalized.Add(item);
        }

        return normalized;
    }
}

internal sealed class MarketIndexDocument
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
    public List<MarketContractEntry> Contracts { get; init; } = [];

    [JsonPropertyName("plugins")]
    public List<MarketPluginEntry> Plugins { get; init; } = [];

    public static MarketIndexDocument Load(
        string json,
        string sourceName,
        string expectedApiVersion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceName);

        MarketIndexDocument document;
        try
        {
            document = JsonSerializer.Deserialize<MarketIndexDocument>(
                json.TrimStart('\uFEFF'),
                MarketValidation.JsonOptions) ?? throw new InvalidOperationException(
                    $"Failed to deserialize market index '{sourceName}'.");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Market index '{sourceName}' is not a valid flat v3 document: {ex.Message}",
                ex);
        }

        return document.ValidateAndNormalize(sourceName, expectedApiVersion);
    }

    private MarketIndexDocument ValidateAndNormalize(string sourceName, string expectedApiVersion)
    {
        var schemaVersion = MarketValidation.Require(SchemaVersion, nameof(SchemaVersion), sourceName);
        if (!string.Equals(schemaVersion, MarketValidation.SchemaVersion, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Market index '{sourceName}' uses schemaVersion '{schemaVersion}', but production requires flat schema '{MarketValidation.SchemaVersion}'.");
        }

        if (GeneratedAt == default)
        {
            throw new InvalidOperationException($"Market index '{sourceName}' is missing a valid generatedAt timestamp.");
        }

        var expectedApi = MarketValidation.ParseVersion(expectedApiVersion, "expected API version", sourceName);
        var normalizedContracts = NormalizeContracts(sourceName);
        var normalizedPlugins = NormalizePlugins(sourceName, expectedApi, normalizedContracts);
        if (normalizedPlugins.Count == 0)
        {
            throw new InvalidOperationException($"Market index '{sourceName}' does not declare any plugins.");
        }

        return new MarketIndexDocument
        {
            SchemaVersion = MarketValidation.SchemaVersion,
            SourceId = MarketValidation.Require(SourceId, nameof(SourceId), sourceName),
            SourceName = MarketValidation.Require(SourceName, nameof(SourceName), sourceName),
            GeneratedAt = GeneratedAt,
            Contracts = normalizedContracts,
            Plugins = normalizedPlugins
        };
    }

    private List<MarketContractEntry> NormalizeContracts(string sourceName)
    {
        var normalized = new List<MarketContractEntry>((Contracts ?? []).Count);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var contract in Contracts ?? [])
        {
            var item = contract.ValidateAndNormalize(sourceName);
            var key = $"{item.Id}@{item.Version}";
            if (!seen.Add(key))
            {
                throw new InvalidOperationException(
                    $"Market index '{sourceName}' contains duplicate shared contract '{key}'.");
            }

            normalized.Add(item);
        }

        return normalized;
    }

    private List<MarketPluginEntry> NormalizePlugins(
        string sourceName,
        Version expectedApiVersion,
        IReadOnlyCollection<MarketContractEntry> contracts)
    {
        var normalized = new List<MarketPluginEntry>((Plugins ?? []).Count);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var plugin in Plugins ?? [])
        {
            var item = plugin.ValidateAndNormalize(sourceName, expectedApiVersion, contracts);
            if (!seen.Add(item.PluginId))
            {
                throw new InvalidOperationException(
                    $"Market index '{sourceName}' contains duplicate plugin id '{item.PluginId}'.");
            }

            normalized.Add(item);
        }

        return normalized;
    }
}

internal sealed class MarketContractEntry
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

    public MarketContractEntry ValidateAndNormalize(string sourceName)
    {
        if (PackageSizeBytes <= 0)
        {
            throw new InvalidOperationException(
                $"Market index '{sourceName}' declares invalid packageSizeBytes '{PackageSizeBytes}' for shared contract '{Id}'.");
        }

        return new MarketContractEntry
        {
            Id = MarketValidation.Require(Id, nameof(Id), sourceName),
            Version = MarketValidation.NormalizeVersion(Version, nameof(Version), sourceName),
            AssemblyName = MarketValidation.NormalizeAssemblyName(AssemblyName, nameof(AssemblyName), sourceName),
            DownloadUrl = MarketValidation.NormalizeHttpUrl(DownloadUrl, nameof(DownloadUrl), sourceName),
            Sha256 = MarketValidation.NormalizeSha256(Sha256, nameof(Sha256), sourceName),
            PackageSizeBytes = PackageSizeBytes
        };
    }
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
    public List<MarketSharedContractReference> SharedContracts { get; init; } = [];

    [JsonPropertyName("packageSources")]
    public List<MarketPackageSourceEntry> PackageSources { get; init; } = [];

    [JsonPropertyName("desktopComponents")]
    public List<string> DesktopComponents { get; init; } = [];

    [JsonPropertyName("settingsSections")]
    public List<string> SettingsSections { get; init; } = [];

    [JsonPropertyName("exports")]
    public List<string> Exports { get; init; } = [];

    [JsonPropertyName("messageTypes")]
    public List<string> MessageTypes { get; init; } = [];

    public MarketPluginEntry ValidateAndNormalize(
        string sourceName,
        Version expectedApiVersion,
        IReadOnlyCollection<MarketContractEntry> contracts)
    {
        var pluginId = MarketValidation.NormalizePluginId(PluginId, nameof(PluginId), sourceName);
        var version = MarketValidation.NormalizeVersion(Version, nameof(Version), sourceName);
        var apiVersion = MarketValidation.NormalizeVersion(ApiVersion, nameof(ApiVersion), sourceName);
        var parsedApiVersion = MarketValidation.ParseVersion(apiVersion, nameof(ApiVersion), sourceName);
        if (parsedApiVersion.Major != expectedApiVersion.Major)
        {
            throw new InvalidOperationException(
                $"Market index '{sourceName}' plugin '{pluginId}' targets PluginSdk API '{apiVersion}', but production requires API major '{expectedApiVersion.Major}' ({expectedApiVersion}).");
        }

        var minHostVersion = MarketValidation.NormalizeVersion(MinHostVersion, nameof(MinHostVersion), sourceName);
        var parsedMinHostVersion = MarketValidation.ParseVersion(minHostVersion, nameof(MinHostVersion), sourceName);
        if (expectedApiVersion.Major == 5 && parsedMinHostVersion < new Version(0, 8, 6))
        {
            throw new InvalidOperationException(
                $"Market index '{sourceName}' plugin '{pluginId}' targets PluginSdk API 5 and must require host version 0.8.6 or newer, but declares '{minHostVersion}'.");
        }

        var entranceAssembly = MarketValidation.NormalizeAssemblyName(EntranceAssembly, nameof(EntranceAssembly), sourceName);
        var repository = MarketValidation.NormalizeGitHubRepositoryUrl(RepositoryUrl, nameof(RepositoryUrl), sourceName);
        var releaseTag = NormalizeReleaseTag(ReleaseTag, version, sourceName, pluginId);
        var releaseAssetName = MarketValidation.NormalizePackageFileName(ReleaseAssetName, nameof(ReleaseAssetName), sourceName);
        var expectedAssetName = $"{pluginId}.{version}.laapp";
        if (!string.Equals(releaseAssetName, expectedAssetName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Market index '{sourceName}' plugin '{pluginId}' release asset must be named '{expectedAssetName}', but found '{releaseAssetName}'.");
        }

        if (PackageSizeBytes <= 0)
        {
            throw new InvalidOperationException(
                $"Market index '{sourceName}' plugin '{pluginId}' declares invalid packageSizeBytes '{PackageSizeBytes}'.");
        }

        var packageSources = NormalizePackageSources(
            PackageSources,
            sourceName,
            pluginId,
            repository,
            releaseTag,
            releaseAssetName);
        var sharedContracts = NormalizeSharedContracts(SharedContracts, sourceName, pluginId, contracts);

        if (PublishedAt != default && UpdatedAt != default && UpdatedAt < PublishedAt)
        {
            throw new InvalidOperationException(
                $"Market index '{sourceName}' plugin '{pluginId}' updatedAt precedes publishedAt.");
        }

        return new MarketPluginEntry
        {
            PluginId = pluginId,
            Name = MarketValidation.Require(Name, nameof(Name), sourceName),
            Description = MarketValidation.Require(Description, nameof(Description), sourceName),
            Author = MarketValidation.Require(Author, nameof(Author), sourceName),
            Version = version,
            ApiVersion = apiVersion,
            MinHostVersion = minHostVersion,
            EntranceAssembly = entranceAssembly,
            IconUrl = MarketValidation.NormalizeHttpUrl(IconUrl, nameof(IconUrl), sourceName, optional: true),
            ReadmeUrl = MarketValidation.NormalizeHttpUrl(ReadmeUrl, nameof(ReadmeUrl), sourceName, optional: true),
            ProjectUrl = MarketValidation.NormalizeHttpUrl(ProjectUrl, nameof(ProjectUrl), sourceName, optional: true),
            HomepageUrl = MarketValidation.NormalizeHttpUrl(HomepageUrl, nameof(HomepageUrl), sourceName, optional: true),
            RepositoryUrl = repository.NormalizedUrl,
            ReleaseTag = releaseTag,
            ReleaseAssetName = releaseAssetName,
            Sha256 = MarketValidation.NormalizeSha256(Sha256, nameof(Sha256), sourceName),
            Md5 = MarketValidation.NormalizeOptionalMd5(Md5, nameof(Md5), sourceName),
            PackageSizeBytes = PackageSizeBytes,
            PublishedAt = PublishedAt,
            UpdatedAt = UpdatedAt,
            ReleaseNotes = MarketValidation.Normalize(ReleaseNotes),
            Tags = MarketValidation.NormalizeDistinctStrings(Tags, nameof(Tags), sourceName, pluginId),
            SharedContracts = sharedContracts,
            PackageSources = packageSources,
            DesktopComponents = MarketValidation.NormalizeDistinctStrings(DesktopComponents, nameof(DesktopComponents), sourceName, pluginId),
            SettingsSections = MarketValidation.NormalizeDistinctStrings(SettingsSections, nameof(SettingsSections), sourceName, pluginId),
            Exports = MarketValidation.NormalizeDistinctStrings(Exports, nameof(Exports), sourceName, pluginId),
            MessageTypes = MarketValidation.NormalizeDistinctStrings(MessageTypes, nameof(MessageTypes), sourceName, pluginId)
        };
    }

    private static string NormalizeReleaseTag(
        string? rawValue,
        string version,
        string sourceName,
        string pluginId)
    {
        var releaseTag = MarketValidation.Require(rawValue, nameof(ReleaseTag), sourceName);
        if (!releaseTag.StartsWith('v') ||
            !string.Equals(releaseTag[1..], version, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Market index '{sourceName}' plugin '{pluginId}' releaseTag must be exactly 'v{version}', but found '{releaseTag}'.");
        }

        return releaseTag;
    }

    private static List<MarketPackageSourceEntry> NormalizePackageSources(
        IReadOnlyCollection<MarketPackageSourceEntry>? sources,
        string sourceName,
        string pluginId,
        GitHubRepositoryIdentity repository,
        string releaseTag,
        string releaseAssetName)
    {
        var sourceItems = sources ?? [];
        if (sourceItems.Count is < 1 or > 3)
        {
            throw new InvalidOperationException(
                $"Market index '{sourceName}' plugin '{pluginId}' must declare between one and three package sources.");
        }

        var normalized = new List<MarketPackageSourceEntry>(sourceItems.Count);
        var seenKinds = new HashSet<string>(StringComparer.Ordinal);
        var previousOrder = -1;
        foreach (var source in sourceItems)
        {
            var item = source.ValidateAndNormalize(
                sourceName,
                pluginId,
                repository,
                releaseTag,
                releaseAssetName);
            var order = Array.IndexOf(MarketValidation.PackageSourceKinds, item.Kind);
            if (order < previousOrder)
            {
                throw new InvalidOperationException(
                    $"Market index '{sourceName}' plugin '{pluginId}' package sources must follow releaseAsset -> rawFallback -> workspaceLocal order.");
            }

            if (!seenKinds.Add(item.Kind))
            {
                throw new InvalidOperationException(
                    $"Market index '{sourceName}' plugin '{pluginId}' declares duplicate package source kind '{item.Kind}'.");
            }

            previousOrder = order;
            normalized.Add(item);
        }

        if (!seenKinds.Contains("releaseAsset"))
        {
            throw new InvalidOperationException(
                $"Market index '{sourceName}' plugin '{pluginId}' must provide a releaseAsset package source.");
        }

        return normalized;
    }

    private static List<MarketSharedContractReference> NormalizeSharedContracts(
        IReadOnlyCollection<MarketSharedContractReference>? dependencies,
        string sourceName,
        string pluginId,
        IReadOnlyCollection<MarketContractEntry> contracts)
    {
        var normalized = new List<MarketSharedContractReference>((dependencies ?? []).Count);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var dependency in dependencies ?? [])
        {
            var item = dependency.ValidateAndNormalize(sourceName, pluginId);
            var key = $"{item.Id}@{item.Version}";
            if (!seen.Add(key))
            {
                throw new InvalidOperationException(
                    $"Market index '{sourceName}' plugin '{pluginId}' declares duplicate shared contract '{key}'.");
            }

            if (!contracts.Any(contract =>
                    string.Equals(contract.Id, item.Id, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(contract.Version, item.Version, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(contract.AssemblyName, item.AssemblyName, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException(
                    $"Market index '{sourceName}' plugin '{pluginId}' references unpublished shared contract '{key}' ({item.AssemblyName}).");
            }

            normalized.Add(item);
        }

        return normalized;
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

    public MarketSharedContractReference ValidateAndNormalize(string sourceName, string pluginId)
    {
        return new MarketSharedContractReference
        {
            Id = MarketValidation.Require(Id, nameof(Id), sourceName),
            Version = MarketValidation.NormalizeVersion(Version, nameof(Version), sourceName),
            AssemblyName = MarketValidation.NormalizeAssemblyName(AssemblyName, nameof(AssemblyName), sourceName)
        };
    }
}

internal sealed class MarketPackageSourceEntry
{
    [JsonPropertyName("kind")]
    public string Kind { get; init; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; init; } = string.Empty;

    public MarketPackageSourceEntry ValidateAndNormalize(
        string sourceName,
        string pluginId,
        GitHubRepositoryIdentity repository,
        string releaseTag,
        string releaseAssetName)
    {
        var kind = MarketValidation.Require(Kind, nameof(Kind), sourceName);
        if (!MarketValidation.PackageSourceKinds.Contains(kind, StringComparer.Ordinal))
        {
            throw new InvalidOperationException(
                $"Market index '{sourceName}' plugin '{pluginId}' declares unsupported package source kind '{kind}'.");
        }

        var url = MarketValidation.Require(Url, nameof(Url), sourceName);
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException(
                $"Market index '{sourceName}' plugin '{pluginId}' declares invalid package source URL '{url}'.");
        }

        ValidateUrl(kind, uri, sourceName, pluginId, repository, releaseTag, releaseAssetName);
        return new MarketPackageSourceEntry { Kind = kind, Url = url };
    }

    private static void ValidateUrl(
        string kind,
        Uri uri,
        string sourceName,
        string pluginId,
        GitHubRepositoryIdentity repository,
        string releaseTag,
        string releaseAssetName)
    {
        var segments = uri.AbsolutePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(Uri.UnescapeDataString)
            .ToArray();

        var valid = kind switch
        {
            "releaseAsset" =>
                uri.Scheme == Uri.UriSchemeHttps &&
                string.Equals(uri.Host, "github.com", StringComparison.OrdinalIgnoreCase) &&
                segments.Length == 6 &&
                string.Equals(segments[0], repository.Owner, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(segments[1], repository.Name, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(segments[2], "releases", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(segments[3], "download", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(segments[4], releaseTag, StringComparison.Ordinal) &&
                string.Equals(segments[5], releaseAssetName, StringComparison.OrdinalIgnoreCase),
            "rawFallback" =>
                uri.Scheme == Uri.UriSchemeHttps &&
                string.Equals(uri.Host, "raw.githubusercontent.com", StringComparison.OrdinalIgnoreCase) &&
                segments.Length == 4 &&
                string.Equals(segments[0], repository.Owner, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(segments[1], repository.Name, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(segments[2], "main", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(segments[3], releaseAssetName, StringComparison.OrdinalIgnoreCase),
            "workspaceLocal" =>
                string.Equals(uri.Scheme, "workspace", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(uri.Host, repository.Name, StringComparison.OrdinalIgnoreCase) &&
                segments.Length == 1 &&
                string.Equals(segments[0], releaseAssetName, StringComparison.OrdinalIgnoreCase),
            _ => false
        };

        if (!valid)
        {
            throw new InvalidOperationException(
                $"Market index '{sourceName}' plugin '{pluginId}' package source '{kind}' does not match repository '{repository.Owner}/{repository.Name}', release '{releaseTag}', and asset '{releaseAssetName}'.");
        }
    }
}

internal sealed record GitHubRepositoryIdentity(string Owner, string Name, string NormalizedUrl);

internal static class ValidatorSelfTests
{
    public static void Run(string expectedApiVersion)
    {
        var valid = JsonNode.Parse(CreateValidIndexJson())!.AsObject();
        _ = MarketIndexDocument.Load(valid.ToJsonString(), "self-test-valid.json", expectedApiVersion);

        ExpectFailure("nested/legacy schema", valid, expectedApiVersion, root => root["schemaVersion"] = "2.0.0");
        ExpectFailure("wrong PluginSdk major", valid, expectedApiVersion, root => Plugin(root)["apiVersion"] = "4.0.0");
        ExpectFailure("host version predates PluginSdk 5", valid, expectedApiVersion, root => Plugin(root)["minHostVersion"] = "0.8.5");
        ExpectFailure("invalid SHA-256", valid, expectedApiVersion, root => Plugin(root)["sha256"] = "bad");
        ExpectFailure("invalid package size", valid, expectedApiVersion, root => Plugin(root)["packageSizeBytes"] = 0);
        ExpectFailure("release/version mismatch", valid, expectedApiVersion, root => Plugin(root)["releaseTag"] = "v9.9.9");
        ExpectFailure("package source order", valid, expectedApiVersion, root =>
        {
            var sources = Plugin(root)["packageSources"]!.AsArray();
            var first = sources[0]!.DeepClone();
            sources[0] = sources[1]!.DeepClone();
            sources[1] = first;
        });
        ExpectFailure("dangling shared contract", valid, expectedApiVersion, root =>
            Plugin(root)["sharedContracts"]![0]!["version"] = "9.9.9");
        ExpectFailure("unknown flat-v3 property", valid, expectedApiVersion, root =>
            Plugin(root)["legacyPublication"] = new JsonObject());

        Console.WriteLine("AirAppMarket.Validator self-tests passed (1 valid + 9 invalid cases).");
    }

    private static JsonObject Plugin(JsonObject root)
    {
        return root["plugins"]![0]!.AsObject();
    }

    private static void ExpectFailure(
        string name,
        JsonObject valid,
        string expectedApiVersion,
        Action<JsonObject> mutate)
    {
        var candidate = valid.DeepClone().AsObject();
        mutate(candidate);
        try
        {
            _ = MarketIndexDocument.Load(candidate.ToJsonString(), $"self-test-{name}.json", expectedApiVersion);
        }
        catch (InvalidOperationException)
        {
            return;
        }

        throw new InvalidOperationException($"Validator self-test '{name}' unexpectedly succeeded.");
    }

    private static string CreateValidIndexJson()
    {
        return """
            {
              "schemaVersion": "3.0.0",
              "sourceId": "self-test",
              "sourceName": "AirAppMarket Validator Self-Test",
              "generatedAt": "2026-07-14T00:00:00Z",
              "contracts": [
                {
                  "id": "LanMountainDesktop.SharedContracts.Sample",
                  "version": "1.0.0",
                  "assemblyName": "LanMountainDesktop.SharedContracts.Sample.dll",
                  "downloadUrl": "https://raw.githubusercontent.com/owner/LanAirApp/main/contract.dll",
                  "sha256": "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                  "packageSizeBytes": 1024
                }
              ],
              "plugins": [
                {
                  "pluginId": "LanMountainDesktop.SelfTest",
                  "name": "Self Test",
                  "description": "Valid flat v3 fixture.",
                  "author": "LanMountainDesktop",
                  "version": "1.2.3",
                  "apiVersion": "5.0.0",
                  "minHostVersion": "0.8.6",
                  "entranceAssembly": "LanMountainDesktop.SelfTest.dll",
                  "repositoryUrl": "https://github.com/owner/SelfTest",
                  "releaseTag": "v1.2.3",
                  "releaseAssetName": "LanMountainDesktop.SelfTest.1.2.3.laapp",
                  "sha256": "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
                  "packageSizeBytes": 4096,
                  "sharedContracts": [
                    {
                      "id": "LanMountainDesktop.SharedContracts.Sample",
                      "version": "1.0.0",
                      "assemblyName": "LanMountainDesktop.SharedContracts.Sample.dll"
                    }
                  ],
                  "packageSources": [
                    {
                      "kind": "releaseAsset",
                      "url": "https://github.com/owner/SelfTest/releases/download/v1.2.3/LanMountainDesktop.SelfTest.1.2.3.laapp"
                    },
                    {
                      "kind": "rawFallback",
                      "url": "https://raw.githubusercontent.com/owner/SelfTest/main/LanMountainDesktop.SelfTest.1.2.3.laapp"
                    },
                    {
                      "kind": "workspaceLocal",
                      "url": "workspace://SelfTest/LanMountainDesktop.SelfTest.1.2.3.laapp"
                    }
                  ]
                }
              ]
            }
            """;
    }
}
