using System.Text.RegularExpressions;
using eppo_sdk.dto;
using static eppo_sdk.dto.OperatorType;
using NuGet.Versioning;

namespace eppo_sdk.validators;

public class RuleValidator
{
    public static Rule? FindMatchingRule(SubjectAttributes subjectAttributes, List<Rule> rules)
    {
        return rules.Find(rule => MatchesRule(subjectAttributes, rule));
    }

    private static bool MatchesRule(SubjectAttributes subjectAttributes, Rule rule)
    {
        List<bool> conditionEvaluations = EvaluateRuleCondition(subjectAttributes, rule.conditions);
        return !conditionEvaluations.Contains(false);
    }

    private static List<bool> EvaluateRuleCondition(SubjectAttributes subjectAttributes, List<Condition> ruleConditions)
    {
        return
            ruleConditions.ConvertAll(condition => EvaluateCondition(subjectAttributes, condition));
    }

    private static bool EvaluateCondition(SubjectAttributes subjectAttributes, Condition condition)
    {
        try
        {
            if (subjectAttributes.ContainsKey(condition.attribute) &&
                subjectAttributes.TryGetValue(condition.attribute, out EppoValue outVal))
            {
                var value = outVal!; // Assuming non-null for simplicity, handle nulls as necessary

                if (condition.operatorType == GTE)
                {
                    if (value.isNumeric() && condition.value.isNumeric())
                    {
                        return value.DoubleValue() >= condition.value.DoubleValue();
                    }

                    if (NuGetVersion.TryParse(value.StringValue(), out var valueSemver) &&
                        NuGetVersion.TryParse(condition.value.StringValue(), out var conditionSemver))
                    {
                        return valueSemver >= conditionSemver;
                    }

                    return false;
                }
                else if (condition.operatorType == GT)
                {
                    if (value.isNumeric() && condition.value.isNumeric())
                    {
                        return value.DoubleValue() > condition.value.DoubleValue();
                    }

                    if (NuGetVersion.TryParse(value.StringValue(), out var valueSemver) &&
                        NuGetVersion.TryParse(condition.value.StringValue(), out var conditionSemver))
                    {
                        return valueSemver > conditionSemver;
                    }

                    return false;
                }
                else if (condition.operatorType == LTE)
                {
                    if (value.isNumeric() && condition.value.isNumeric())
                    {
                        return value.DoubleValue() <= condition.value.DoubleValue();
                    }

                    if (NuGetVersion.TryParse(value.StringValue(), out var valueSemver) &&
                        NuGetVersion.TryParse(condition.value.StringValue(), out var conditionSemver))
                    {
                        return valueSemver <= conditionSemver;
                    }

                    return false;
                }
                else if (condition.operatorType == LT)
                {
                    if (value.isNumeric() && condition.value.isNumeric())
                    {
                        return value.DoubleValue() < condition.value.DoubleValue();
                    }

                    if (NuGetVersion.TryParse(value.StringValue(), out var valueSemver) &&
                        NuGetVersion.TryParse(condition.value.StringValue(), out var conditionSemver))
                    {
                        return valueSemver < conditionSemver;
                    }

                    return false;
                }
                else if (condition.operatorType == MATCHES)
                {
                    return Regex.Match(value.StringValue(), condition.value.StringValue(), RegexOptions.IgnoreCase).Success;
                }
                else if (condition.operatorType == ONE_OF)
                {
                    return Compare.IsOneOf(value.StringValue(), condition.value.ArrayValue());
                }
                else if (condition.operatorType == NOT_ONE_OF)
                {
                    return !Compare.IsOneOf(value.StringValue(), condition.value.ArrayValue());
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