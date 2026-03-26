namespace FrontendSchemeRegistration.Application.UnitTests.Options;

using Application.Options;
using FluentAssertions;

public class FibreOptionsTests
{
    [Test]
    public void WhenLaunchDateSet_ShouldReturnUtc()
    {
        var subject = new FibreOptions
        {
            LaunchDate = "2026-03-01"
        };

        subject.LaunchDateUtc.Kind.Should().Be(DateTimeKind.Utc);
    }
}