namespace eppo_sdk.dto;

public class EppoValue
{
    public string value { get; set; }

    public EppoValueType type { get; set; } = EppoValueType.NULL;

    public List<string> array { get; set; }

    public EppoValue()
    {
    }

    public EppoValue(string value, EppoValueType type)
    {
        this.value = value;
        this.type = type;
    }

    public EppoValue(List<string> array)
    {
        this.array = array;
        this.type = EppoValueType.ARRAY_OF_STRING;
    }

    public EppoValue(EppoValueType type)
    {
        this.type = type;
    }
}