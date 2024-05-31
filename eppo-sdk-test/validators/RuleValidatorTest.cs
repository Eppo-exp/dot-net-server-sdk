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
            { "price",  15 },
            { "appVersion", "1.15.0" }
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

        var subjectAttributes = new SubjectAttributes { { "price", "abcd" } };

        Assert.That(RuleValidator.FindMatchingRule(subjectAttributes, rules), Is.Null);
    }

    [Test]
    public void ShouldMatchAnyRuleWithRegexCondition()
    {
        var rules = new List<Rule>();
        Rule rule = CreateRule(new List<Condition>());
        AddRegexConditionToRule(rule);
        rules.Add(rule);

        var subjectAttributes = new SubjectAttributes { { "match",  "abcd"} };

        Assert.That(rule, Is.EqualTo(RuleValidator.FindMatchingRule(subjectAttributes, rules)));
    }

    [Test]
    public void ShouldNotMatchAnyRuleWithRegexConditionIsUnmatched()
    {
        var rules = new List<Rule>();
        var rule = CreateRule(new List<Condition>());
        AddRegexConditionToRule(rule);
        rules.Add(rule);

        var subjectAttributes = new SubjectAttributes { { "match",  "123" } };

        Assert.That(RuleValidator.FindMatchingRule(subjectAttributes, rules), Is.Null);
    }

    [Test]
    public void ShouldMatchAnyRuleWithOneOfRule()
    {
        var rules = new List<Rule>();
        var rule = CreateRule(new List<Condition>());
        AddOneOfCondition(rule);
        rules.Add(rule);

        var subjectAttributes = new SubjectAttributes { { "oneOf", "value2" } };

        Assert.That(rule, Is.EqualTo(RuleValidator.FindMatchingRule(subjectAttributes, rules)));
    }

    [Test]
    public void ShouldNotMatchAnyRuleWithOneOfRule()
    {
        var rules = new List<Rule>();
        var rule = CreateRule(new List<Condition>());
        AddOneOfCondition(rule);
        rules.Add(rule);

        var subjectAttributes = new SubjectAttributes { { "oneOf",  "value3"} };

        Assert.That(RuleValidator.FindMatchingRule(subjectAttributes, rules), Is.Null);
    }

    [Test]
    public void ShouldMatchAnyRuleWithNotOneOfRule()
    {
        var rules = new List<Rule>();
        var rule = CreateRule(new List<Condition>());
        AddNotOneOfCondition(rule);
        rules.Add(rule);

        var subjectAttributes = new SubjectAttributes { { "oneOf", "value3" } };

        Assert.That(rule, Is.EqualTo(RuleValidator.FindMatchingRule(subjectAttributes, rules)));
    }

    [Test]
    public void ShouldNotMatchAnyRuleWithNotOneOfRuleNotPassed()
    {
        var rules = new List<Rule>();
        var rule = CreateRule(new List<Condition>());
        AddNotOneOfCondition(rule);
        rules.Add(rule);

        var subjectAttributes = new SubjectAttributes { { "oneOf",  "value1" } };

        Assert.That(RuleValidator.FindMatchingRule(subjectAttributes, rules), Is.Null);
    }

    [Test]
    public void ShouldMatchRuleIsNullTrueNullType()
    {
        var rules = new List<Rule>();
        var rule = CreateRule(new List<Condition>());
        AddIsNullCondition(rule, true);
        rules.Add(rule);

        var subjectAttributes = new SubjectAttributes { { "isnull", null } };

        Assert.That(rule, Is.EqualTo(RuleValidator.FindMatchingRule(subjectAttributes, rules)));
    }

    [Test]
    public void ShouldMatchRuleIsNullTrue()
    {
        var rules = new List<Rule>();
        var rule = CreateRule(new List<Condition>());
        AddIsNullCondition(rule, true);
        rules.Add(rule);

        var subjectAttributes = new SubjectAttributes { { "isnull", null } };

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

        var subjectAttributes = new SubjectAttributes { { "isnull", "not null" } };

        Assert.That(rule, Is.EqualTo(RuleValidator.FindMatchingRule(subjectAttributes, rules)));
    }

    [Test]
    public void ShouldNotMatchRuleIsNullTrue()
    {
        var rules = new List<Rule>();
        var rule = CreateRule(new List<Condition>());
        AddIsNullCondition(rule, true);
        rules.Add(rule);

        var subjectAttributes = new SubjectAttributes { { "isnull", "not null" } };

        Assert.That(RuleValidator.FindMatchingRule(subjectAttributes, rules), Is.Null);
    }

    [Test]
    public void ShouldNotMatchRuleIsNullFalse()
    {
        var rules = new List<Rule>();
        var rule = CreateRule(new List<Condition>());
        AddIsNullCondition(rule, false);
        rules.Add(rule);

        var subjectAttributes = new SubjectAttributes { { "isnull", null } };

        Assert.That(RuleValidator.FindMatchingRule(subjectAttributes, rules), Is.Null);
    }
    private static void AddIsNullCondition(Rule rule, Boolean value)
    {
        rule.conditions.Add(new Condition
        {
            Value = value,
            Attribute = "isnull",
            Operator = OperatorType.IS_NULL
        });
    }

     private const string SubjectKey = "subjectKey";
    private const int TotalShards = 10;

    // Assuming you have these classes defined elsewhere (adapt them to your implementation)
    private List<Shard> nonMatchingShards;
    private List<Shard> matchingSplits;
    private Rule rockAndRollLegendRule;
    private Variation musicVariation;
    private Subject subject;
    private Variation matchVariation;

    [SetUp]
    public void Setup()
    {
        // Initialize your test data here (nonMatchingShards, matchingSplits, etc.)
    }

    [Test]
    public void NoMatchingShards_ReturnsFalse()
    {
        Assert.False(RuleValidator.MatchesAllShards(nonMatchingShards.ToArray(), SubjectKey, TotalShards));
    }

    [Test]
    public void SomeMatchingShards_ReturnsFalse()
    {
        var allShards = matchingSplits.Concat(nonMatchingShards).ToList();
        Assert.False(RuleValidator.MatchesAllShards(allShards.ToArray(), SubjectKey, TotalShards));
    }

    [Test]
    public void MatchesShards_ReturnsTrue()
    {
        Assert.True(RuleValidator.MatchesAllShards(matchingSplits.ToArray(), SubjectKey, TotalShards));
    }

    [Test]
    public void FlagEvaluation_ReturnsMatchingVariation()
    {
        var allocations = new List<Allocation>() {
            new Allocation(
                "rock",
                new List<Rule>() { rockAndRollLegendRule },
                musicSplits,
                false)
        };
        var variations = new Dictionary<string, Variation>() {
            { "music", musicVariation }
        };

        var bigFlag = new Flag(
            "HallOfFame",
            true,
            allocations,
            VariationType.String,
            variations,
            TotalShards);

        var result = RuleValidator.EvaluateFlag(bigFlag, SubjectKey, subject);

        Assert.NotNull(result);
        Assert.AreEqual(musicVariation.Key, result.Variation.Key);
        Assert.AreEqual(musicVariation.Value, result.Variation.Value);
    }

    [Test]
    public void DisabledFlag_ReturnsNull()
    {
        var flag = new Flag("disabled", false, new List<Allocation>(), VariationType.Boolean, new Dictionary<string, Variation>(), TotalShards);
        Assert.Null(RuleValidator.EvaluateFlag(flag, SubjectKey, new Dictionary<string, object>()));
    }

    [Test]
    public void FlagWithInactiveAllocations_ReturnsNull()
    {
        var now = DateTime.UtcNow.ToUnixTimeSeconds();
        var overAlloc = new Allocation("over", new List<Rule>(), matchingSplits, false, endAt: now - 10000);
        var futureAlloc = new Allocation("hasntStarted", new List<Rule>(), matchingSplits, false, startAt: now + 60000);

        var flag = new Flag(
            "inactive_allocs",
            true,
            new List<Allocation>() { overAlloc, futureAlloc },
            VariationType.Boolean,
            new Dictionary<string, Variation>() { { matchVariation.Key, matchVariation } },
            TotalShards);

        Assert.Null(RuleValidator.EvaluateFlag(flag, SubjectKey, subject));
    }

    [Test]
    public void FlagWithoutAllocations_ReturnsNull()
    {
        var flag = new Flag("no_allocs", true, new List<Allocation>(), VariationType.Boolean, new Dictionary<string, Variation>(), TotalShards);
        Assert.Null(RuleValidator.EvaluateFlag(flag, SubjectKey, subject));
    }

    [Test]
    public void MatchesVariationWithoutRules_ReturnsMatchingVariation()
    {
        var allocation1 = new Allocation("alloc1", new List<Rule>(), matchingSplits, false);
        var basicVariation = new Variation("foo", "bar");
        var flag = new Flag(
            "matches",
            true,
            new List<Allocation>() { allocation1 },
            VariationType.String,
            new Dictionary<string, Variation>() { { "match", basicVariation } },
            TotalShards);

        var result = RuleValidator.EvaluateFlag(flag, SubjectKey, subject);

        Assert.NotNull(result);
        Assert.That(result.variation.value, Is.EqualTo("bar"));
    }




    private static void AddOneOfCondition(Rule rule)
    {
        rule.conditions.Add(new Condition
        {
            Value =new List<string>
            {
                "value1",
                "value2"
            },
            Attribute = "oneOf",
            Operator = OperatorType.ONE_OF
        });
    }

    private static void AddNotOneOfCondition(Rule rule)
    {
        rule.conditions.Add(new Condition
        {
            Value = new List<string>
            {
                "value1",
                "value2"
            },
            Attribute = "oneOf",
            Operator = OperatorType.NOT_ONE_OF
        });
    }

    private static void AddRegexConditionToRule(Rule rule)
    {
        var condition = new Condition
        {
            Value = "[a-z]+",
            Attribute = "match",
            Operator = OperatorType.MATCHES
        };
        rule.conditions.Add(condition);
    }

    private static void AddPriceToSubjectAttribute(SubjectAttributes subjectAttributes)
    {
        subjectAttributes.Add("price", "30");
    }

    private static void AddNumericConditionToRule(Rule rule)
    {
        rule.conditions.Add(new Condition
        {
            Value = 10,
            Attribute = "price",
            Operator = OperatorType.GTE
        });

        rule.conditions.Add(new Condition
        {
            Value = 20,
            Attribute = "price",
            Operator = OperatorType.LTE
        });
    }

    private static void AddSemVerConditionToRule(Rule rule)
    {
        rule.conditions.Add(new Condition
        {
            Value = "1.2.3",
            Attribute = "appVersion",
            Operator = OperatorType.GTE
        });

        rule.conditions.Add(new Condition
        {
            Value = "2.2.0", 
            Attribute = "appVersion",
            Operator = OperatorType.LTE
        });
    }

    private static void AddNameToSubjectAttribute(SubjectAttributes subjectAttributes)
    {
        subjectAttributes.Add("name", "test");
    }

    private static Rule CreateRule(List<Condition> conditions)
    {
        return new Rule(conditions);
    }
}