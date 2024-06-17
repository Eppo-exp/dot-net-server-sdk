using System.Security.Cryptography.X509Certificates;
using eppo_sdk.dto;
using eppo_sdk.dto.bandit;
using eppo_sdk.validators;

namespace eppo_sdk_test.validators;

using ActionScore = KeyValuePair<string, double>;
using StringDictionary = Dictionary<string, string>;
using DoubleDictionary = Dictionary<string, double>;

public class BanditEvaluatorTest
{
    BanditEvaluator banditEvaluator = new BanditEvaluator(10000);

    readonly List<NumericAttributeCoefficient> numCoeffs = new()
        {
            new NumericAttributeCoefficient("age", 2.0, 0.5),
            new NumericAttributeCoefficient("height", 1.5, 0.3)
        };

    readonly List<NumericAttributeCoefficient> negativeNumCoeffs = new()
        {
            new NumericAttributeCoefficient("age", -2.0, 0.5),
            new NumericAttributeCoefficient("height", -1.5, 0.3)
        };

    readonly List<CategoricalAttributeCoefficient> catCoeffs = new()
    {
        new CategoricalAttributeCoefficient("color", 0.2, new DoubleDictionary() {["red"] = 1.0, ["blue"] = 0.5}),
        new CategoricalAttributeCoefficient("size", 0.3, new DoubleDictionary() {["large"] = 2.0, ["small"] = 1.0}),
    };

    readonly List<CategoricalAttributeCoefficient> negativeCatCoeffs = new()
    {
        new CategoricalAttributeCoefficient("color", 0.2, new DoubleDictionary() {["red"] = -1.0, ["blue"] = -0.5}),
        new CategoricalAttributeCoefficient("size", 0.3, new DoubleDictionary() {["large"] = -2.0, ["small"] = -1.0}),
    };

    [Test]
    public void ShouldScoreNumericAttributes()
    {
        var subjectAttributes = new DoubleDictionary()
        {
            ["age"] = 30,
            ["height"] = 170
        };
        var expectedScore = 30 * 2.0 + 170 * 1.5;
        var actualScore = BanditEvaluator.ScoreNumericAttributes(numCoeffs, subjectAttributes);
        Assert.That(actualScore, Is.EqualTo(expectedScore));
    }

    [Test]
    public void ShouldScoreNumericAttributesWithMissing()
    {
        var subjectAttributes = new DoubleDictionary()
        {
            ["age"] = 30
        };
        var expectedScore = 30 * 2.0 + 0.3; // 0.3 is missing value for height.
        var actualScore = BanditEvaluator.ScoreNumericAttributes(numCoeffs, subjectAttributes);
        Assert.That(actualScore, Is.EqualTo(expectedScore));
    }

    [Test]
    public void ShouldScoreNumericAttributesWithAllMissing()
    {
        var subjectAttributes = new DoubleDictionary()
        {
        };
        var expectedScore = 0.5 + 0.3;
        var actualScore = BanditEvaluator.ScoreNumericAttributes(numCoeffs, subjectAttributes);
        Assert.That(actualScore, Is.EqualTo(expectedScore));
    }

    [Test]
    public void ShouldScoreNumericAttributeNoCoefficients()
    {
        var subjectAttributes = new DoubleDictionary()
        {
            ["age"] = 30,
            ["height"] = 170
        };
        var expectedScore = 0.0; // No coefficients to apply

        var actualScore = BanditEvaluator.ScoreNumericAttributes(new List<NumericAttributeCoefficient>(), subjectAttributes);
        Assert.That(actualScore, Is.EqualTo(expectedScore));
    }
    [Test]
    public void ShouldScoreNumericAttributeNegativeCoefficients()
    {
        var subjectAttributes = new DoubleDictionary()
        {
            ["age"] = 30,
            ["height"] = 170
        };
        var expectedScore = 30 * -2.0 + 170 * -1.5;

        var actualScore = BanditEvaluator.ScoreNumericAttributes(negativeNumCoeffs, subjectAttributes);
        Assert.That(actualScore, Is.EqualTo(expectedScore));
    }

    [Test]
    public void ShouldScoreCategoricalAttributes()
    {
        var subjectAttributes = new StringDictionary()
        {
            ["color"] = "red",
            ["size"] = "large"
        };
        var expectedScore = 1.0 + 2.0;

        var actualScore = BanditEvaluator.ScoreCategoricalAttributes(catCoeffs, subjectAttributes);
        Assert.That(actualScore, Is.EqualTo(expectedScore));
    }

    [Test]
    public void ShouldScoreCategoricalAttributesSomeMissing()
    {
        var subjectAttributes = new StringDictionary()
        {
            ["color"] = "red"
        };
        var expectedScore = 1.0 + 0.3;

        var actualScore = BanditEvaluator.ScoreCategoricalAttributes(catCoeffs, subjectAttributes);
        Assert.That(actualScore, Is.EqualTo(expectedScore));
    }

    [Test]
    public void ShouldScoreCategoricalAttributesAllMissing()
    {
        var subjectAttributes = new StringDictionary()
        {
        };
        var expectedScore = 0.2 + 0.3;

        var actualScore = BanditEvaluator.ScoreCategoricalAttributes(catCoeffs, subjectAttributes);
        Assert.That(actualScore, Is.EqualTo(expectedScore));
    }

    [Test]
    public void ShouldScoreCategoricalAttributesNoCoefficients()
    {
        var subjectAttributes = new StringDictionary()
        {
            ["color"] = "red",
            ["size"] = "large"
        };
        var expectedScore = 0;

        var actualScore = BanditEvaluator.ScoreCategoricalAttributes(new List<CategoricalAttributeCoefficient>(), subjectAttributes);
        Assert.That(actualScore, Is.EqualTo(expectedScore));
    }
    [Test]
    public void ShouldScoreCategoricalAttributesNegativeCoefficients()
    {
        var subjectAttributes = new StringDictionary()
        {
            ["color"] = "red",
            ["size"] = "large"
        };
        var expectedScore = -1.0 + -2.0;

        var actualScore = BanditEvaluator.ScoreCategoricalAttributes(negativeCatCoeffs, subjectAttributes);
        Assert.That(actualScore, Is.EqualTo(expectedScore));
    }

    [Test]
    public void ShouldWeighOneAction()
    {
        var scores = new List<ActionScore> {
           new("action", 87.0)
        };
        var expectedWeights = new List<ActionScore> {
           new("action", 1.0)
        };
        Assert.That(banditEvaluator.WeighActions(scores, 10 /* Gamma */, 0.1 /* min probability */), Is.EquivalentTo(expectedWeights));
    }

    [Test]
    public void ShouldWeighMultipleActionScoresToTheFloor()
    {
        // Large spread with a large gamma will move most actions below the min proability.
        var scores = new List<ActionScore> {
           new("action", 87.0),
           new("action2", 1.0),
           new("action3", 15.0),
           new("action4", 2.7),
           new("action5", 0.5),
        };

        var gamma = 10;
        var minProbability = 0.1;
        var expectedFloorValue = minProbability / scores.Count; // 0.02
        var expectedWinnerWeight = 1 - (expectedFloorValue * (scores.Count - 1));

        var expectedWeights = new List<ActionScore> {
           new("action", expectedWinnerWeight),
           new("action2", expectedFloorValue),
           new("action3", expectedFloorValue),
           new("action4", expectedFloorValue),
           new("action5", expectedFloorValue)
        };

        var weights = banditEvaluator.WeighActions(scores, gamma, minProbability);

        AssertActionScoreListsMatch(weights, expectedWeights);

    }
    [Test]
    public void ShouldWeighMultipleActionScores()
    {
        // Based on jersey numbers which isn't really a score value, but that's ok for our purposes.
        var scores = new List<ActionScore> {
           new("Ovechkin", 8.0),
           new("Crosby", 87.0),
           new("Lemieux", 66.0),
           new("Gretzky", 99.0),
           new("Lindros", 88.0)
        };

        // Low gamma to encourage small spread of weights.
        var gamma = 0.1;
        var minProbability = 0.1;


        var weights = banditEvaluator.WeighActions(scores, gamma, minProbability);

        // The actual weights are kind of hand-wavy and black box if they're just pulled from the underlying calculation
        // What matters is that the weights add up to 1 and (for this dataset) that the action weights are in the same ranking as their scores.
        Assert.Multiple(() =>
        {
            Assert.That(weights.Select(aScore => aScore.Value).Sum(), Is.EqualTo(1));
            Assert.That(weights.OrderBy(w => w.Value).Select(w => w.Key), Is.EquivalentTo(
                new List<string>
                    {
                    "Gretzky",
                    "Lindros",
                    "Crosby",
                    "Lemieux",
                    "Ovechkin"
                    }
            ));
        });
    }

    [Test]
    public void ShouldWeighWithGamma()
    {
        var scores = new List<ActionScore> {
           new("action", 2.0),
           new("action2", 0.5),
        };

        var smallGamma = 1;
        var largeGamma = 10;
        var minProbability = 0.1;

        var smallGammaWeights = banditEvaluator.WeighActions(scores, smallGamma, minProbability);
        var largeGammaWeights = banditEvaluator.WeighActions(scores, largeGamma, minProbability);
        Assert.Multiple(() =>
        {
            // Winner shares more of their score with a smaller gamma
            Assert.That(
                smallGammaWeights.Find(w => w.Key == "action").Value,
                Is.LessThan(largeGammaWeights.Find(w => w.Key == "action").Value));

            // Non-winners get bigger share of weight with a smaller gamma
            Assert.That(
                smallGammaWeights.Find(w => w.Key == "action2").Value,
                Is.GreaterThan(largeGammaWeights.Find(w => w.Key == "action2").Value));
        });
    }


    [Test]
    public void ShouldEquallyWeighEvenField()
    {
        var scores = new List<ActionScore> {
           new("action1", 0.5),
           new("action2", 0.5),
           new("action3", 0.5),
           new("action4", 0.5),
        };
        var expectedWeights = new List<ActionScore> {
           new("action1", 1.0/4),
           new("action2", 1.0/4),
           new("action3", 1.0/4),
           new("action4", 1.0/4)
        };
        var gamma = 0.1;
        var minProbability = 0.1;

        var weights = banditEvaluator.WeighActions(scores, gamma, minProbability);
        AssertActionScoreListsMatch(weights, expectedWeights);
    }

    [Test]
    public void TestEvaluateBandit()
    {
        // Mock data

        const string flagKey = "test_flag";
        const string subjectKey = "test_subject";
        var subjectAttributes = new ContextAttributes(subjectKey)
        {
             { "age", 25.0 } ,
             { "location", "US" }
        };

        ContextAttributes[] actionContexts = {
            new("action1") { { "price", 10.0 },{ "category", "A" } },
            new("action2") { { "price", 20.0 },{ "category", "B" } }
        };

        var coefficients = new Dictionary<string, ActionCoefficients>()
        {
            {
                "action1", new ActionCoefficients("action1", 0.5)
                {
                    SubjectNumericCoefficients = new List<NumericAttributeCoefficient>() { new("age", 0.1, 0.0) },
                    SubjectCategoricalCoefficients = new List<CategoricalAttributeCoefficient>() { new( "location",  0.0, new DoubleDictionary() { { "US", 0.2 } } )},
                    ActionNumericCoefficients = new List<NumericAttributeCoefficient>() { new( "price", 0.05,  0.0 )},
                    ActionCategoricalCoefficients = new List<CategoricalAttributeCoefficient>() { new( "category",  0.0, new DoubleDictionary(){ { "A", 0.3 } } )}
                }
            },
            {
                "action2", new ActionCoefficients("action2", 0.3)
                {
                    SubjectNumericCoefficients = new List<NumericAttributeCoefficient>() { new("age", 0.1, 0.0) },
                    SubjectCategoricalCoefficients = new List<CategoricalAttributeCoefficient>() { new( "location",  0.0, new DoubleDictionary() { { "US", 0.2 } } )},
                    ActionNumericCoefficients = new List<NumericAttributeCoefficient>() { new( "price", 0.05,  0.0 )},
                    ActionCategoricalCoefficients = new List<CategoricalAttributeCoefficient>() { new( "category",  0.0, new DoubleDictionary(){ { "A", 0.3 } } )}
                }
            }
        };

        var banditModel = new ModelData
        {
            Gamma = 0.1,
            DefaultActionScore = 0.0,
            ActionProbabilityFloor = 0.1,
            Coefficients = coefficients
        };

        var evaluator = new BanditEvaluator(10_000);

        // Evaluate bandit
        var evaluation = evaluator.EvaluateBandit(flagKey, subjectAttributes, actionContexts.ToDictionary(i => i.Key), banditModel);
        Assert.Multiple(() =>
        {
            Assert.That(evaluation, Is.Not.Null);
            // Assertions
            Assert.That(evaluation!.FlagKey, Is.EqualTo(flagKey));
            Assert.That(evaluation.SubjectKey, Is.EqualTo(subjectKey));
            Assert.That(evaluation.SubjectAttributes.NumericAttributes, Is.EquivalentTo(subjectAttributes.AsAttributeSet().NumericAttributes));
            Assert.That(evaluation.SubjectAttributes.CategoricalAttributes, Is.EquivalentTo(subjectAttributes.AsAttributeSet().CategoricalAttributes));

            // Note: The test result here is different than in the python SDK for the same inputs because of the 
            // mechanism used to shuffle the actions. Here, we use the same sharder as we would for non-test environments.
            // In Python, a pass-thru sharder is used, so the actions are shuffled into a differtent order.
            Assert.That(evaluation.ActionKey, Is.EqualTo("action2"));
            Assert.That(evaluation.Gamma, Is.EqualTo(banditModel.Gamma));
            Assert.That(evaluation.ActionScore, Is.EqualTo(4.0));
            Assert.That(Math.Round(evaluation.ActionWeight, 4), Is.EqualTo(0.4926).Within(4)); // Adjust precision for floating-point comparison
        });
    }

    private static void AssertActionScoreListsMatch(List<ActionScore> actual, List<ActionScore> expected, bool ignoreOrder = true)
    {
        Assert.That(actual.Count, Is.EqualTo(expected.Count));
        if (ignoreOrder)
        {
            var expectedWeightsDict = new Dictionary<string, ActionScore>(expected.Select(aScore => KeyValuePair.Create(aScore.Key, aScore)));
            var actualWeightsDict = new Dictionary<string, ActionScore>(actual.Select(aScore => KeyValuePair.Create(aScore.Key, aScore)));
            Assert.That(actualWeightsDict, Is.EquivalentTo(expectedWeightsDict));
        }
        else
        {
            Assert.That(actual, Is.EquivalentTo(expected));
        }
    }
}
