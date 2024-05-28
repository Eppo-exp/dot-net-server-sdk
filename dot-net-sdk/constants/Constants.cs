namespace eppo_sdk.constants;

public class Constants
{
    public const string DEFAULT_BASE_URL = "https://fscdn.eppo.cloud/api";

    public const int REQUEST_TIMEOUT_MILLIS = 1000;

    private const long MILLISECOND_PER_SECOND = 1000;

    public const long TIME_INTERVAL_IN_MILLIS = 30 * MILLISECOND_PER_SECOND;

    public const long JITTER_INTERVAL_IN_MILLIS = 5 * MILLISECOND_PER_SECOND;

    public const int MAX_CACHE_ENTRIES = 1000;

    public const string RAC_ENDPOINT = "/randomized_assignment/v3/config";
    public const string UFC_ENDPOINT = "/flag-config/v1/config";
}