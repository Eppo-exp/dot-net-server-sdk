using eppo_sdk.dto.bandit;
using eppo_sdk.validators;
using static NUnit.Framework.Assert;

namespace eppo_sdk_test.validators;

using DoubleDictionary = Dictionary<string, double>;
using StringDictionary = Dictionary<string, string>;

public class BanditEvaluatorTest
{
    BanditEvaluator banditEvaluator = new BanditEvaluator(10000);

    readonly List<NumericAttributeCoefficient> numCoeffs = new()
    {
        new NumericAttributeCoefficient("age", 2.0, 0.5),
        new NumericAttributeCoefficient("height", 1.5, 0.3),
    };

    readonly List<NumericAttributeCoefficient> negativeNumCoeffs = new()
    {
        new NumericAttributeCoefficient("age", -2.0, 0.5),
        new NumericAttributeCoefficient("height", -1.5, 0.3),
    };

    readonly List<CategoricalAttributeCoefficient> catCoeffs = new()
    {
        new CategoricalAttributeCoefficient(
            "color",
            0.2,
            new DoubleDictionary() { ["red"] = 1.0, ["blue"] = 0.5 }
        ),
        new CategoricalAttributeCoefficient(
            "size",
            0.3,
            new DoubleDictionary() { ["large"] = 2.0, ["small"] = 1.0 }
        ),
    };

    readonly List<CategoricalAttributeCoefficient> negativeCatCoeffs = new()
    {
        new CategoricalAttributeCoefficient(
            "color",
            0.2,
            new DoubleDictionary() { ["red"] = -1.0, ["blue"] = -0.5 }
        ),
        new CategoricalAttributeCoefficient(
            "size",
            0.3,
            new DoubleDictionary() { ["large"] = -2.0, ["small"] = -1.0 }
        ),
    };

    [Test]
    public void ShouldScoreNumericAttributes()
    {
        var subjectAttributes = new DoubleDictionary() { ["age"] = 30, ["height"] = 170 };
        var expectedScore = 30 * 2.0 + 170 * 1.5;
        var actualScore = BanditEvaluator.ScoreNumericAttributes(numCoeffs, subjectAttributes);
        That(actualScore, Is.EqualTo(expectedScore));
    }

    [Test]
    public void ShouldScoreNumericAttributesWithMissing()
    {
        var subjectAttributes = new DoubleDictionary() { ["age"] = 30 };
        var expectedScore = 30 * 2.0 + 0.3; // 0.3 is missing value for height.
        var actualScore = BanditEvaluator.ScoreNumericAttributes(numCoeffs, subjectAttributes);
        That(actualScore, Is.EqualTo(expectedScore));
    }

    [Test]
    public void ShouldScoreNumericAttributesWithAllMissing()
    {
        var subjectAttributes = new DoubleDictionary() { };
        var expectedScore = 0.5 + 0.3;
        var actualScore = BanditEvaluator.ScoreNumericAttributes(numCoeffs, subjectAttributes);
        That(actualScore, Is.EqualTo(expectedScore));
    }

    [Test]
    public void ShouldScoreNumericAttributeNoCoefficients()
    {
        var subjectAttributes = new DoubleDictionary() { ["age"] = 30, ["height"] = 170 };
        var expectedScore = 0.0; // No coefficients to apply

        var actualScore = BanditEvaluator.ScoreNumericAttributes(
            new List<NumericAttributeCoefficient>(),
            subjectAttributes
        );
        That(actualScore, Is.EqualTo(expectedScore));
    }

    [Test]
    public void ShouldScoreNumericAttributeNegativeCoefficients()
    {
        var subjectAttributes = new DoubleDictionary() { ["age"] = 30, ["height"] = 170 };
        var expectedScore = 30 * -2.0 + 170 * -1.5;

        var actualScore = BanditEvaluator.ScoreNumericAttributes(
            negativeNumCoeffs,
            subjectAttributes
        );
        That(actualScore, Is.EqualTo(expectedScore));
    }

    [Test]
    public void ShouldScoreCategoricalAttributes()
    {
        var subjectAttributes = new StringDictionary() { ["color"] = "red", ["size"] = "large" };
        var expectedScore = 1.0 + 2.0;

        var actualScore = BanditEvaluator.ScoreCategoricalAttributes(catCoeffs, subjectAttributes);
        That(actualScore, Is.EqualTo(expectedScore));
    }

    [Test]
    public void ShouldScoreCategoricalAttributesSomeMissing()
    {
        var subjectAttributes = new StringDictionary() { ["color"] = "red" };
        var expectedScore = 1.0 + 0.3;

        var actualScore = BanditEvaluator.ScoreCategoricalAttributes(catCoeffs, subjectAttributes);
        That(actualScore, Is.EqualTo(expectedScore));
    }

    [Test]
    public void ShouldScoreCategoricalAttributesAllMissing()
    {
        var subjectAttributes = new StringDictionary() { };
        var expectedScore = 0.2 + 0.3;

        var actualScore = BanditEvaluator.ScoreCategoricalAttributes(catCoeffs, subjectAttributes);
        That(actualScore, Is.EqualTo(expectedScore));
    }

    [Test]
    public void ShouldScoreCategoricalAttributesNoCoefficients()
    {
        var subjectAttributes = new StringDictionary() { ["color"] = "red", ["size"] = "large" };
        var expectedScore = 0;

        var actualScore = BanditEvaluator.ScoreCategoricalAttributes(
            new List<CategoricalAttributeCoefficient>(),
            subjectAttributes
        );
        That(actualScore, Is.EqualTo(expectedScore));
    }

    [Test]
    public void ShouldScoreCategoricalAttributesNegativeCoefficients()
    {
        var subjectAttributes = new StringDictionary() { ["color"] = "red", ["size"] = "large" };
        var expectedScore = -1.0 + -2.0;

        var actualScore = BanditEvaluator.ScoreCategoricalAttributes(
            negativeCatCoeffs,
            subjectAttributes
        );
        That(actualScore, Is.EqualTo(expectedScore));
    }

    [Test]
    public void ShouldWeighOneAction()
    {
        var scores = new Dictionary<string, double> { ["action"] = 87.0 };
        var expectedWeights = new Dictionary<string, double> { ["action"] = 1.0 };
        That(
            BanditEvaluator.WeighActions(
                scores,
                10 /* Gamma */
                ,
                0.1 /* min probability */
            ),
            Is.EquivalentTo(expectedWeights)
        );
    }

    [Test]
    public void ShouldWeighMultipleActionScoresToTheFloor()
    {
        // Large spread with a large gamma will move most actions below the min proability.
        var scores = new Dictionary<string, double>
        {
            ["action"] = 87.0,
            ["action2"] = 1.0,
            ["action3"] = 15.0,
            ["action4"] = 2.7,
            ["action5"] = 0.5,
        };

        var gamma = 10;
        var minProbability = 0.1;
        var expectedFloorValue = minProbability / scores.Count; // 0.02
        var expectedWinnerWeight = 1 - (expectedFloorValue * (scores.Count - 1));

        var expectedWeights = new Dictionary<string, double>
        {
            ["action"] = expectedWinnerWeight,
            ["action2"] = expectedFloorValue,
            ["action3"] = expectedFloorValue,
            ["action4"] = expectedFloorValue,
            ["action5"] = expectedFloorValue,
        };

        var weights = BanditEvaluator.WeighActions(scores, gamma, minProbability);

        AssertActionScoreDictsMatch(weights, expectedWeights);
    }

    [Test]
    public void ShouldWeighMultipleActionScores()
    {
        // Based on jersey numbers which isn't really a score value, but that's ok for our purposes.
        var scores = new Dictionary<string, double>
        {
            ["Ovechkin"] = 8.0,
            ["Crosby"] = 87.0,
            ["Lemieux"] = 66.0,
            ["Gretzky"] = 99.0,
            ["Lindros"] = 88.0,
        };

        // Low gamma to encourage small spread of weights.
        var gamma = 0.1;
        var minProbability = 0.1;

        var weights = BanditEvaluator.WeighActions(scores, gamma, minProbability);

        // The actual weights are kind of hand-wavy and black box if they're just pulled from the underlying calculation
        // What matters is that the weights add up to 1 and (for this dataset) that the action weights are in the same ranking as their scores.
        Multiple(() =>
        {
            That(weights.Select(aScore => aScore.Value).Sum(), Is.EqualTo(1));
            That(
                weights.OrderBy(w => w.Value).Select(w => w.Key),
                Is.EquivalentTo(
                    new List<string> { "Gretzky", "Lindros", "Crosby", "Lemieux", "Ovechkin" }
                )
            );
        });
    }

    [Test]
    public void ShouldWeighWithGamma()
    {
        var scores = new Dictionary<string, double> { ["action"] = 2.0, ["action2"] = 0.5 };

        var smallGamma = 1;
        var largeGamma = 10;
        var minProbability = 0.1;

        var smallGammaWeights = BanditEvaluator.WeighActions(scores, smallGamma, minProbability);
        var largeGammaWeights = BanditEvaluator.WeighActions(scores, largeGamma, minProbability);
        Multiple(() =>
        {
            // Winner shares more of their score with a smaller gamma
            That(smallGammaWeights["action"], Is.LessThan(largeGammaWeights["action"]));

            // Non-winners get bigger share of weight with a smaller gamma
            That(smallGammaWeights["action2"], Is.GreaterThan(largeGammaWeights["action2"]));
        });
    }

    [Test]
    public void ShouldEquallyWeighEvenField()
    {
        var scores = new Dictionary<string, double>
        {
            ["action1"] = 0.5,
            ["action2"] = 0.5,
            ["action3"] = 0.5,
            ["action4"] = 0.5,
        };
        var expectedWeights = new Dictionary<string, double>
        {
            ["action1"] = 1.0 / 4,
            ["action2"] = 1.0 / 4,
            ["action3"] = 1.0 / 4,
            ["action4"] = 1.0 / 4,
        };
        var gamma = 0.1;
        var minProbability = 0.1;

        var weights = BanditEvaluator.WeighActions(scores, gamma, minProbability);
        AssertActionScoreDictsMatch(weights, expectedWeights);
    }

    [Test]
    public void TestEvaluateBandit()
    {
        // Mock data

        const string flagKey = "test_flag";
        const string subjectKey = "test_subject";
        var subjectAttributes = new ContextAttributes(subjectKey)
        {
            { "age", 25.0 },
            { "location", "US" },
        };

        ContextAttributes[] actionContexts =
        {
            new("action1") { { "price", 10.0 }, { "category", "A" } },
            new("action2") { { "price", 20.0 }, { "category", "B" } },
        };

        var coefficients = new Dictionary<string, ActionCoefficients>()
        {
            {
                "action1",
                new ActionCoefficients("action1", 0.5)
                {
                    SubjectNumericCoefficients = new List<NumericAttributeCoefficient>()
                    {
                        new("age", 0.1, 0.0),
                    },
                    SubjectCategoricalCoefficients = new List<CategoricalAttributeCoefficient>()
                    {
                        new("location", 0.0, new DoubleDictionary() { { "US", 0.2 } }),
                    },
                    ActionNumericCoefficients = new List<NumericAttributeCoefficient>()
                    {
                        new("price", 0.05, 0.0),
                    },
                    ActionCategoricalCoefficients = new List<CategoricalAttributeCoefficient>()
                    {
                        new("category", 0.0, new DoubleDictionary() { { "A", 0.3 } }),
                    },
                }
            },
            {
                "action2",
                new ActionCoefficients("action2", 0.3)
                {
                    SubjectNumericCoefficients = new List<NumericAttributeCoefficient>()
                    {
                        new("age", 0.1, 0.0),
                    },
                    SubjectCategoricalCoefficients = new List<CategoricalAttributeCoefficient>()
                    {
                        new("location", 0.0, new DoubleDictionary() { { "US", 0.2 } }),
                    },
                    ActionNumericCoefficients = new List<NumericAttributeCoefficient>()
                    {
                        new("price", 0.05, 0.0),
                    },
                    ActionCategoricalCoefficients = new List<CategoricalAttributeCoefficient>()
                    {
                        new("category", 0.0, new DoubleDictionary() { { "B", 0.3 } }),
                    },
                }
            },
        };

        var banditModel = new ModelData
        {
            Gamma = 0.1,
            DefaultActionScore = 0.0,
            ActionProbabilityFloor = 0.1,
            Coefficients = coefficients,
        };

        var evaluator = new BanditEvaluator(10_000);

        // Evaluate bandit
        var evaluation = evaluator.EvaluateBandit(
            flagKey,
            subjectAttributes,
            actionContexts.ToDictionary(i => i.Key),
            banditModel
        );
        Multiple(() =>
        {
            That(evaluation, Is.Not.Null);

            That(evaluation!.FlagKey, Is.EqualTo(flagKey));
            That(evaluation.SubjectKey, Is.EqualTo(subjectKey));
            That(
                evaluation.SubjectAttributes.NumericAttributes,
                Is.EquivalentTo(subjectAttributes.AsAttributeSet().NumericAttributes)
            );
            That(
                evaluation.SubjectAttributes.CategoricalAttributes,
                Is.EquivalentTo(subjectAttributes.AsAttributeSet().CategoricalAttributes)
            );

            // Note: The test result here is different than in the python SDK for the same inputs because of the
            // mechanism used to shuffle the actions. Here, we use the same sharder as we would for non-test environments.
            // In Python, a pass-thru sharder is used, so the actions are shuffled into a differtent order.
            That(evaluation.ActionKey, Is.EqualTo("action2"));
            That(evaluation.Gamma, Is.EqualTo(banditModel.Gamma));
            That(evaluation.ActionScore, Is.EqualTo(4.3));
            That(Math.Round(evaluation.ActionWeight, 4), Is.EqualTo(0.5074));
        });
    }

    private static void AssertActionScoreDictsMatch(
        IDictionary<string, double> actual,
        IDictionary<string, double> expected
    )
    {
        Multiple(() =>
        {
            That(actual, Has.Count.EqualTo(expected.Count));
            That(actual, Is.EquivalentTo(expected));
        });
    }
}
