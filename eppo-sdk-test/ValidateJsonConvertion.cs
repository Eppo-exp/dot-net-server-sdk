using System.Text.Json.Nodes;
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
            That(condition.operatorType, Is.EqualTo(OperatorType.ONE_OF));
            That(condition.attribute, Is.EqualTo("device"));
        });
    }

    [Test]
    public void ShouldConvertArrayOfStringType()
    {
        const string json = @"{
            'typedValue': ['iOS','Android']
        }";
        HasEppoValue? eValue = JsonConvert.DeserializeObject<HasEppoValue>(json);
        Multiple(() =>
        {
            That(eValue.Type, Is.EqualTo(EppoValueType.ARRAY_OF_STRING));
            That(eValue.ArrayValue(), Is.EquivalentTo(new[] {"iOS", "Android"}));
        });
    }
    
    [Test]
    public void ShouldConvertStringType()
    {
        const string json = @"{
            'typedValue': 'us'
        }";
        HasEppoValue? eValue = JsonConvert.DeserializeObject<HasEppoValue>(json);
        Multiple(() =>
        {
            That(eValue.Type, Is.EqualTo(EppoValueType.STRING));
            That(eValue.StringValue(), Is.EqualTo("us"));
        });
    }

    [Test]
    public void ShouldConvertBoolType()
    {
        const string json = @"{
            'typedValue': false
        }";
        HasEppoValue? eValue = JsonConvert.DeserializeObject<HasEppoValue>(json);
        Multiple(() =>
        {
            That(eValue.Type, Is.EqualTo(EppoValueType.BOOLEAN));
            That(eValue.BoolValue(), Is.False);
        });
    }
    [Test]
    public void ShouldConvertIntegerType()
    {
        const string json = @"{
            'typedValue': 5
        }";
        HasEppoValue? eValue = JsonConvert.DeserializeObject<HasEppoValue>(json);
        Multiple(() =>
        {
            That(eValue.Type, Is.EqualTo(EppoValueType.INTEGER));
            That(eValue.IntegerValue(), Is.EqualTo(5));
        });
    }

    [Test]
    public void ShouldConvertNullType()
    {
        const string json = @"{
            'typedValue': null
        }";
        HasEppoValue? eValue = JsonConvert.DeserializeObject<HasEppoValue>(json);
        Multiple(() =>
        {
            That(eValue.Type, Is.EqualTo(EppoValueType.NULL));
            That(eValue.Value, Is.EqualTo(null));
        });
    }


    [Test]
    public void ShouldConvertObjectlType()
    {
        const string json = @"{
            'typedValue': {
                'foo': 'bar',
                'bar': 'baz'
            }
        }";
        HasEppoValue? eValue = JsonConvert.DeserializeObject<HasEppoValue>(json);
        Multiple(() =>
        {
            That(eValue.Type, Is.EqualTo(EppoValueType.JSON));
            // That(eValue.Value, Is.EqualTo(null));s
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
        Assert.That(rules[0].conditions[0].ArrayValue(), Is.EqualTo(new List<string>
        {
            "iOS",
            "Android"
        }));
        Assert.That(rules[0].conditions[0].attribute, Is.EqualTo("device"));

        Assert.That(rules[1].conditions[0].operatorType, Is.EqualTo(OperatorType.NOT_ONE_OF));
        Assert.That(rules[1].conditions[0].attribute, Is.EqualTo("country"));
    }
}