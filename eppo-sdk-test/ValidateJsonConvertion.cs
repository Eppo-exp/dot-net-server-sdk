using eppo_sdk.dto;
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
      That(splits?[0].variationKey, Is.EqualTo("on"));
      That(splits?[0].shards.Count, Is.EqualTo(2));
      That(splits?[0].shards[0].salt, Is.EqualTo("some-salt"));
      That(splits?[0].shards[0].ranges.Count, Is.EqualTo(2));
      That(splits?[0].shards[0].ranges[0].start, Is.EqualTo(0));
      That(splits?[0].shards[0].ranges[0].end, Is.EqualTo(2500));
      That(splits?[0].shards[0].ranges[1].start, Is.EqualTo(2500));
      That(splits?[0].shards[0].ranges[1].end, Is.EqualTo(9999));

      That(splits?[0].shards[1].salt, Is.EqualTo("some-salt-two"));

      That(splits?[0].extraLogging, Is.EquivalentTo(new Dictionary<string, string>
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
          'startAt': 5000,
          'endAt': 2147483647
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
          'startAt': 5000,
          'endAt': 2147483647
        }
      ]
    ";

    var allocations = JsonConvert.DeserializeObject<List<Allocation>>(json);
    Assert.Multiple(() =>
    {
      That(allocations, Is.Not.Null);
      That(allocations?.Count, Is.EqualTo(2));
      That(allocations?[0].key, Is.EqualTo("on-for-age-50+"));
      That(allocations?[0].doLog, Is.EqualTo(false));
      That(allocations?[0].startAt, Is.EqualTo(5000));
      That(allocations?[0].endAt, Is.EqualTo(Int32.MaxValue));

      That(allocations?[0].rules.Count, Is.EqualTo(1));
      That(allocations?[0].splits.Count, Is.EqualTo(1));

      That(allocations?[1].key, Is.EqualTo("off-for-all"));
      That(allocations?[1].doLog, Is.EqualTo(true));
      That(allocations?[1].startAt, Is.EqualTo(5000));
      That(allocations?[1].endAt, Is.EqualTo(Int32.MaxValue));

      That(allocations?[1].rules.Count, Is.EqualTo(0));
      That(allocations?[1].splits.Count, Is.EqualTo(1));
    });

  }
}