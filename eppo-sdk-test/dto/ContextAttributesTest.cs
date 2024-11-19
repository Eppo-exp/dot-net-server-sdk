using eppo_sdk.dto.bandit;
using eppo_sdk.exception;

namespace eppo_sdk_test.dto;

public class ContextAttributesTest
{
    [Test]
    public void ShouldSortAndConvertAttributes()
    {
        var attrs = new Dictionary<string, object>() {
            {"age", 30},
            {"tier", "4"},
            {"referralUser", true},
            {"accountAge", 0.5},
            {"favouriteColour", "green"}
        };
        var expectedNumeric = new Dictionary<string, double>()
        {
            {"age",30.0},
            {"accountAge", 0.5},
        };
        var expectedStrings = new Dictionary<string, string>()
        {
            {"tier", "4"},
            {"referralUser","true"},
            {"favouriteColour","green"}
        };
        var actual = ContextAttributes.FromDict("context", attrs);
        Assert.Multiple(() =>
        {
            Assert.That(actual.GetNumeric(), Is.EquivalentTo(expectedNumeric));
            Assert.That(actual.GetCategorical(), Is.EquivalentTo(expectedStrings));
        });
    }
    [Test]
    public void ShouldAllowExplicitCategoricalAttributes()
    {
        var categoryAttrs = new Dictionary<string, object?>() {
            {"age", 30},
            {"tier", "4"},
            {"referralUser", true},
            {"favouriteColour", "green"}
        };
        var numericAttrs = new Dictionary<string, object?>() {
            {"accountAge", 0.5},
        };
        var expectedNumeric = new Dictionary<string, double>()
        {
            {"accountAge", 0.5},
        };
        var expectedStrings = new Dictionary<string, string>()
        {
            {"age","30"},
            {"tier", "4"},
            {"referralUser","true"},
            {"favouriteColour","green"}
        };
        var actual = ContextAttributes.FromNullableAttributes("context", categoryAttrs, numericAttrs);
        Assert.Multiple(() =>
        {
            Assert.That(actual.GetNumeric(), Is.EquivalentTo(expectedNumeric));
            Assert.That(actual.GetCategorical(), Is.EquivalentTo(expectedStrings));
        });
    }

    [Test]
    public void Add_InvalidType_ThrowsInvalidAttributeTypeException()
    {
        var contextAttributes = new ContextAttributes("test_key");
        Assert.Throws<InvalidAttributeTypeException>(() => contextAttributes.Add("key1", new object()));
    }

    [Test]
    public void Add_InvalidType_ExceptionContainsKeyAndValue()
    {
        var contextAttributes = new ContextAttributes("test_key");
        object invalidValue = new object();

        var exception = Assert.Throws<InvalidAttributeTypeException>(() => contextAttributes.Add("key1", invalidValue));
        Assert.Multiple(() =>
        {
            Assert.That(exception.Key, Is.EqualTo("key1"));
            Assert.That(exception.Value, Is.EqualTo(invalidValue));
        });
    }
}
