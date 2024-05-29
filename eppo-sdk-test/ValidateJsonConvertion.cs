using eppo_sdk.dto;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
      That(condition.operatorType, Is.EqualTo(OperatorType.ONE_OF));
      That(condition.attribute, Is.EqualTo("device"));
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
    Assert.That(rules.Count, Is.EqualTo(3));
    Assert.That(rules[0].conditions[0].operatorType, Is.EqualTo(OperatorType.ONE_OF));
    Assert.That(rules[0].conditions[0].value.ArrayValue(), Is.EqualTo(new List<string>
        {
            "iOS",
            "Android"
        }));
    Assert.That(rules[0].conditions[0].attribute, Is.EqualTo("device"));

    Assert.That(rules[1].conditions[0].operatorType, Is.EqualTo(OperatorType.NOT_ONE_OF));
    Assert.That(rules[1].conditions[0].attribute, Is.EqualTo("country"));
  }

  [Test]
  public void ShouldSupportJsonValueType()
  {
    const string json = @"{
            'background': 'black',
            'color': 'yellow',
            'logo': '_assets/newlogo.png'
        }";
    var value = new EppoValue(json, EppoValueType.JSON);
    var expected = new JObject
    {
      ["background"] = "black",
      ["color"] = "yellow",
      ["logo"] = "_assets/newlogo.png"
    };
    Multiple(() =>
    {
      That(value.Value, Is.EqualTo(json));
      That(value.Type, Is.EqualTo(EppoValueType.JSON));
      That(value.JsonValue(), Is.EqualTo(expected));

    });
  }
  [Test]
  public void ShouldParseJsonValueType()
  {
    const string json = @"{
            'shardRange': {
              'start': 0,
              'end': 10000
            },
            'typedValue': ""{'background': 'black','color': 'yellow','logo': '_assets/newlogo.png'}""
        }";
    var variation = JsonConvert.DeserializeObject<Variation>(json);
    var value = variation.typedValue;
    var expected = new JObject
    {
      ["background"] = "black",
      ["color"] = "yellow",
      ["logo"] = "_assets/newlogo.png"
    };
    Multiple(() =>
    {
      That(variation, Is.Not.Null);
      That(value, Is.Not.Null);
      // When parsing a payload, the JSON object is actually encoded as a string.
      That(value.Type, Is.EqualTo(EppoValueType.STRING));
      That(value.JsonValue(), Is.EqualTo(expected));
    });
  }

}