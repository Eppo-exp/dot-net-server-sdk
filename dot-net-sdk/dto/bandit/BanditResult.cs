using Newtonsoft.Json;

namespace eppo_sdk.dto.bandit;

public class BanditResult
{
    public string Variation { get; }
    public string? Action { get; }

    [JsonConstructor]
    public BanditResult(string Variation, string? Action)
    {
        this.Variation = Variation;
        this.Action = Action;
    }

    public BanditResult(string variation) : this(variation, null)
    {
    }


    public override string ToString() => Action ?? Variation;
}
