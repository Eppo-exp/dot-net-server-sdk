using System.Text.RegularExpressions;
using eppo_sdk.dto;
using static eppo_sdk.dto.OperatorType;
using NuGet.Versioning;
using eppo_sdk.helpers;
using eppo_sdk.exception;
using Newtonsoft.Json;
using System.ComponentModel;

namespace eppo_sdk.validators;



public static partial class RuleValidator
{
    private const string SUBJECT_KEY_FIELD = "id";

    public static FlagEvaluation? EvaluateFlag(Flag flag, string subjectKey, IDictionary<string, object> subjectAttributes)
    {
        if (!flag.enabled) return null;

        var now = DateTimeOffset.Now.ToUniversalTime();
        foreach (var allocation in flag.Allocations)
        {
            if (allocation.startAt.HasValue && allocation.startAt.Value > now || allocation.endAt.HasValue && allocation.endAt.Value < now)
            {
                continue;
            }

            if (!subjectAttributes.ContainsKey(SUBJECT_KEY_FIELD))
            {
                subjectAttributes[SUBJECT_KEY_FIELD] = subjectKey;
            }

            if (allocation.rules == null || allocation.rules.Count == 0 || MatchesAnyRule(allocation.rules, subjectAttributes))
            {
                foreach (var split in allocation.splits)
                {
                    if (MatchesAllShards(split.Shards, subjectKey, flag.totalShards))
                    {
                        if (flag.variations.TryGetValue(split.VariationKey, out Variation? variation) && variation != null)
                        {
                            return new FlagEvaluation(variation, allocation.doLog, allocation.key, split.ExtraLogging);
                        }
                        throw new ExperimentConfigurationNotFound($"Variation {split.VariationKey} could not be found");

                    }
                }
            }
        }

        return null;
    }



    // Find the first shard that does not match. If it's null. then all shards match.     
    public static bool MatchesAllShards(IEnumerable<Shard> shards, string subjectKey, int totalShards) => shards.FirstOrDefault(shard => !MatchesShard(shard, subjectKey, totalShards)) == null;

    private static bool MatchesShard(Shard shard, string subjectKey, int totalShards)
    {
        var hashKey = shard.salt + "-" + subjectKey;
        var subjectBucket = Sharder.GetShard(hashKey, totalShards);

        return shard.ranges.Any(range => Sharder.IsInRange(subjectBucket, range));
    }

    private static bool MatchesAnyRule(IEnumerable<Rule> rules, IDictionary<string, object> subject) => rules.Any() && FindMatchingRule(subject, rules) != null;

    public static Rule? FindMatchingRule(IDictionary<string, object> subjectAttributes, IEnumerable<Rule> rules) => rules.FirstOrDefault(rule => MatchesRule(subjectAttributes, rule));

    private static bool MatchesRule(IDictionary<string, object> subjectAttributes, Rule rule) => rule.conditions.All(condition => EvaluateCondition(subjectAttributes, condition));

    private static bool EvaluateCondition(IDictionary<string, object> subjectAttributes, Condition condition)
    {
        try
        {
            // Operators other than `IS_NULL` need to assume non-null
            bool isNull = !subjectAttributes.TryGetValue(condition.Attribute, out Object? outVal) || HasEppoValue.IsNullValue(new HasEppoValue(outVal));
            if (condition.Operator == IS_NULL)
            {
                return condition.BoolValue() == isNull;
            }
            else if (!isNull)
            {
                var value = new HasEppoValue(outVal!); // Assuming non-null for simplicity, handle nulls as necessary

                switch (condition.Operator)
                {
                    case GTE:
                        {
                            if (value.IsNumeric() && condition.IsNumeric())
                            {
                                return value.DoubleValue() >= condition.DoubleValue();
                            }

                            if (NuGetVersion.TryParse(value.StringValue(), out var valueSemver) &&
                                NuGetVersion.TryParse(Compare.ToString(condition.Value), out var conditionSemver))
                            {
                                return valueSemver >= conditionSemver;
                            }

                            return false;
                        }
                    case GT:
                        {
                            if (value.IsNumeric() && condition.IsNumeric())
                            {
                                return value.DoubleValue() > condition.DoubleValue();
                            }

                            if (NuGetVersion.TryParse(value.StringValue(), out var valueSemver) &&
                                NuGetVersion.TryParse(Compare.ToString(condition.Value), out var conditionSemver))
                            {
                                return valueSemver > conditionSemver;
                            }

                            return false;
                        }
                    case LTE:
                        {
                            if (value.IsNumeric() && condition.IsNumeric())
                            {
                                return value.DoubleValue() <= condition.DoubleValue();
                            }

                            if (NuGetVersion.TryParse(value.StringValue(), out var valueSemver) &&
                                NuGetVersion.TryParse(Compare.ToString(condition.Value), out var conditionSemver))
                            {
                                return valueSemver <= conditionSemver;
                            }

                            return false;
                        }
                    case LT:
                        {
                            if (value.IsNumeric() && condition.IsNumeric())
                            {
                                return value.DoubleValue() < condition.DoubleValue();
                            }

                            if (NuGetVersion.TryParse(Compare.ToString(value.Value), out var valueSemver) &&
                                NuGetVersion.TryParse(Compare.ToString(condition.Value), out var conditionSemver))
                            {
                                return valueSemver < conditionSemver;
                            }

                            return false;
                        }
                    case MATCHES:
                        {
                            return Regex.Match(Compare.ToString(value.Value), Compare.ToString(condition.Value)).Success;
                        }
                    case NOT_MATCHES:
                        {
                            return !Regex.Match(Compare.ToString(value.Value), Compare.ToString(condition.Value)).Success;
                        }
                    case ONE_OF:
                        {
                            return Compare.IsOneOf(value, condition.ArrayValue());
                        }
                    case NOT_ONE_OF:
                        {
                            return !Compare.IsOneOf(value, condition.ArrayValue());
                        }
                }
            }
            return false; // Return false if attribute is not found or other errors occur
        }
        catch (Exception)
        {
            return false;
        }
    }
}

public class Compare
{
    public static bool IsOneOf(HasEppoValue value, List<string> arrayValues)
    {
        return arrayValues.IndexOf(ToString(value.Value)) >= 0;
    }
    public static string ToString(object? obj)
    {
        // Simple casting to string except for tricksy floats.
        if (obj is string v)
        {
            return v;
        }
        else if (obj is long i)
        {
            return Convert.ToString(i);
        }
        else if ((obj is double || obj is float) && Math.Truncate((double)obj) == (double)obj)
        {
            // Example: 123456789.0 is cast to a more suitable format of int.
            return Convert.ToString(Convert.ToInt32(obj));
        }
        else if (obj != null && (obj is double || obj is float))
        {
            // Explicit conversion of doubles/floats although they're handled by the fallthrough below.
            return Convert.ToString(obj)!;
        }
        // Cross-SDK standard for encoding other possible value types such as bool, null and list<strings>
        return JsonConvert.SerializeObject(obj);
    }
}
