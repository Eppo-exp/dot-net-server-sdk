namespace eppo_sdk.dto;

    public class Allocation
    {
        public string key { get; }
        public List<Rule> rules { get; set; }
        public List<Split> splits { get; set; }
        public bool doLog { get; set; }
        public int? startAt { get; set; }
        public int? endAt { get; set; }

        public Allocation(string key, IEnumerable<Rule> rules, IEnumerable<Split> splits, bool doLog, int? startAt = null, int? endAt = null)
        {
            this.key = key;
            this.rules = new List<Rule>(rules);
            this.splits = new List<Split>(splits);
            this.doLog = doLog;
            this.startAt = startAt;
            this.endAt = endAt;
        }
    }
