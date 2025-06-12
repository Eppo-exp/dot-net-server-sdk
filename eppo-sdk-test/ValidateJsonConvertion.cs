using eppo_sdk.dto;
using eppo_sdk.dto.bandit;
using Newtonsoft.Json;
using static NUnit.Framework.Assert;

namespace eppo_sdk_test;
public class ValidateJsonConvertion
{
    [Test]
    public void ShouldConvertCondition()
    {
        const string json = @"{
            'value': ['iOS','Android'],
            'operator': 'ONE_OF',
            'attribute': 'device'
        }";
        var condition = JsonConvert.DeserializeObject<Condition>(json);
        Multiple(() =>
        {
            That(condition, Is.Not.Null);
            That(condition?.Operator, Is.EqualTo(OperatorType.ONE_OF));
            That(condition?.Attribute, Is.EqualTo("device"));
            That(condition?.ArrayValue().Count, Is.EqualTo(2));
        });
    }
    [Test]
    public void ShouldConvertSplits()
    {
        const string json = @"[
            {
              'variationKey': 'on',
              'shards': [
                {
                  'salt': 'some-salt',
                  'ranges': [
                    {
                      'start': 0,
                      'end': 2500
                    },
                    {
                      'start': 2500,
                      'end': 9999
                    }
                  ]
                },
                {
                  'salt': 'some-salt-two',
                  'ranges': [
                    {
                      'start': 9999,
                      'end': 10000
                    }
                  ]
                }
              ],
              'extraLogging': {
                'foo': 'bar',
                'bar': 'baz'
              }
            }
          ]";
        var splits = JsonConvert.DeserializeObject<List<Split>>(json);
        Assert.Multiple(() =>
        {
            That(splits, Is.Not.Null);
            That(splits?.Count, Is.EqualTo(1));
            That(splits?[0].VariationKey, Is.EqualTo("on"));
            That(splits?[0].Shards.Count, Is.EqualTo(2));
            That(splits?[0].Shards[0].salt, Is.EqualTo("some-salt"));
            That(splits?[0].Shards[0].ranges.Count, Is.EqualTo(2));
            That(splits?[0].Shards[0].ranges[0].start, Is.EqualTo(0));
            That(splits?[0].Shards[0].ranges[0].end, Is.EqualTo(2500));
            That(splits?[0].Shards[0].ranges[1].start, Is.EqualTo(2500));
            That(splits?[0].Shards[0].ranges[1].end, Is.EqualTo(9999));

            That(splits?[0].Shards[1].salt, Is.EqualTo("some-salt-two"));

            That(splits?[0].ExtraLogging, Is.EquivalentTo(new Dictionary<string, string>
            {
                ["foo"] = "bar",
                ["bar"] = "baz"
            }));
        });
    }

    [Test]
    public void ShouldConvertRules()
    {
        const string json = @"[
        {
          'allocationKey': 'allocation-experiment-4',
          'conditions': [{'value': ['iOS','Android'],'operator': 'ONE_OF','attribute': 'device'},
            {'value': 1,'operator': 'GT','attribute': 'version'}
          ]},
        {
          'allocationKey': 'allocation-experiment-4',
          'conditions': [
            {'value': ['China'],'operator': 'NOT_ONE_OF','attribute': 'country'}
          ]},
        {
          'allocationKey': 'allocation-experiment-4',
          'conditions': [{'value': '.*geteppo.com','operator': 'MATCHES','attribute': 'email'}
          ]}
      ]";

        var rules = JsonConvert.DeserializeObject<List<Rule>>(json);
        Assert.That(rules?.Count, Is.EqualTo(3));
        Assert.That(rules[0].conditions[0].Operator, Is.EqualTo(OperatorType.ONE_OF));
        Assert.That(rules[0].conditions[0].ArrayValue(), Is.EqualTo(new List<string>
        {
            "iOS",
            "Android"
        }));
        Assert.That(rules[0].conditions[0].Attribute, Is.EqualTo("device"));

        Assert.That(rules[1].conditions[0].Operator, Is.EqualTo(OperatorType.NOT_ONE_OF));
        Assert.That(rules[1].conditions[0].Attribute, Is.EqualTo("country"));
    }

    [Test]
    public void ShouldConvertVariations()
    {
        const string json = @"{
      'on': {
        'key': 'on',
        'value': true
      },
      'off': {
        'key': 'off',
        'value': false
      }
    }";
        Variation expectedVariation = new Variation("on", true);

        var variations = JsonConvert.DeserializeObject<Dictionary<string, Variation>>(json);
        Multiple(() =>
        {
            That(variations, Is.Not.Null);
            That(variations?.Count, Is.EqualTo(2));
            That(variations?.ContainsKey("on") ?? false);
            Variation? on = null;
            That(variations?.TryGetValue("on", out on), Is.True);
            That(on, Is.Not.Null);
            That(on?.BoolValue(), Is.True);

        });
    }

    [Test]
    public void ShouldConvertAllocations()
    {
        const string json = /*lang=json*/ @"[
        {
          'key': 'on-for-age-50+',
          'rules': [
            {
              'conditions': [
                {
                  'attribute': 'age',
                  'operator': 'GTE',
                  'value': 50
                }
              ]
            }
          ],
          'splits': [
            {
              'variationKey': 'on',
              'shards': [
                {
                  'salt': 'some-salt',
                  'ranges': [
                    {
                      'start': 0,
                      'end': 10000
                    }
                  ]
                }
              ]
            }
          ],
          'doLog': false,
          'startAt': '2022-10-31T09:00:00.594Z',
          'endAt': '2050-10-31T09:00:00.594Z',
        },
        {
          'key': 'off-for-all',
          'rules': [],
          'splits': [
            {
              'variationKey': 'off',
              'shards': []
            }
          ],
          'doLog': true,
          'startAt': '2022-10-31T09:00:00.594Z',
          'endAt': '2050-10-31T09:00:00.594Z',
        }
      ]
    ";

        var allocations = JsonConvert.DeserializeObject<List<Allocation>>(json);
        Assert.Multiple(() =>
        {
            That(allocations, Is.Not.Null);
            That(allocations?.Count, Is.EqualTo(2));
            That(allocations?[0].Key, Is.EqualTo("on-for-age-50+"));
            That(allocations?[0].DoLog, Is.EqualTo(false));
            That(allocations?[0].StartAt, Is.EqualTo(DateTime.Parse("2022-10-31T09:00:00.594Z").ToUniversalTime()));
            That(allocations?[0].EndAt, Is.EqualTo(DateTime.Parse("2050-10-31T09:00:00.594Z").ToUniversalTime()));

            That(allocations?[0].Rules.Count, Is.EqualTo(1));
            That(allocations?[0].Splits.Count, Is.EqualTo(1));

            That(allocations?[1].Key, Is.EqualTo("off-for-all"));
            That(allocations?[1].DoLog, Is.EqualTo(true));
            That(allocations?[1].StartAt, Is.EqualTo(DateTime.Parse("2022-10-31T09:00:00.594Z").ToUniversalTime()));
            That(allocations?[1].EndAt, Is.EqualTo(DateTime.Parse("2050-10-31T09:00:00.594Z").ToUniversalTime()));

            That(allocations?[1].Rules.Count, Is.EqualTo(0));
            That(allocations?[1].Splits.Count, Is.EqualTo(1));
        });

    }



    [Test]
    public void ShouldParseBandit()
    {
        const string banditJson = @"{
    ""banditKey"": ""banner_bandit"",
    ""modelName"": ""falcon"",
    ""updatedAt"": ""2023-09-13T04:52:06.462Z"",
    ""modelVersion"": ""v123"",
    ""modelData"": {
      ""gamma"": 1.0,
      ""defaultActionScore"": 0.0,
      ""actionProbabilityFloor"": 0.0,
      ""coefficients"": {
        ""nike"": {
          ""actionKey"": ""nike"",
          ""intercept"": 1.0,
          ""actionNumericCoefficients"": [
            {
              ""attributeKey"": ""brand_affinity"",
              ""coefficient"": 1.0,
              ""missingValueCoefficient"": -0.1
            }
          ],
          ""actionCategoricalCoefficients"": [
            {
              ""attributeKey"": ""loyalty_tier"",
              ""valueCoefficients"": {
                  ""gold"": 4.5,
                  ""silver"":  3.2,
                  ""bronze"":  1.9
              },
              ""missingValueCoefficient"": 0.0
            }
          ],
          ""subjectNumericCoefficients"": [
            {
              ""attributeKey"": ""account_age"",
              ""coefficient"": 0.3,
              ""missingValueCoefficient"": 0.0
            }
          ],
          ""subjectCategoricalCoefficients"": [
            {
              ""attributeKey"": ""gender_identity"",
              ""valueCoefficients"": {
                ""female"": 0.5,
                ""male"": -0.5
              },
              ""missingValueCoefficient"": 2.3
            }
          ]
        },
      }
    }
  }";

        Bandit? bandit = JsonConvert.DeserializeObject<Bandit>(banditJson);

        Assert.Multiple(() =>
        {
            That(bandit, Is.Not.Null);
            var notNullBandit = bandit!;

            That(notNullBandit.BanditKey, Is.EqualTo("banner_bandit"));
            That(notNullBandit.ModelName, Is.EqualTo("falcon"));
            That(notNullBandit.ModelVersion, Is.EqualTo("v123"));
            That(notNullBandit.UpdatedAt, Is.EqualTo(DateTime.Parse("2023-09-13T04:52:06.462Z").ToUniversalTime()));

            That(notNullBandit.ModelData, Is.Not.Null);
            var model = notNullBandit.ModelData;

            That(model.Gamma, Is.EqualTo(1.0));
            That(model.DefaultActionScore, Is.EqualTo(0.0));
            That(model.ActionProbabilityFloor, Is.EqualTo(0.0));

            That(model.Coefficients, Is.Not.Null);
            That(model.Coefficients, Has.Count.EqualTo(1));

            That(model.Coefficients.TryGetValue("nike", out var actionCoefficients), Is.True);
            That(actionCoefficients, Is.Not.Null);

            var ac = actionCoefficients!;
            That(ac.ActionKey, Is.EqualTo("nike"));
            That(ac.Intercept, Is.EqualTo(1.0));


            That(ac.ActionCategoricalCoefficients, Is.Not.Null);
            That(ac.ActionNumericCoefficients, Is.Not.Null);
            That(ac.SubjectCategoricalCoefficients, Is.Not.Null);
            That(ac.SubjectNumericCoefficients, Is.Not.Null);

            var loyaltyDict = new Dictionary<string, double>
            {
                ["gold"] = 4.5,
                ["silver"] = 3.2,
                ["bronze"] = 1.9
            };
            var loyaltyAttrCoef = new CategoricalAttributeCoefficient("loyalty_tier", 0.0, loyaltyDict);
            AssertCategoricalCoefficients(ac.ActionCategoricalCoefficients, new() { loyaltyAttrCoef });

            var genderDict = new Dictionary<string, double>
            {
                ["male"] = -0.5,
                ["female"] = 0.5
            };
            var genderAttrCoef = new CategoricalAttributeCoefficient("gender_identity", 2.3, genderDict);
            AssertCategoricalCoefficients(ac.SubjectCategoricalCoefficients, new() { genderAttrCoef });

            That(ac.ActionNumericCoefficients, Is.EquivalentTo(new List<NumericAttributeCoefficient>() { new("brand_affinity", 1, -0.1) }));
            That(ac.SubjectNumericCoefficients, Is.EquivalentTo(new List<NumericAttributeCoefficient>() { new("account_age", 0.3, 0) }));
        });
    }



    [Test]
    public void ShouldParsePartialBandit()
    {
        const string banditJson = @"{

      ""banditKey"": ""cold_start_bandit"",
      ""modelName"": ""falcon"",
      ""updatedAt"": ""2023-09-13T04:52:06.462Z"",
      ""modelVersion"": ""cold start"",
      ""modelData"": {
        ""gamma"": 1.0,
        ""defaultActionScore"": 0.0,
        ""actionProbabilityFloor"": 0.0,
        ""coefficients"": {}
      }


  }";

        Bandit? bandit = JsonConvert.DeserializeObject<Bandit>(banditJson);

        Assert.Multiple(() =>
        {
            That(bandit, Is.Not.Null);
            var notNullBandit = bandit!;

            That(notNullBandit.BanditKey, Is.EqualTo("cold_start_bandit"));
            That(notNullBandit.ModelName, Is.EqualTo("falcon"));
            That(notNullBandit.ModelVersion, Is.EqualTo("cold start"));
            That(notNullBandit.UpdatedAt, Is.EqualTo(DateTime.Parse("2023-09-13T04:52:06.462Z").ToUniversalTime()));

            That(notNullBandit.ModelData, Is.Not.Null);
            var model = notNullBandit.ModelData;

            That(model.Gamma, Is.EqualTo(1.0));
            That(model.DefaultActionScore, Is.EqualTo(0.0));
            That(model.ActionProbabilityFloor, Is.EqualTo(0.0));

            That(model.Coefficients, Is.Not.Null);
            That(model.Coefficients, Has.Count.EqualTo(0));

        });
    }

    private static void AssertCategoricalCoefficients(IReadOnlyList<CategoricalAttributeCoefficient> actual, List<CategoricalAttributeCoefficient> expected)
    {
        // Can't use EquivalentTo here b/c the nested collection won't match (EquivalentTo appears to use EqualTo on nested values)
        That(actual, Has.Count.EqualTo(expected.Count));

        for (var i = 0; i < actual.Count; ++i)
        {
            That(actual[i].AttributeKey, Is.EqualTo(expected[i].AttributeKey));
            That(actual[i].MissingValueCoefficient, Is.EqualTo(expected[i].MissingValueCoefficient));
            That(actual[i].ValueCoefficients, Is.EquivalentTo(expected[i].ValueCoefficients));
        }
    }

}
