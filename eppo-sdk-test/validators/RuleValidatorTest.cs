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
            { "price", new EppoValue("15", EppoValueType.NUMBER) },
            { "appVersion", new EppoValue("1.15.0", EppoValueType.STRING) }
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
        Assert.False(RuleEvaluator.MatchesAllShards(nonMatchingShards.ToArray(), SubjectKey, TotalShards));
    }

    [Test]
    public void SomeMatchingShards_ReturnsFalse()
    {
        var allShards = matchingSplits.Concat(nonMatchingShards).ToList();
        Assert.False(RuleEvaluator.MatchesAllShards(allShards.ToArray(), SubjectKey, TotalShards));
    }

    [Test]
    public void MatchesShards_ReturnsTrue()
    {
        Assert.True(RuleEvaluator.MatchesAllShards(matchingSplits.ToArray(), SubjectKey, TotalShards));
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

        var result = RuleEvaluator.EvaluateFlag(bigFlag, SubjectKey, subject);

        Assert.NotNull(result);
        Assert.AreEqual(musicVariation.Key, result.Variation.Key);
        Assert.AreEqual(musicVariation.Value, result.Variation.Value);
    }

    [Test]
    public void DisabledFlag_ReturnsNull()
    {
        var flag = new Flag("disabled", false, new List<Allocation>(), VariationType.Boolean, new Dictionary<string, Variation>(), TotalShards);
        Assert.Null(RuleEvaluator.EvaluateFlag(flag, SubjectKey, new Dictionary<string, object>()));
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

        Assert.Null(RuleEvaluator.EvaluateFlag(flag, SubjectKey, subject));
    }

    [Test]
    public void FlagWithoutAllocations_ReturnsNull()
    {
        var flag = new Flag("no_allocs", true, new List<Allocation>(), VariationType.Boolean, new Dictionary<string, Variation>(), TotalShards);
        Assert.Null(RuleEvaluator.EvaluateFlag(flag, SubjectKey, subject));
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

        var result = RuleEvaluator.EvaluateFlag(flag, SubjectKey, subject);

        Assert.NotNull(result);
        Assert.That(result.variation.value is.EqualTo("bar"));

    private static void AddOneOfCondition(Rule rule)
    {
        rule.conditions.Add(new Condition("oneOf", OperatorType.ONE_OF, new EppoValue(new List<string>
            {
                "value1",
                "value2"
            })));
    }

    private static void AddNotOneOfCondition(Rule rule)
    {
        rule.conditions.Add(new Condition("oneOf", OperatorType.NOT_ONE_OF, new EppoValue(new List<string>
            {
                "value1",
                "value2"
            })));
    }

    private static void AddRegexConditionToRule(Rule rule)
    {
         rule.conditions.Add(new Condition("match", OperatorType.MATCHES, EppoValue.String("[a-z]+")));
    }

    private static void AddPriceToSubjectAttribute(SubjectAttributes subjectAttributes)
    {
        subjectAttributes.Add("price", new EppoValue("30", EppoValueType.STRING));
    }

    private static void AddNumericConditionToRule(Rule rule)
    {
        rule.conditions.Add(new Condition("price", OperatorType.GTE, new EppoValue("10", EppoValueType.NUMBER)));
        rule.conditions.Add(new Condition("price", OperatorType.LTE, new EppoValue("20", EppoValueType.NUMBER)));
    }

    private static void AddSemVerConditionToRule(Rule rule)
    {
        rule.conditions.Add(new Condition("appVersion", OperatorType.GTE, new EppoValue("1.2.3", EppoValueType.NUMBER)));
        rule.conditions.Add(new Condition("appVersion", OperatorType.LTE, new EppoValue("2.2.0", EppoValueType.NUMBER)));
    }

    private static void AddNameToSubjectAttribute(SubjectAttributes subjectAttributes)
    {
        subjectAttributes.Add("name", new EppoValue("test", EppoValueType.STRING));
    }

    private static Rule CreateRule(List<Condition> conditions)
    {
        return new Rule(conditions);
    }
}