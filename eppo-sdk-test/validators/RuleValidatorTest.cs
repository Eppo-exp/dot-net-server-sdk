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
        var subjectAttributes = new Subject();
        AddNameToSubjectAttribute(subjectAttributes);

        Assert.That(ruleWithEmptyConditions, Is.EqualTo(RuleValidator.FindMatchingRule(subjectAttributes, rules)));
    }

    [Test]
    public void ShouldMatchAnyRuleWithEmptyRules()
    {
        var rules = new List<Rule>();
        var subjectAttributes = new Subject();
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

        var subjectAttributes = new Subject();
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

        var subjectAttributes = new Subject
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

        var subjectAttributes = new Subject { { "price", "abcd" } };

        Assert.That(RuleValidator.FindMatchingRule(subjectAttributes, rules), Is.Null);
    }

    [Test]
    public void ShouldMatchAnyRuleWithRegexCondition()
    {
        var rules = new List<Rule>();
        Rule rule = CreateRule(new List<Condition>());
        AddRegexConditionToRule(rule);
        rules.Add(rule);

        var subjectAttributes = new Subject { { "match", "abcd" } };

        Assert.That(rule, Is.EqualTo(RuleValidator.FindMatchingRule(subjectAttributes, rules)));
    }

    [Test]
    public void ShouldNotMatchAnyRuleWithRegexConditionIsUnmatched()
    {
        var rules = new List<Rule>();
        var rule = CreateRule(new List<Condition>());
        AddRegexConditionToRule(rule);
        rules.Add(rule);

        var subjectAttributes = new Subject { { "match", "123" } };

        Assert.That(RuleValidator.FindMatchingRule(subjectAttributes, rules), Is.Null);
    }

    [Test]
    public void ShouldMatchAnyRuleWithOneOfRule()
    {
        var rules = new List<Rule>();
        var rule = CreateRule(new List<Condition>());
        AddOneOfCondition(rule);
        rules.Add(rule);

        var subjectAttributes = new Subject { { "oneOf", "value2" } };

        Assert.That(rule, Is.EqualTo(RuleValidator.FindMatchingRule(subjectAttributes, rules)));
    }

    [Test]
    public void ShouldNotMatchAnyRuleWithOneOfRule()
    {
        var rules = new List<Rule>();
        var rule = CreateRule(new List<Condition>());
        AddOneOfCondition(rule);
        rules.Add(rule);

        var subjectAttributes = new Subject { { "oneOf", "value3" } };

        Assert.That(RuleValidator.FindMatchingRule(subjectAttributes, rules), Is.Null);
    }

    [Test]
    public void ShouldMatchAnyRuleWithNotOneOfRule()
    {
        var rules = new List<Rule>();
        var rule = CreateRule(new List<Condition>());
        AddNotOneOfCondition(rule);
        rules.Add(rule);

        var subjectAttributes = new Subject { { "oneOf", "value3" } };

        Assert.That(RuleValidator.FindMatchingRule(subjectAttributes, rules), Is.EqualTo(rule));
    }

    [Test]
    public void ShouldNotMatchAnyRuleWithNotOneOfRuleNotPassed()
    {
        var rules = new List<Rule>();
        var rule = CreateRule(new List<Condition>());
        AddNotOneOfCondition(rule);
        rules.Add(rule);

        var subjectAttributes = new Subject { { "oneOf", "value1" } };

        Assert.That(RuleValidator.FindMatchingRule(subjectAttributes, rules), Is.Null);
    }

    [Test]
    public void ShouldMatchRuleIsNullTrueNullType()
    {
        var rules = new List<Rule>();
        var rule = CreateRule(new List<Condition>());
        AddIsNullCondition(rule, true);
        rules.Add(rule);

        var subjectAttributes = new Subject { { "isnull", null } };

        Assert.That(rule, Is.EqualTo(RuleValidator.FindMatchingRule(subjectAttributes, rules)));
    }

    [Test]
    public void ShouldMatchRuleIsNullTrue()
    {
        var rules = new List<Rule>();
        var rule = CreateRule(new List<Condition>());
        AddIsNullCondition(rule, true);
        rules.Add(rule);

        var subjectAttributes = new Subject { { "isnull", null } };

        Assert.That(rule, Is.EqualTo(RuleValidator.FindMatchingRule(subjectAttributes, rules)));
    }


    [Test]
    public void ShouldMatchRuleIsNullNoAttribute()
    {
        var rules = new List<Rule>();
        var rule = CreateRule(new List<Condition>());
        AddIsNullCondition(rule, true);
        rules.Add(rule);

        var subjectAttributes = new Subject { };

        Assert.That(rule, Is.EqualTo(RuleValidator.FindMatchingRule(subjectAttributes, rules)));
    }

    [Test]
    public void ShouldMatchRuleIsNullFalse()
    {
        var rules = new List<Rule>();
        var rule = CreateRule(new List<Condition>());
        AddIsNullCondition(rule, false);
        rules.Add(rule);

        var subjectAttributes = new Subject { { "isnull", "not null" } };

        Assert.That(rule, Is.EqualTo(RuleValidator.FindMatchingRule(subjectAttributes, rules)));
    }

    [Test]
    public void ShouldNotMatchRuleIsNullTrue()
    {
        var rules = new List<Rule>();
        var rule = CreateRule(new List<Condition>());
        AddIsNullCondition(rule, true);
        rules.Add(rule);

        var subjectAttributes = new Subject { { "isnull", "not null" } };

        Assert.That(RuleValidator.FindMatchingRule(subjectAttributes, rules), Is.Null);
    }

    [Test]
    public void ShouldNotMatchRuleIsNullFalse()
    {
        var rules = new List<Rule>();
        var rule = CreateRule(new List<Condition>());
        AddIsNullCondition(rule, false);
        rules.Add(rule);

        var subjectAttributes = new Subject { { "isnull", null } };

        Assert.That(RuleValidator.FindMatchingRule(subjectAttributes, rules), Is.Null);
    }
    private static void AddIsNullCondition(Rule rule, Boolean value)
    {
        rule.conditions.Add(new("isnull", OperatorType.IS_NULL, value));
    }

    private const string SubjectKey = "subjectKey";
    private const int TotalShards = 10;

    private List<Split> nonMatchingSplits;
    private List<Split> matchingSplits;
    private List<Split> musicSplits;
    private Rule rockAndRollLegendRule = new(new List<Condition>
        {
            new("age",OperatorType.GTE,40),
            new("occupation",OperatorType.MATCHES, "musician"),
            new("albumCount", OperatorType.GTE, 50)
        });
    private Subject subject;
    private Variation matchVariation = new("match", "foo");

    [SetUp]
    public void Setup()
    {
        subject = new()
        {
            ["age"] = 42,
            ["albumCount"] = 57,
            ["occupation"] = "musician"
        };
        var allShards = new List<Shard>();
        var shardRangeAll = new List<ShardRange>()
        {
            new ShardRange(0, TotalShards)
        };

        allShards.Add(new Shard("na", shardRangeAll));

        matchingSplits = new List<Split>()
        {
            new Split("match", allShards, null)
        };

        musicSplits = new List<Split>()
        {
            new Split("music", new List<Shard>()
            {
                new Shard("na", new List<ShardRange>()
                {
                    new ShardRange(2, 5)
                })
            }, null)
        };

        nonMatchingSplits = new List<Split>()
        {
            new Split("match", new List<Shard>()
            {
                new Shard("na", new List<ShardRange>()
                {
                    new ShardRange(0, 4),
                    new ShardRange(5, 9)
                }),
                new Shard("cl", new List<ShardRange>()
                {

                })
            }, null)
        };
    }

    [Test]
    public void NoMatchingShards_ReturnsFalse()
    {
        Assert.That(RuleValidator.MatchesAllShards(nonMatchingSplits[0].shards, SubjectKey, TotalShards), Is.False);
    }

    [Test]
    public void SomeMatchingShards_ReturnsFalse()
    {
        var allShards = matchingSplits[0].shards;
        allShards.AddRange(nonMatchingSplits[0].shards);
        Assert.That(RuleValidator.MatchesAllShards(allShards, SubjectKey, TotalShards), Is.False);
    }

    [Test]
    public void MatchesShards_ReturnsTrue()
    {
        Assert.That(RuleValidator.MatchesAllShards(matchingSplits[0].shards, SubjectKey, TotalShards), Is.True);
    }

    [Test]
    public void FlagEvaluation_ReturnsMatchingVariation()
    {
        var allocations = new List<Allocation>() {
            new(
                "rock",
                new List<Rule>() { rockAndRollLegendRule },
                musicSplits,
                false, null, null)
        };
        var variations = new Dictionary<string, Variation>() {
            { "music", new("music", "rockandroll") },
            { "football", new("football", "football") },
            { "space", new("space", "space") }
        };

        var bigFlag = new Flag(
            "HallOfFame",
            true,
            allocations,
            EppoValueType.STRING,
            variations,
            TotalShards);

        var result = RuleValidator.EvaluateFlag(bigFlag, SubjectKey, subject);

        Assert.Multiple(() =>
            {
                Assert.NotNull(result);
                Assert.That(result.Variation.Key, Is.EqualTo("music"));
                Assert.That(result.Variation.Value, Is.EqualTo("rockandroll"));
            }
        );
    }

    [Test]
    public void DisabledFlag_ReturnsNull()
    {
        var flag = new Flag("disabled", false, new List<Allocation>(), EppoValueType.BOOLEAN, new Dictionary<string, Variation>(), TotalShards);
        Assert.Null(RuleValidator.EvaluateFlag(flag, SubjectKey, new Subject()));
    }

    [Test]
    public void FlagWithInactiveAllocations_ReturnsNull()
    {

        var now = DateTimeOffset.Now;
        var overAlloc = new Allocation("over", new List<Rule>(), matchingSplits, false, null, endAt: now.Subtract( new TimeSpan( 0,0,10000)).DateTime );

        var futureAlloc = new Allocation("hasntStarted", new List<Rule>(), matchingSplits, false, startAt: now.Add( new TimeSpan( 0,0,6000)).DateTime, null);

        var flag = new Flag(
            "inactive_allocs",
            true,
            new List<Allocation>() { overAlloc, futureAlloc },
            EppoValueType.BOOLEAN,
            new Dictionary<string, Variation>() { { matchVariation.Key, matchVariation } },
            TotalShards);

        Assert.Null(RuleValidator.EvaluateFlag(flag, SubjectKey, subject));
    }

    [Test]
    public void FlagWithoutAllocations_ReturnsNull()
    {
        var flag = new Flag("no_allocs", true, new List<Allocation>(), EppoValueType.BOOLEAN, new Dictionary<string, Variation>(), TotalShards);
        Assert.Null(RuleValidator.EvaluateFlag(flag, SubjectKey, subject));
    }

    [Test]
    public void MatchesVariationWithoutRules_ReturnsMatchingVariation()
    {
        var allocation1 = new Allocation("alloc1", new List<Rule>(), matchingSplits, false, null, null);
        var basicVariation = new Variation("foo", "bar");
        var flag = new Flag(
            "matches",
            true,
            new List<Allocation>() { allocation1 },
            EppoValueType.STRING,
            new Dictionary<string, Variation>() { { "match", basicVariation } },
            TotalShards);

        var result = RuleValidator.EvaluateFlag(flag, SubjectKey, subject);

        Assert.NotNull(result);
        Assert.That(result.Variation.Value, Is.EqualTo("bar"));
    }

    private static void AddOneOfCondition(Rule rule)
    {
        rule.conditions.Add(new("oneOf", OperatorType.ONE_OF, new List<string>
            {
                "value1",
                "value2"
            }));
    }

    private static void AddNotOneOfCondition(Rule rule)
    {
        rule.conditions.Add(new Condition("oneOf", OperatorType.NOT_ONE_OF, new List<string>
            {
                "value1",
                "value2"
            }));

    }

    private static void AddRegexConditionToRule(Rule rule)
    {
        var condition = new Condition("match", OperatorType.MATCHES, "[a-z]+");
        rule.conditions.Add(condition);
    }

    private static void AddPriceToSubjectAttribute(Subject subjectAttributes)
    {
        subjectAttributes.Add("price", "30");
    }

    private static void AddNumericConditionToRule(Rule rule)
    {
        rule.conditions.Add(new Condition("price", OperatorType.GTE, 10));

        rule.conditions.Add(new Condition("price", OperatorType.LTE, 20));
    }

    private static void AddSemVerConditionToRule(Rule rule)
    {
        rule.conditions.Add(new Condition("appVersion", OperatorType.GTE, "1.2.3"));
        rule.conditions.Add(new Condition("appVersion", OperatorType.LTE, "2.2.0"));
    }

    private static void AddNameToSubjectAttribute(Subject subjectAttributes)
    {
        subjectAttributes.Add("name", "test");
    }

    private static Rule CreateRule(List<Condition> conditions)
    {
        return new Rule(conditions);
    }
}
