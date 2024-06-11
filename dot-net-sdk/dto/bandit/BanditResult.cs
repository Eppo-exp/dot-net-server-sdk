namespace eppo_sdk.dto.bandit;

public record BanditResult(string Variation, string? Action)
{
    public BanditResult(string variation) : this(variation, null)
    {
       
    }

    public override string ToString()
    {
        return Action ?? Variation;  // Null-coalescing operator (??)
    }
}