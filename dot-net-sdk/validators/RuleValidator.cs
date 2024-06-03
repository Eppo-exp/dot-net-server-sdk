using System.Text.RegularExpressions;
using eppo_sdk.dto;
using static eppo_sdk.dto.OperatorType;
using NuGet.Versioning;
using eppo_sdk.helpers;
using eppo_sdk.exception;
using Microsoft.Extensions.Logging;
using NLog;

namespace eppo_sdk.validators;



public static partial class RuleValidator
{

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static FlagEvaluation? EvaluateFlag(Flag flag, string subjectKey, Subject subjectAttributes)
    {
        if (!flag.enabled) return null;

        var now = DateTimeOffset.Now;
        foreach (var allocation in flag.allocations)
        {
            if (allocation.startAt.HasValue && allocation.startAt.Value > now || allocation.endAt.HasValue && allocation.endAt.Value < now)
            {
                continue;
            }

            subjectAttributes["id"] = subjectKey;

            if (allocation.rules.Count == 0 || MatchesAnyRule(allocation.rules, subjectAttributes))
            {
                foreach (var split in allocation.splits)
                {
                    if (MatchesAllShards(split.shards, subjectKey, flag.totalShards))
                    {
                        if (flag.variations.TryGetValue(split.variationKey, out Variation variation) && variation != null)
                        {
                            return new FlagEvaluation(variation, allocation.doLog, allocation.key);
                        }
                        throw new ExperimentConfigurationNotFound($"Variation {split.variationKey} could not be found");

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

    private static bool MatchesAnyRule(IEnumerable<Rule> rules, Subject subject) => rules.Any() && FindMatchingRule(subject, rules) != null;

    public static Rule? FindMatchingRule(Subject subjectAttributes, IEnumerable<Rule> rules) => rules.FirstOrDefault(rule => MatchesRule(subjectAttributes, rule));

    private static bool MatchesRule(Subject subjectAttributes, Rule rule) => rule.conditions.All(condition => EvaluateCondition(subjectAttributes, condition));

    private static bool EvaluateCondition(Subject subjectAttributes, Condition condition)
    {
        try
        {
            // Operators other than `IS_NULL` need to assume non-null
            if (condition.Operator == IS_NULL)
            {
                bool isNull = !subjectAttributes.TryGetValue(condition.Attribute, out Object? outVal) || HasEppoValue.IsNullValue(new HasEppoValue(outVal));
                return condition.BoolValue() == isNull;
            }
            else if (subjectAttributes.TryGetValue(condition.Attribute, out Object? outVal))
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
                                NuGetVersion.TryParse(condition.StringValue(), out var conditionSemver))
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
                                NuGetVersion.TryParse(condition.StringValue(), out var conditionSemver))
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
                                NuGetVersion.TryParse(condition.StringValue(), out var conditionSemver))
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

                            if (NuGetVersion.TryParse(value.StringValue(), out var valueSemver) &&
                                NuGetVersion.TryParse(condition.StringValue(), out var conditionSemver))
                            {
                                return valueSemver < conditionSemver;
                            }

                            return false;
                        }
                    case MATCHES:
                        {
                            return Regex.Match(value.StringValue(), condition.StringValue(), RegexOptions.IgnoreCase).Success;
                        }
                    case ONE_OF:
                        {
                            return Compare.IsOneOf(value.StringValue(), condition.ArrayValue());
                        }
                    case NOT_ONE_OF:
                        {
                            return !Compare.IsOneOf(value.StringValue(), condition.ArrayValue());
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

internal class Compare
{
    public static bool IsOneOf(string a, List<string> arrayValues)
    {
        return arrayValues.ConvertAll(v => v.ToLower()).IndexOf(a.ToLower()) >= 0;
    }
}
