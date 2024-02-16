using eppo_sdk.dto;
using eppo_sdk.validators;

namespace eppo_sdk_test.validators;

public class RuleValidatorTest
{
    [Test]
    public void ShouldMatchAndyRuleWithEmptyCondition()
    {
        var ruleWithEmptyConditions = CreateRule(new List<Condition>());

        var rules = new List<Rule> { ruleWithEmptyConditions };
        var subjectAttributes = new SubjectAttributes();
        AddNameToSubjectAttribute(subjectAttributes);

        Assert.That(ruleWithEmptyConditions, Is.EqualTo(RuleValidator.FindMatchingRule(subjectAttributes, rules)));
    }

    [Test]
    public void ShouldMatchAnyRuleWithEmptyRules()
    {
        var rules = new List<Rule>();
        var subjectAttributes = new SubjectAttributes();
        AddNameToSubjectAttribute(subjectAttributes);

        Assert.That(RuleValidator.FindMatchingRule(subjectAttributes, rules), Is.Null);
    }

    [Test]
    public void ShouldMatchAnyRuleWhenNoRuleMatches()
    {
        var rules = new List<Rule>();
        var rule = CreateRule(new List<Condition>());
        AddNumericConditionToRule(rule);
        rules.Add(rule);

        var subjectAttributes = new SubjectAttributes();
        AddPriceToSubjectAttribute(subjectAttributes);

        Assert.That(RuleValidator.FindMatchingRule(subjectAttributes, rules), Is.Null);
    }

    [Test]
    public void ShouldMatchAnyRuleWhenRuleMatches()
    {
        var rules = new List<Rule>();
        var rule = CreateRule(new List<Condition>());
        AddNumericConditionToRule(rule);
        AddSemVerConditionToRule(rule);
        rules.Add(rule);

        var subjectAttributes = new SubjectAttributes();
        subjectAttributes.Add("price", new EppoValue("15", EppoValueType.NUMBER));
        subjectAttributes.Add("appVersion", new EppoValue("1.15.0", EppoValueType.STRING));

        Assert.That(rule, Is.EqualTo(RuleValidator.FindMatchingRule(subjectAttributes, rules)));
    }

    [Test]
    public void ShouldNotMatchAnyRuleWhenThrowInvalidSubjectAttribute()
    {
        var rules = new List<Rule>();
        var rule = CreateRule(new List<Condition>());
        AddNumericConditionToRule(rule);
        rules.Add(rule);

        var subjectAttributes = new SubjectAttributes { { "price", new EppoValue("abcd", EppoValueType.STRING) } };

        Assert.That(RuleValidator.FindMatchingRule(subjectAttributes, rules), Is.Null);
    }

    [Test]
    public void ShouldMatchAnyRuleWithRegexCondition()
    {
        var rules = new List<Rule>();
        Rule rule = CreateRule(new List<Condition>());
        AddRegexConditionToRule(rule);
        rules.Add(rule);

        var subjectAttributes = new SubjectAttributes { { "match", new EppoValue("abcd", EppoValueType.STRING) } };

        Assert.That(rule, Is.EqualTo(RuleValidator.FindMatchingRule(subjectAttributes, rules)));
    }

    [Test]
    public void ShouldNotMatchAnyRuleWithRegexConditionIsUnmatched()
    {
        var rules = new List<Rule>();
        var rule = CreateRule(new List<Condition>());
        AddRegexConditionToRule(rule);
        rules.Add(rule);

        var subjectAttributes = new SubjectAttributes { { "match", new EppoValue("123", EppoValueType.STRING) } };

        Assert.That(RuleValidator.FindMatchingRule(subjectAttributes, rules), Is.Null);
    }

    [Test]
    public void ShouldMatchAnyRuleWithOneOfRule()
    {
        var rules = new List<Rule>();
        var rule = CreateRule(new List<Condition>());
        AddOneOfCondition(rule);
        rules.Add(rule);

        var subjectAttributes = new SubjectAttributes { { "oneOf", new EppoValue("value2", EppoValueType.STRING) } };

        Assert.That(rule, Is.EqualTo(RuleValidator.FindMatchingRule(subjectAttributes, rules)));
    }

    [Test]
    public void ShouldNotMatchAnyRuleWithOneOfRule()
    {
        var rules = new List<Rule>();
        var rule = CreateRule(new List<Condition>());
        AddOneOfCondition(rule);
        rules.Add(rule);

        var subjectAttributes = new SubjectAttributes { { "oneOf", new EppoValue("value3", EppoValueType.STRING) } };

        Assert.That(RuleValidator.FindMatchingRule(subjectAttributes, rules), Is.Null);
    }

    [Test]
    public void ShouldMatchAnyRuleWithNotOneOfRule()
    {
        var rules = new List<Rule>();
        var rule = CreateRule(new List<Condition>());
        AddNotOneOfCondition(rule);
        rules.Add(rule);

        var subjectAttributes = new SubjectAttributes { { "oneOf", new EppoValue("value3", EppoValueType.STRING) } };

        Assert.That(rule, Is.EqualTo(RuleValidator.FindMatchingRule(subjectAttributes, rules)));
    }

    [Test]
    public void ShouldNotMatchAnyRuleWithNotOneOfRuleNotPassed()
    {
        var rules = new List<Rule>();
        var rule = CreateRule(new List<Condition>());
        AddNotOneOfCondition(rule);
        rules.Add(rule);

        var subjectAttributes = new SubjectAttributes { { "oneOf", new EppoValue("value1", EppoValueType.STRING) } };

        Assert.That(RuleValidator.FindMatchingRule(subjectAttributes, rules), Is.Null);
    }

    private static void AddOneOfCondition(Rule rule)
    {
        rule.conditions.Add(new Condition
        {
            value = new EppoValue(new List<string>
            {
                "value1",
                "value2"
            }),
            attribute = "oneOf",
            operatorType = OperatorType.ONE_OF
        });
    }

    private static void AddNotOneOfCondition(Rule rule)
    {
        rule.conditions.Add(new Condition
        {
            value = new EppoValue(new List<string>
            {
                "value1",
                "value2"
            }),
            attribute = "oneOf",
            operatorType = OperatorType.NOT_ONE_OF
        });
    }

    private static void AddRegexConditionToRule(Rule rule)
    {
        var condition = new Condition
        {
            value = new EppoValue("[a-z]+", EppoValueType.STRING),
            attribute = "match",
            operatorType = OperatorType.MATCHES
        };
        rule.conditions.Add(condition);
    }

    private static void AddPriceToSubjectAttribute(SubjectAttributes subjectAttributes)
    {
        subjectAttributes.Add("price", new EppoValue("30", EppoValueType.STRING));
    }

    private static void AddNumericConditionToRule(Rule rule)
    {
        rule.conditions.Add(new Condition
        {
            value = new EppoValue("10", EppoValueType.NUMBER),
            attribute = "price",
            operatorType = OperatorType.GTE
        });

        rule.conditions.Add(new Condition
        {
            value = new EppoValue("20", EppoValueType.NUMBER),
            attribute = "price",
            operatorType = OperatorType.LTE
        });
    }

    private static void AddSemVerConditionToRule(Rule rule)
    {
        rule.conditions.Add(new Condition
        {
            value = new EppoValue("1.0.0", EppoValueType.STRING),
            attribute = "appVersion",
            operatorType = OperatorType.GTE
        });

        rule.conditions.Add(new Condition
        {
            value = new EppoValue("2.2.0", EppoValueType.STRING),
            attribute = "appVersion",
            operatorType = OperatorType.LTE
        });
    }

    private static void AddNameToSubjectAttribute(SubjectAttributes subjectAttributes)
    {
        subjectAttributes.Add("name", new EppoValue("test", EppoValueType.STRING));
    }

    private static Rule CreateRule(List<Condition> conditions)
    {
        return new Rule
        {
            conditions = conditions
        };
    }
}