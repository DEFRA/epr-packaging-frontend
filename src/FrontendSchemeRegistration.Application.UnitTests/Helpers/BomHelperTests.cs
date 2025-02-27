namespace FrontendSchemeRegistration.Application.UnitTests.Helpers;

using FluentAssertions;
using FrontendSchemeRegistration.Application.Helpers;

[TestFixture]
public class BomHelperTests
{
    [Test]
    public void PrependBOMBytes_Returns_When_MemoryStreamIsNull()
    {
        MemoryStream memoryStream = null;

        BomHelper.PrependBOMBytes(memoryStream);

        memoryStream.Should().BeNull();
    }
}