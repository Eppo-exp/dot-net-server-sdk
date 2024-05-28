using System.Text.RegularExpressions;
using eppo_sdk.dto;
using static eppo_sdk.dto.OperatorType;
using NuGet.Versioning;

namespace eppo_sdk.validators;



public static class RuleValidator
{
    // public static FlagEvaluation? EvaluateFlag(Flag flag, string subjectKey, Dictionary<string, object> subjectAttributes)
    // {
    //     if (!flag.Enabled) return null;

    //     var now = DateTime.UtcNow.ToUnixTimeSeconds();
    //     foreach (var allocation in flag.Allocations)
    //     {
    //         if (allocation.StartAt.HasValue && allocation.StartAt.Value > now)
    //         {
    //             continue;
    //         }
    //         if (allocation.EndAt.HasValue && allocation.EndAt.Value < now)
    //         {
    //             continue;
    //         }

    //         var subject = new Dictionary<string, object>() { { "id", subjectKey } };
    //         subject.Concat(subjectAttributes);
    //         if (MatchesAnyRule(allocation.Rules, subject))
    //         {
    //             foreach (var split in allocation.Splits)
    //             {
    //                 if (MatchesAllShards(split.Shards, subjectKey, flag.TotalShards))
    //                 {
    //                     return new FlagEvaluation(flag.Variations[split.VariationKey], allocation.DoLog, allocation.Key);
    //                 }
    //             }
    //         }
    //     }

    //     return null;
    // }


    public static bool FindMatchingRule(SubjectAttributes subjectAttributes, List<Rule> rules) => rules.FirstOrDefault(rule => MatchesRule(subjectAttributes, rule)) != default;

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
