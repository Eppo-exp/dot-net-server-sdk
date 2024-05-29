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

        var subjectAttributes = new SubjectAttributes
        {
            { "price",  EppoValue.Number("15") },
            { "appVersion",  EppoValue.String("1.15.0") }
        };

        Assert.That(rule, Is.EqualTo(RuleValidator.FindMatchingRule(subjectAttributes, rules)));
    }

    [Test]
    public void ShouldNotMatchAnyRuleWhenThrowInvalidSubjectAttribute()
    {
        var rules = new List<Rule>();
        var rule = CreateRule(new List<Condition>());
        AddNumericConditionToRule(rule);
        rules.Add(rule);

        var subjectAttributes = new SubjectAttributes { { "price", EppoValue.String("abcd") } };

        Assert.That(RuleValidator.FindMatchingRule(subjectAttributes, rules), Is.Null);
    }

    [Test]
    public void ShouldMatchAnyRuleWithRegexCondition()
    {
        var rules = new List<Rule>();
        Rule rule = CreateRule(new List<Condition>());
        AddRegexConditionToRule(rule);
        rules.Add(rule);

        var subjectAttributes = new SubjectAttributes { { "match",  EppoValue.String("abcd")} };

        Assert.That(rule, Is.EqualTo(RuleValidator.FindMatchingRule(subjectAttributes, rules)));
    }

    [Test]
    public void ShouldNotMatchAnyRuleWithRegexConditionIsUnmatched()
    {
        var rules = new List<Rule>();
        var rule = CreateRule(new List<Condition>());
        AddRegexConditionToRule(rule);
        rules.Add(rule);

        var subjectAttributes = new SubjectAttributes { { "match",  EppoValue.String("123") } };

        Assert.That(RuleValidator.FindMatchingRule(subjectAttributes, rules), Is.Null);
    }

    [Test]
    public void ShouldMatchAnyRuleWithOneOfRule()
    {
        var rules = new List<Rule>();
        var rule = CreateRule(new List<Condition>());
        AddOneOfCondition(rule);
        rules.Add(rule);

        var subjectAttributes = new SubjectAttributes { { "oneOf", EppoValue.String("value2") } };

        Assert.That(rule, Is.EqualTo(RuleValidator.FindMatchingRule(subjectAttributes, rules)));
    }

    [Test]
    public void ShouldNotMatchAnyRuleWithOneOfRule()
    {
        var rules = new List<Rule>();
        var rule = CreateRule(new List<Condition>());
        AddOneOfCondition(rule);
        rules.Add(rule);

        var subjectAttributes = new SubjectAttributes { { "oneOf",  EppoValue.String("value3")} };

        Assert.That(RuleValidator.FindMatchingRule(subjectAttributes, rules), Is.Null);
    }

    [Test]
    public void ShouldMatchAnyRuleWithNotOneOfRule()
    {
        var rules = new List<Rule>();
        var rule = CreateRule(new List<Condition>());
        AddNotOneOfCondition(rule);
        rules.Add(rule);

        var subjectAttributes = new SubjectAttributes { { "oneOf", EppoValue.String("value3") } };

        Assert.That(rule, Is.EqualTo(RuleValidator.FindMatchingRule(subjectAttributes, rules)));
    }

    [Test]
    public void ShouldNotMatchAnyRuleWithNotOneOfRuleNotPassed()
    {
        var rules = new List<Rule>();
        var rule = CreateRule(new List<Condition>());
        AddNotOneOfCondition(rule);
        rules.Add(rule);

        var subjectAttributes = new SubjectAttributes { { "oneOf",  EppoValue.String("value1") } };

        Assert.That(RuleValidator.FindMatchingRule(subjectAttributes, rules), Is.Null);
    }

    [Test]
    public void ShouldMatchRuleIsNullTrueNullType()
    {
        var rules = new List<Rule>();
        var rule = CreateRule(new List<Condition>());
        AddIsNullCondition(rule, true);
        rules.Add(rule);

        var subjectAttributes = new SubjectAttributes { { "isnull", new EppoValue() } };

        Assert.That(rule, Is.EqualTo(RuleValidator.FindMatchingRule(subjectAttributes, rules)));
    }

    [Test]
    public void ShouldMatchRuleIsNullTrue()
    {
        var rules = new List<Rule>();
        var rule = CreateRule(new List<Condition>());
        AddIsNullCondition(rule, true);
        rules.Add(rule);

        var subjectAttributes = new SubjectAttributes { { "isnull", EppoValue.String(null) } };

        Assert.That(rule, Is.EqualTo(RuleValidator.FindMatchingRule(subjectAttributes, rules)));
    }


    [Test]
    public void ShouldMatchRuleIsNullNoAttribute()
    {
        var rules = new List<Rule>();
        var rule = CreateRule(new List<Condition>());
        AddIsNullCondition(rule, true);
        rules.Add(rule);

        var subjectAttributes = new SubjectAttributes {  };

        Assert.That(rule, Is.EqualTo(RuleValidator.FindMatchingRule(subjectAttributes, rules)));
    }

    [Test]
    public void ShouldMatchRuleIsNullFalse()
    {
        var rules = new List<Rule>();
        var rule = CreateRule(new List<Condition>());
        AddIsNullCondition(rule, false);
        rules.Add(rule);

        var subjectAttributes = new SubjectAttributes { { "isnull", EppoValue.String("not null") } };

        Assert.That(rule, Is.EqualTo(RuleValidator.FindMatchingRule(subjectAttributes, rules)));
    }

    [Test]
    public void ShouldNotMatchRuleIsNullTrue()
    {
        var rules = new List<Rule>();
        var rule = CreateRule(new List<Condition>());
        AddIsNullCondition(rule, true);
        rules.Add(rule);

        var subjectAttributes = new SubjectAttributes { { "isnull", EppoValue.String("not null") } };

        Assert.That(RuleValidator.FindMatchingRule(subjectAttributes, rules), Is.Null);
    }

    [Test]
    public void ShouldNotMatchRuleIsNullFalse()
    {
        var rules = new List<Rule>();
        var rule = CreateRule(new List<Condition>());
        AddIsNullCondition(rule, false);
        rules.Add(rule);

        var subjectAttributes = new SubjectAttributes { { "isnull", new EppoValue() } };

        Assert.That(RuleValidator.FindMatchingRule(subjectAttributes, rules), Is.Null);
    }
    private static void AddIsNullCondition(Rule rule, Boolean value)
    {
        rule.conditions.Add(new Condition
        {
            value = EppoValue.Bool(value),
            attribute = "isnull",
            operatorType = OperatorType.IS_NULL
        });
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
            value = EppoValue.String("[a-z]+"),
            attribute = "match",
            operatorType = OperatorType.MATCHES
        };
        rule.conditions.Add(condition);
    }

    private static void AddPriceToSubjectAttribute(SubjectAttributes subjectAttributes)
    {
        subjectAttributes.Add("price", EppoValue.String("30"));
    }

    private static void AddNumericConditionToRule(Rule rule)
    {
        rule.conditions.Add(new Condition
        {
            value = EppoValue.Number("10"),
            attribute = "price",
            operatorType = OperatorType.GTE
        });

        rule.conditions.Add(new Condition
        {
            value = EppoValue.Number("20"),
            attribute = "price",
            operatorType = OperatorType.LTE
        });
    }

    private static void AddSemVerConditionToRule(Rule rule)
    {
        rule.conditions.Add(new Condition
        {
            value = EppoValue.String("1.2.3"),
            attribute = "appVersion",
            operatorType = OperatorType.GTE
        });

        rule.conditions.Add(new Condition
        {
            value = EppoValue.String("2.2.0"), 
            attribute = "appVersion",
            operatorType = OperatorType.LTE
        });
    }

    private static void AddNameToSubjectAttribute(SubjectAttributes subjectAttributes)
    {
        subjectAttributes.Add("name", EppoValue.String("test"));
    }

    private static Rule CreateRule(List<Condition> conditions)
    {
        return new Rule
        {
            conditions = conditions
        };
    }
}