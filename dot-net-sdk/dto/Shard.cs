namespace eppo_sdk.dto;

    public class Shard
    {
        public string salt { get; }
        public List<ShardRange> ranges { get; set; }

        public Shard(string salt, IEnumerable<ShardRange> ranges)
        {
            this.salt = salt;
            this.ranges = new List<ShardRange>(ranges);
        }
    }
