using eppo_sdk.helpers;

namespace eppo_sdk_test.helpers;

public class InputValidatorTest
{
    [Test]
    public void ShouldValidateNotBlankWithCorrectInput()
    {
        Assert.That(InputValidator.ValidateNotBlank("testing", "Message Sample"), Is.True);
    }

    [Test]
    public void ShouldValidateNotBlankAndThrowError()
    {
        var invalidDataException = Assert.Throws<InvalidDataException>(() =>
            InputValidator.ValidateNotBlank("", "Name should be valid")
        );
        Assert.That(invalidDataException?.Message, Is.EqualTo("Name should be valid"));
    }
}
