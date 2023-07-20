using System.Text.RegularExpressions;
using eppo_sdk.dto;
using static eppo_sdk.dto.OperatorType;

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
        if (subjectAttributes.ContainsKey(condition.attribute))
        {
            subjectAttributes.TryGetValue(condition.attribute, out EppoValue outVal);
            var value = outVal!;
            Dictionary<OperatorType, Func<EppoValue, EppoValue, bool>> validationFunctions = new()
            {
                { GTE, (a, b) => a.LongValue() >= b.LongValue() },
                { GT, (a, b) => a.LongValue() > b.LongValue() },
                { LTE, (a, b) => a.LongValue() <= b.LongValue() },
                { LT, (a, b) => a.LongValue() < b.LongValue() },
                {
                    MATCHES, (a, b) =>
                        Regex.Match(a.StringValue(), b.StringValue(), RegexOptions.IgnoreCase).Success
                },
                {
                    ONE_OF, (a, b) => Compare.IsOneOf(value.StringValue(), condition.value.ArrayValue())
                },
                {
                    NOT_ONE_OF, (a, b) => !Compare.IsOneOf(value.StringValue(), condition.value.ArrayValue())
                }
            };

            try
            {
                validationFunctions.TryGetValue(condition.operatorType, out Func<EppoValue, EppoValue, bool> funcType);
                return funcType!(value, condition.value);
            }
            catch (Exception)
            {
                return false;
            }
        }

        return false;
    }
}

internal class Compare
{
    public static bool IsOneOf(string a, List<string> arrayValues)
    {
        return arrayValues.ConvertAll(v => v.ToLower()).IndexOf(a.ToLower()) >= 0;
    }
}