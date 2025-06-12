using System.Collections.Immutable;
using eppo_sdk.dto;
using eppo_sdk.dto.bandit;
using eppo_sdk.http;

namespace eppo_sdk.store;

/// <summary>
/// Immutable configuration snapshot containing flags, bandits, and metadata.
/// Provides thread-safe read-only access to configuration data.
/// This is the primary interface for accessing all configuration in the SDK.
/// </summary>
public sealed class Configuration
{
    private readonly ImmutableDictionary<string, Flag> _flags;
    private readonly ImmutableDictionary<string, Bandit> _bandits;
    private readonly string? _flagConfigVersion;
    private readonly BanditReferences? _banditReferences;
    private readonly ImmutableHashSet<string> _banditModelVersions;

    /// <summary>
    /// Creates an empty configuration.
    /// </summary>
    public static Configuration Empty =>
        new Configuration(
            ImmutableDictionary<string, Flag>.Empty,
            ImmutableDictionary<string, Bandit>.Empty,
            null,
            null,
            ImmutableHashSet<string>.Empty
        );

    /// <summary>
    /// Initializes a new instance of the Configuration class with the specified data.
    /// </summary>
    private Configuration(
        ImmutableDictionary<string, Flag> flags,
        ImmutableDictionary<string, Bandit> bandits,
        string? flagConfigVersion,
        BanditReferences? banditReferences,
        ImmutableHashSet<string> banditModelVersions
    )
    {
        _flags = flags;
        _bandits = bandits;
        _flagConfigVersion = flagConfigVersion;
        _banditReferences = banditReferences;
        _banditModelVersions = banditModelVersions;
    }

    /// <summary>
    /// Creates a new configuration from versioned API responses.
    /// </summary>
    public Configuration(
        VersionedResourceResponse<FlagConfigurationResponse> flagsResponse,
        VersionedResourceResponse<BanditModelResponse> banditsResponse
    )
    {
        var flags = flagsResponse.Resource?.Flags ?? new Dictionary<string, Flag>();
        var bandits = banditsResponse.Resource?.Bandits ?? new Dictionary<string, Bandit>();

        _flags = flags.ToImmutableDictionary();
        _bandits = bandits.ToImmutableDictionary();
        _flagConfigVersion = flagsResponse.VersionIdentifier;
        _banditReferences = flagsResponse.Resource?.BanditReferences;

        _banditModelVersions = bandits.Values.Select(b => b.ModelVersion).ToImmutableHashSet();
    }

    /// <summary>
    /// Creates a new configuration from a flag response, keeping bandits from the current config.
    /// </summary>
    public Configuration WithNewFlags(
        VersionedResourceResponse<FlagConfigurationResponse> flagsResponse
    )
    {
        var flags = flagsResponse.Resource?.Flags ?? new Dictionary<string, Flag>();

        return new Configuration(
            flags.ToImmutableDictionary(),
            _bandits,
            flagsResponse.VersionIdentifier,
            flagsResponse.Resource?.BanditReferences,
            _banditModelVersions
        );
    }

    /// <summary>
    /// Creates a new configuration from collections of flags, bandits, and metadata.
    /// </summary>
    public Configuration(
        IEnumerable<Flag> flags,
        IEnumerable<Bandit> bandits,
        BanditReferences? banditReferences,
        string? flagConfigVersion
    )
    {
        _flags = flags.ToImmutableDictionary(f => f.Key);
        _bandits = bandits.ToImmutableDictionary(b => b.BanditKey);
        _flagConfigVersion = flagConfigVersion;
        _banditReferences = banditReferences;
        _banditModelVersions = bandits.Select(b => b.ModelVersion).ToImmutableHashSet();
    }

    /// <summary>
    /// Attempts to get a flag by key.
    /// </summary>
    public bool TryGetFlag(string key, out Flag? flag)
    {
        return _flags.TryGetValue(key, out flag);
    }

    /// <summary>
    /// Attempts to get a bandit by key.
    /// </summary>
    public bool TryGetBandit(string key, out Bandit? bandit)
    {
        return _bandits.TryGetValue(key, out bandit);
    }

    /// <summary>
    /// Gets the flag configuration version.
    /// </summary>
    public string? GetFlagConfigVersion()
    {
        return _flagConfigVersion;
    }

    /// <summary>
    /// Gets all bandit model versions in this configuration.
    /// </summary>
    public IEnumerable<string> GetBanditModelVersions()
    {
        return _banditModelVersions;
    }

    /// <summary>
    /// Attempts to get a bandit by flag key and variation value.
    /// </summary>
    public bool TryGetBanditByVariation(string flagKey, string variationValue, out Bandit? bandit)
    {
        bandit = null;
        if (_banditReferences == null)
            return false;

        if (
            !_banditReferences.TryGetBanditKey(flagKey, variationValue, out string? banditKey)
            || banditKey == null
        )
        {
            return false;
        }

        return TryGetBandit(banditKey, out bandit);
    }
}
