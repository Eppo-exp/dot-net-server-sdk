using System.ComponentModel;
using Newtonsoft.Json;

namespace eppo_sdk.dto;

public class Allocation 
{   
    public string Key {get;}
    public List<Rule> Rules {get;} = new List<Rule>();
    public List<Split> Splits {get;}
    public DateTime? StartAt {get;} = null;
    public DateTime? EndAt {get;} = null;

    [DefaultValue(true)]
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
    public bool DoLog {get;} = true;

    public Allocation(string key, List<Rule> rules, List<Split> splits, bool doLog, DateTime? startAt, DateTime? endAt) {
        Key = key;
        Rules = rules;
        Splits = splits;
        StartAt = startAt;
        EndAt = endAt;
        DoLog = doLog;
    }
}
