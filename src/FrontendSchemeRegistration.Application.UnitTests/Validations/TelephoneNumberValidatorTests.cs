namespace FrontendSchemeRegistration.Application.UnitTests.Validations;

using Application.Validations;
using FluentAssertions;

[TestFixture]
public class TelephoneNumberValidatorTests
{
    [Test]
    [TestCase("020 1212 1212")]
    [TestCase("078 1212 1212")]
    [TestCase("78 1212 1212")]
    [TestCase("+44 078 1212 1212")]
    [TestCase("+44 78 1212 1212")]
    [TestCase("(+44) 78 1212 1212")]
    [TestCase("0044 078 1212 1212")]
    [TestCase("02012121212")]
    [TestCase("07812121212")]
    [TestCase("7812121212")]
    [TestCase("+4407812121212")]
    [TestCase("+447812121212")]
    [TestCase("004407812121212")]
    [TestCase("+49 30 901820")]
    [TestCase("+34919931307")]
    public void IsValid_GivenValidUKNumberProvided_ShouldReturnTrue(string phoneNumber)
    {
        TelephoneNumberValidator.IsValid(phoneNumber).Should().BeTrue();
    }

    [Test]
    [TestCase("020 1212 121")]
    [TestCase("020 1212 121")]
    [TestCase("078 1212 121A")]
    [TestCase("")]
    [TestCase("a")]
    [TestCase("800 890 567sad")]
    [TestCase("800 890 567123")]
    [TestCase("asd800 890 567")]
    [TestCase("123800 890 567")]
    [TestCase("07812121212!!")]
    [TestCase("..07812121212")]
    [TestCase("!@£$%800 890 567")]
    [TestCase("072121212^&*()_+")]
    [TestCase("0721^&*()_+2121")]
    [TestCase("078 1(212 12)1A")]
    public void IsValid_GivenInvalidUKNumberProvided_ShouldReturnFalse(string phoneNumber)
    {
        TelephoneNumberValidator.IsValid(phoneNumber).Should().BeFalse();
    }
}