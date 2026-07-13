using FluentAssertions;
using FrontendSchemeRegistration.Application.Options;
using FrontendSchemeRegistration.UI.Controllers;
using FrontendSchemeRegistration.UI.Services;
using FrontendSchemeRegistration.UI.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Options;
using Moq;

namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

[TestFixture]
public class DeclarationProcessingControllerTests
{
    private const string ViewName = "DeclarationProcessing";
    private Mock<IRegistrationApplicationService> _registrationApplicationServiceMock;
    private DeclarationProcessingController _systemUnderTest;

    [SetUp]
    public void SetUp()
    {
        _registrationApplicationServiceMock = new Mock<IRegistrationApplicationService>();
        var pollingOptions = Options.Create(new RegistrationFeeSnapshotPollingOptions
        {
            TimeoutSeconds = 60,
            IntervalSeconds = 3,
        });

        _systemUnderTest = new DeclarationProcessingController(
            _registrationApplicationServiceMock.Object,
            pollingOptions);

        _systemUnderTest.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Session = new Mock<ISession>().Object
            }
        };

        var urlHelperMock = new Mock<IUrlHelper>();
        urlHelperMock
            .Setup(x => x.Action(It.Is<UrlActionContext>(ctx => ctx.Action == "Status")))
            .Returns("/declaration-processing-status");
        urlHelperMock
            .Setup(x => x.Action(It.Is<UrlActionContext>(ctx => ctx.Action == "Get")))
            .Returns("/organisation-details-confirmation");
        _systemUnderTest.Url = urlHelperMock.Object;
    }

    [Test]
    public void Get_ReturnsViewWithPopulatedViewModel()
    {
        // Arrange
        var submissionId = Guid.NewGuid();

        // Act
        var result = _systemUnderTest.Get(submissionId) as ViewResult;

        // Assert
        result.Should().NotBeNull();
        result.ViewName.Should().Be(ViewName);
        var model = result.Model.Should().BeOfType<DeclarationProcessingViewModel>().Subject;
        model.SubmissionId.Should().Be(submissionId);
        model.PollingIntervalMs.Should().Be(3000);
        model.PollingTimeoutMs.Should().Be(60000);
        model.StatusUrl.Should().Be("/declaration-processing-status");
        model.FallbackUrl.Should().Be("/organisation-details-confirmation");
    }

    [Test]
    public async Task Status_SnapshotReady_ReturnsRedirectUrl()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        _registrationApplicationServiceMock
            .Setup(s => s.TryPopulateRegistrationFeeSnapshotAsync(It.IsAny<ISession>(), submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _systemUnderTest.Status(submissionId);

        // Assert
        var redirectUrl = result.Value!.GetType().GetProperty("redirectUrl")!.GetValue(result.Value) as string;
        redirectUrl.Should().Be("/organisation-details-confirmation");
    }

    [Test]
    public async Task Status_SnapshotNotReady_ReturnsInProgress()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        _registrationApplicationServiceMock
            .Setup(s => s.TryPopulateRegistrationFeeSnapshotAsync(It.IsAny<ISession>(), submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _systemUnderTest.Status(submissionId);

        // Assert
        var inProgress = result.Value!.GetType().GetProperty("isFeeCalculationInProgress")!.GetValue(result.Value);
        inProgress.Should().Be(true);
    }
}
