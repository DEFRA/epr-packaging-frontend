namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

using Application.DTOs.Submission;
using Application.DTOs.UserAccount;
using Application.Services.Interfaces;
using Constants;
using EPR.Common.Authorization.Sessions;
using FluentAssertions;
using FrontendSchemeRegistration.Application.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Primitives;
using Moq;
using UI.Controllers;
using UI.Controllers.ControllerExtensions;
using UI.Sessions;
using UI.ViewModels;

[TestFixture]
public class FileUploadSubmissionConfirmationControllerTests
{
    private static readonly Guid SubmissionId = Guid.NewGuid();
    private Mock<ISubmissionService> _submissionServiceMock;
    private Mock<IUserAccountService> _userAccountServiceMock;
    private Mock<ISessionManager<FrontendSchemeRegistrationSession>> _sessionManagerMock;
    private FileUploadSubmissionConfirmation _systemUnderTest;
    private Mock<IUrlHelper> _urlHelperMock;

    [SetUp]
    public void SetUp()
    {
        _urlHelperMock = new Mock<IUrlHelper>();
        _urlHelperMock.Setup(url => url.Action(It.Is<UrlActionContext>(uac => uac.Action == "Get")))
            .Returns(PagePaths.FileUploadSubLanding);
        _urlHelperMock.Setup(x => x.Content(It.IsAny<string>())).Returns((string contentPath) => contentPath);
        _sessionManagerMock = new Mock<ISessionManager<FrontendSchemeRegistrationSession>>();
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                UserData = new() { Organisations = { new() { OrganisationRole = OrganisationRoles.ComplianceScheme } } }
            });

        _submissionServiceMock = new Mock<ISubmissionService>();

        _userAccountServiceMock = new Mock<IUserAccountService>();

        _systemUnderTest = new FileUploadSubmissionConfirmation(_submissionServiceMock.Object, _userAccountServiceMock.Object, _sessionManagerMock.Object);
        _systemUnderTest.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Request = { Query = new QueryCollection(new Dictionary<string, StringValues> { { "submissionId", SubmissionId.ToString() } }) },
                Session = Mock.Of<ISession>()
            }
        };

        _systemUnderTest.Url = _urlHelperMock.Object;
    }

    [Test]
    public async Task Get_RedirectsToFileUpload_WhenNoSubmissionIsFound()
    {
        // Act
        var result = await _systemUnderTest.Get() as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be(nameof(FileUploadController.Get));
        result.ControllerName.Should().Be(nameof(FileUploadController).RemoveControllerFromName());
    }

    [Test]
    public async Task Get_ReturnsFileUploadSubmissionConfirmationView_WhenGetSubmissionAsyncReturnsSubmissionDto()
    {
        // Arrange
        const string personFirstName = "Jane";
        const string personLastName = "Doe";
        var submittedByGuid = Guid.NewGuid();
        var submittedAt = DateTime.UtcNow;

        _submissionServiceMock
            .Setup(x => x.GetSubmissionAsync<PomSubmission>(SubmissionId))
            .ReturnsAsync(new PomSubmission
            {
                Id = SubmissionId,
                LastSubmittedFile = new SubmittedFileInformation
                {
                    SubmittedBy = submittedByGuid,
                    SubmittedDateTime = submittedAt
                }
            });

        _userAccountServiceMock
            .Setup(x => x.GetPersonByUserId(submittedByGuid))
            .ReturnsAsync(new PersonDto
            {
                FirstName = personFirstName,
                LastName = personLastName
            });

        // Act
        var result = await _systemUnderTest.Get() as ViewResult;

        // Assert
        result.ViewName.Should().Be("FileUploadSubmissionConfirmation");
        result.ViewData.Keys.Should().HaveCount(1);
        result.ViewData.Keys.Should().Contain("BackLinkToDisplay");
        result.ViewData["BackLinkToDisplay"].Should().Be($"~{PagePaths.FileUploadSubLanding}");
        result.Model.Should().BeEquivalentTo(new FileUploadSubmissionConfirmationViewModel
        {
            OrganisationRole = OrganisationRoles.ComplianceScheme,
            SubmittedAt = submittedAt,
            SubmittedBy = "Jane Doe"
        });

        _submissionServiceMock.Verify(x => x.GetSubmissionAsync<PomSubmission>(SubmissionId), Times.Once);
        _userAccountServiceMock.Verify(x => x.GetPersonByUserId(submittedByGuid), Times.Once);
    }
}
