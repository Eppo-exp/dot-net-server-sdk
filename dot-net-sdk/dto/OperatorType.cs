using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace eppo_sdk.dto;

[JsonConverter(typeof(StringEnumConverter))]
public enum OperatorType
{
    MATCHES,
    NOT_MATCHES,
    GTE,
    GT,
    LTE,
    LT,
    ONE_OF,
    NOT_ONE_OF,
    IS_NULL,
}
