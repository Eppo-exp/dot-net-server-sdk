using System.Collections.Immutable;
using eppo_sdk.dto;
using eppo_sdk.dto.bandit;

namespace eppo_sdk.store;

/// <summary>
/// Immutable configuration snapshot containing flags, bandits, and metadata.
/// Provides thread-safe read-only access to configuration data.
/// </summary>
public sealed class Configuration
{
    private readonly ImmutableDictionary<string, Flag> _flags;
    private readonly ImmutableDictionary<string, Bandit> _bandits;
    private readonly ImmutableDictionary<string, object> _metadata;

    // Metadata key constants
    private const string KEY_BANDIT_REFERENCES = "banditReferences";
    private const string KEY_BANDIT_VERSIONS = "banditVersions";
    private const string KEY_FLAG_CONFIG_VERSION = "ufcVersion";

    /// <summary>
    /// Initializes a new instance of the Configuration class.
    /// </summary>
    /// <param name="flags">The flags to include in this configuration.</param>
    /// <param name="bandits">The bandits to include in this configuration.</param>
    /// <param name="metadata">The metadata to include in this configuration.</param>
    public Configuration(
        IEnumerable<Flag> flags,
        IEnumerable<Bandit> bandits,
        IDictionary<string, object> metadata
    )
    {
        _flags = flags.ToImmutableDictionary(f => f.Key, f => f);
        _bandits = bandits.ToImmutableDictionary(b => b.BanditKey, b => b);
        _metadata = metadata.ToImmutableDictionary();
    }

    /// <summary>
    /// Attempts to retrieve a flag by its key.
    /// </summary>
    /// <param name="key">The flag key to look up.</param>
    /// <param name="flag">When this method returns, contains the flag associated with the specified key, if found; otherwise, null.</param>
    /// <returns>true if the flag was found; otherwise, false.</returns>
    public bool TryGetFlag(string key, out Flag? flag)
    {
        return _flags.TryGetValue(key, out flag);
    }

    /// <summary>
    /// Attempts to retrieve a bandit by its key.
    /// </summary>
    /// <param name="key">The bandit key to look up.</param>
    /// <param name="bandit">When this method returns, contains the bandit associated with the specified key, if found; otherwise, null.</param>
    /// <returns>true if the bandit was found; otherwise, false.</returns>
    public bool TryGetBandit(string key, out Bandit? bandit)
    {
        return _bandits.TryGetValue(key, out bandit);
    }

    /// <summary>
    /// Attempts to retrieve the bandit references metadata.
    /// </summary>
    /// <param name="banditReferences">When this method returns, contains the bandit references if found; otherwise, null.</param>
    /// <returns>true if bandit references were found; otherwise, false.</returns>
    public bool TryGetBanditReferences(out BanditReferences? banditReferences)
    {
        return TryGetMetadata(KEY_BANDIT_REFERENCES, out banditReferences);
    }

    /// <summary>
    /// Attempts to retrieve the bandit versions metadata.
    /// </summary>
    /// <param name="banditVersions">When this method returns, contains the bandit versions if found; otherwise, null.</param>
    /// <returns>true if bandit versions were found; otherwise, false.</returns>
    public bool TryGetBanditVersions(out IEnumerable<string>? banditVersions)
    {
        return TryGetMetadata(KEY_BANDIT_VERSIONS, out banditVersions);
    }

    /// <summary>
    /// Attempts to retrieve the flag configuration version metadata.
    /// </summary>
    /// <param name="flagConfigVersion">When this method returns, contains the flag configuration version if found; otherwise, null.</param>
    /// <returns>true if flag configuration version was found; otherwise, false.</returns>
    public bool TryGetFlagConfigVersion(out string? flagConfigVersion)
    {
        return TryGetMetadata(KEY_FLAG_CONFIG_VERSION, out flagConfigVersion);
    }

    /// <summary>
    /// Gets the bandit references metadata, or null if not found.
    /// </summary>
    public BanditReferences? BanditReferences
    {
        get
        {
            TryGetBanditReferences(out var banditReferences);
            return banditReferences;
        }
    }

    /// <summary>
    /// Gets the bandit versions metadata, or an empty collection if not found.
    /// </summary>
    public IEnumerable<string> BanditVersions
    {
        get
        {
            TryGetBanditVersions(out var banditVersions);
            return banditVersions ?? Array.Empty<string>();
        }
    }

    /// <summary>
    /// Gets the flag configuration version metadata, or null if not found.
    /// </summary>
    public string? FlagConfigVersion
    {
        get
        {
            TryGetFlagConfigVersion(out var flagConfigVersion);
            return flagConfigVersion;
        }
    }

    /// <summary>
    /// Attempts to retrieve metadata by its key.
    /// </summary>
    /// <typeparam name="TMetadata">The type of the metadata value.</typeparam>
    /// <param name="key">The metadata key to look up.</param>
    /// <param name="metadata">When this method returns, contains the metadata associated with the specified key, if found; otherwise, default value.</param>
    /// <returns>true if the metadata was found; otherwise, false.</returns>
    public bool TryGetMetadata<TMetadata>(string key, out TMetadata? metadata)
    {
        if (_metadata.TryGetValue(key, out var value) && value is TMetadata typedValue)
        {
            metadata = typedValue;
            return true;
        }

        metadata = default;
        return false;
    }

    /// <summary>
    /// Gets all flags in this configuration.
    /// </summary>
    public IEnumerable<Flag> Flags => _flags.Values;

    /// <summary>
    /// Gets all bandits in this configuration.
    /// </summary>
    public IEnumerable<Bandit> Bandits => _bandits.Values;

    /// <summary>
    /// Gets all metadata in this configuration.
    /// </summary>
    public IDictionary<string, object> Metadata => _metadata;
}
