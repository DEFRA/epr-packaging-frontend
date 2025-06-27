namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

using Application.Constants;
using Application.DTOs.Submission;
using Application.Services.Interfaces;
using Constants;
using EPR.Common.Authorization.Models;
using EPR.Common.Authorization.Sessions;
using FluentAssertions;
using FrontendSchemeRegistration.UI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Moq;
using UI.Controllers;
using UI.Sessions;
using UI.ViewModels;

[TestFixture]
public class FileUploadPartnershipsSuccessControllerTests
{
    private static readonly string SubmissionId = Guid.NewGuid().ToString();

    private FileUploadPartnershipsSuccessController _systemUnderTest;
    private Mock<ISubmissionService> _submissionServiceMock;
    private Mock<ISessionManager<FrontendSchemeRegistrationSession>> _sessionManagerMock;
    private Mock<IRegistrationApplicationService> _registrationApplicationServiceMock;

    [SetUp]
    public void SetUp()
    {
        _submissionServiceMock = new Mock<ISubmissionService>();
        _sessionManagerMock = new Mock<ISessionManager<FrontendSchemeRegistrationSession>>();
        _registrationApplicationServiceMock = new Mock<IRegistrationApplicationService>();
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                UserData = new UserData
                {
                    Organisations = new List<Organisation>
                    {
                        new()
                        {
                            OrganisationRole = OrganisationRoles.Producer
                        }
                    }
                },
                RegistrationSession = new RegistrationSession
                {
                    Journey = new List<string>
                    {
                        PagePaths.FileUploadCompanyDetailsSubLanding,
                        PagePaths.FileUploadCompanyDetails,
                        PagePaths.FileUploadBrands,
                        PagePaths.FileUploadPartnerships
                    }
                }
            });
        _registrationApplicationServiceMock.Setup(x => x.validateRegistrationYear(It.IsAny<string>(), It.IsAny<bool>())).Returns(DateTime.Now.Year);

        _systemUnderTest = new FileUploadPartnershipsSuccessController(_submissionServiceMock.Object, _sessionManagerMock.Object, _registrationApplicationServiceMock.Object);
        _systemUnderTest.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Request =
                {
                    Query = new QueryCollection(new Dictionary<string, StringValues>
                    {
                        { "submissionId", SubmissionId },
                    }),
                },
                Session = new Mock<ISession>().Object
            }
        };
    }

    [Test]
    public async Task Get_RedirectsToFileUploadCompanyDetailsGet_WhenGetSubmissionAsyncReturnsNull()
    {
        // Arrange
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>())).ReturnsAsync((RegistrationSubmission)null);

        // Act
        var result = await _systemUnderTest.Get() as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("FileUploadCompanyDetails");

        _submissionServiceMock.Verify(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()), Times.Once);
    }

    [Test]
    public async Task Get_ReturnsFileUploadPartnershipsSuccessView_WhenGetSubmissionAsyncReturnsSubmissionDto()
    {
        // Arrange
        const string fileName = "example.csv";
        _submissionServiceMock.Setup(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()))
            .ReturnsAsync(new RegistrationSubmission
            {
                PartnershipsFileName = fileName
            });

        // Act
        var result = await _systemUnderTest.Get() as ViewResult;

        // Assert
        result.ViewName.Should().Be("FileUploadPartnershipsSuccess");
        result.Model.Should().BeEquivalentTo(new FileUploadSuccessViewModel
        {
            FileName = fileName,
            SubmissionId = new Guid(SubmissionId),
            RegistrationYear = DateTime.Now.Year
        });

        _submissionServiceMock.Verify(x => x.GetSubmissionAsync<RegistrationSubmission>(It.IsAny<Guid>()), Times.Once);
    }
}