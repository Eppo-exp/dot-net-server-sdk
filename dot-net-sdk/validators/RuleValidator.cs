using System.Text.RegularExpressions;
using eppo_sdk.dto;
using static eppo_sdk.dto.OperatorType;
using NuGet.Versioning;
using eppo_sdk.helpers;

namespace eppo_sdk.validators;



public static class RuleValidator
{
    public static FlagEvaluation? EvaluateFlag(Flag flag, string subjectKey, SubjectAttributes subjectAttributes)
    {
        if (!flag.enabled) return null;

        var now = DateTimeOffset.Now.ToUnixTimeSeconds();
        foreach (var allocation in flag.allocations)
        {
            if (allocation.startAt.HasValue && allocation.startAt.Value > now || allocation.endAt.HasValue && allocation.endAt.Value < now)
            {
                continue;
            }

            subjectAttributes.Add("id", EppoValue.String(subjectKey));

            if (MatchesAnyRule(allocation.rules, subjectAttributes))
            {
                foreach (var split in allocation.splits)
                {
                    if (MatchesAllShards(split.shards, subjectKey, flag.totalShards))
                    {
                        return new FlagEvaluation(flag.variations[split.variationKey], allocation.doLog, allocation.key);
                    }
                }
            }
        }

        return null;
    }



    // Find the first shard that does not match. If it's null. then all shards match.     
    public static bool MatchesAllShards(IEnumerable<Shard> shards, string subjectKey, int totalShards) => shards.First(shard => !MatchesShard(shard, subjectKey, totalShards)) == null;

    private static bool MatchesShard(Shard shard, string subjectKey, int totalShards)
    {
        var hashKey = shard.salt + "-" + subjectKey;
        var subjectBucket = Sharder.GetShard(hashKey, totalShards);

        return shard.ranges.Any(range => Sharder.IsInRange(subjectBucket, range));
    }

    private static bool MatchesAnyRule(IEnumerable<Rule> rules, SubjectAttributes subject) => rules.Any() && FindMatchingRule(subject, rules) != null;

    public static Rule? FindMatchingRule(SubjectAttributes subjectAttributes, IEnumerable<Rule> rules) => rules.FirstOrDefault(rule => MatchesRule(subjectAttributes, rule));

    private static bool MatchesRule(SubjectAttributes subjectAttributes, Rule rule) => rule.conditions.All(condition => EvaluateCondition(subjectAttributes, condition));
    private static bool EvaluateCondition(SubjectAttributes subjectAttributes, Condition condition)
    {
        try
        {
            if (subjectAttributes.TryGetValue(condition.Attribute, out var value))
            {
                switch (condition.Operator)
                {
                    case GTE:
                        {
                            if (value.isNumeric() && condition.Value.isNumeric())
                            {
                                return value.DoubleValue() >= condition.Value.DoubleValue();
                            }

                            if (NuGetVersion.TryParse(value.StringValue(), out var valueSemver) &&
                                NuGetVersion.TryParse(condition.Value.StringValue(), out var conditionSemver))
                            {
                                return valueSemver >= conditionSemver;
                            }

                            return false;
                        }
                    case GT:
                        {
                            if (value.isNumeric() && condition.Value.isNumeric())
                            {
                                return value.DoubleValue() > condition.Value.DoubleValue();
                            }

                            if (NuGetVersion.TryParse(value.StringValue(), out var valueSemver) &&
                                NuGetVersion.TryParse(condition.Value.StringValue(), out var conditionSemver))
                            {
                                return valueSemver > conditionSemver;
                            }

                            return false;
                        }
                    case LTE:
                        {
                            if (value.isNumeric() && condition.Value.isNumeric())
                            {
                                return value.DoubleValue() <= condition.Value.DoubleValue();
                            }

                            if (NuGetVersion.TryParse(value.StringValue(), out var valueSemver) &&
                                NuGetVersion.TryParse(condition.Value.StringValue(), out var conditionSemver))
                            {
                                return valueSemver <= conditionSemver;
                            }

                            return false;
                        }
                    case LT:
                        {
                            if (value.isNumeric() && condition.Value.isNumeric())
                            {
                                return value.DoubleValue() < condition.Value.DoubleValue();
                            }

                            if (NuGetVersion.TryParse(value.StringValue(), out var valueSemver) &&
                                NuGetVersion.TryParse(condition.Value.StringValue(), out var conditionSemver))
                            {
                                return valueSemver < conditionSemver;
                            }

                            return false;
                        }
                    case MATCHES:
                        {
                            return Regex.Match(value.StringValue(), condition.Value.StringValue(), RegexOptions.IgnoreCase).Success;
                        }
                    case ONE_OF:
                        {
                            return Compare.IsOneOf(value.StringValue(), condition.Value.ArrayValue());
                        }
                    case NOT_ONE_OF:
                        {
                            return !Compare.IsOneOf(value.StringValue(), condition.Value.ArrayValue());
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
