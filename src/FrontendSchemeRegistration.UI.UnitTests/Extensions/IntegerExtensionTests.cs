using FluentAssertions;
using FrontendSchemeRegistration.UI.Extensions;

namespace FrontendSchemeRegistration.UI.UnitTests.Extensions
{
    [TestFixture]
    public class IntegerExtensionTests
    {

        [Test]
        [TestCase(0, "Zero")]
        [TestCase(1, "One")]
        [TestCase(2, "Two")]
        [TestCase(3, "Three")]
        [TestCase(4, "Four")]
        [TestCase(5, "Five")]
        [TestCase(6, "Six")]
        [TestCase(7, "Seven")]
        [TestCase(8, "Eight")]
        [TestCase(9, "Nine")]
        [TestCase(123, "One Two Three")]
        [TestCase(1000, "One Zero Zero Zero")]
        public void ToDigitsAsWords_WhenAskedForEnglish_ReturnsEnglish(int value, string expectedValue)
        {
            string actual = value.ToDigitsAsWords("en");

            actual.Should().Be(expectedValue);
        }

        [Test]
        [TestCase(0, "Zero")]
        [TestCase(1, "One")]
        [TestCase(2, "Two")]
        [TestCase(3, "Three")]
        [TestCase(4, "Four")]
        [TestCase(5, "Five")]
        [TestCase(6, "Six")]
        [TestCase(7, "Seven")]
        [TestCase(8, "Eight")]
        [TestCase(9, "Nine")]
        [TestCase(123, "One Two Three")]
        [TestCase(1000, "One Zero Zero Zero")]
        public void ToDigitsAsWords_WhenAskedForWelsh_ReturnsWelsh(int value, string expectedValue)
        {
            string actual = value.ToDigitsAsWords("cy");

            actual.Should().Be(expectedValue);
        }

        [Test]
        [TestCase(null)]
        [TestCase("")]
        [TestCase("x")]
        [TestCase("xyz")]
        public void ToDigitsAsWords_ReturnsEnglishByDefault(string twoLetterISOLanguageName)
        {
            string actual = 123.ToDigitsAsWords(twoLetterISOLanguageName);

            actual.Should().Be("One Two Three");
        }
    }
}