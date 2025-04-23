namespace FrontendSchemeRegistration.UI.UnitTests.Attributes.ActionFilters;

using Application.Constants;
using EPR.Common.Authorization.Models;
using EPR.Common.Authorization.Sessions;
using FluentAssertions;
using FrontendSchemeRegistration.Application.DTOs.ComplianceScheme;
using FrontendSchemeRegistration.UI.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Moq;
using UI.Attributes.ActionFilters;
using UI.Sessions;

[TestFixture]
public class PrnsObligationActionFilterAttributeTests
{
    private Mock<ActionExecutionDelegate> _delegateMock;
    private Mock<ISessionManager<FrontendSchemeRegistrationSession>> _sessionMock;
    private Mock<IServiceProvider> _serviceProviderMock;
    private PrnsObligationActionFilterAttribute _systemUnderTest;
    private ActionExecutingContext _actionExecutingContext;

    [SetUp]
    public void SetUp()
    {
        _delegateMock = new Mock<ActionExecutionDelegate>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _sessionMock = new Mock<ISessionManager<FrontendSchemeRegistrationSession>>();
        _serviceProviderMock.Setup(x => x.GetService(typeof(ISessionManager<FrontendSchemeRegistrationSession>))).Returns(_sessionMock.Object);
        _actionExecutingContext = new ActionExecutingContext(
            new ActionContext(
                new DefaultHttpContext
                {
                    RequestServices = _serviceProviderMock.Object,
                    Session = new Mock<ISession>().Object
                },
                Mock.Of<RouteData>(),
                Mock.Of<ActionDescriptor>(),
                Mock.Of<ModelStateDictionary>()),
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            Mock.Of<Controller>());
        _systemUnderTest = new PrnsObligationActionFilterAttribute();
    }

    [Test]
    public async Task OnActionExecutionAsync_CallsNext_WhenSessionIsPresent_ForDirectRegistrant()
    {
        // Arrange
        _sessionMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(new FrontendSchemeRegistrationSession
        {
            UserData = new UserData
            {
                Organisations = new List<Organisation>
                {
                    new()
                    {
                        Name = "Test Organisation",
						OrganisationRole = OrganisationRoles.Producer,
						NationId = 1
                    }
                }
            }
        });

        // Act
        await _systemUnderTest.OnActionExecutionAsync(_actionExecutingContext, _delegateMock.Object);

        // Assert
        _delegateMock.Verify(next => next(), Times.Once);
    }


	[Test]
	public async Task OnActionExecutionAsync_CallsNext_WhenSessionIsPresent_ForComplianceSchemeOperator()
	{
		// Arrange
		_sessionMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(new FrontendSchemeRegistrationSession
		{
			RegistrationSession = new()
			{
				SelectedComplianceScheme = new ComplianceSchemeDto()
				{
					Id = Guid.NewGuid(),
					Name = "Test",
					NationId = 1
				}
			},
			UserData = new UserData
			{
				Organisations = new List<Organisation>
				{
					new()
					{
						Name = "Test Organisation",
						OrganisationRole = OrganisationRoles.ComplianceScheme,
						NationId = 1
					}
				}
			}
		});

		// Act
		await _systemUnderTest.OnActionExecutionAsync(_actionExecutingContext, _delegateMock.Object);

		// Assert
		_delegateMock.Verify(next => next(), Times.Once);
	}

	[Test]
    public async Task OnActionExecutionAsync_RedirectsToRoot_WhenSessionIsNotPresent_ForAnyUser()
	{
        // Arrange
        _sessionMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync((FrontendSchemeRegistrationSession)null);

        // Act
        await _systemUnderTest.OnActionExecutionAsync(_actionExecutingContext, _delegateMock.Object);

        // Assert
        _actionExecutingContext.Result.Should().BeOfType<RedirectResult>().Subject.Url.Should().Be($"~{PagePaths.Root}");
        _delegateMock.Verify(next => next(), Times.Never);
    }

	[Test]
	public async Task OnActionExecutionAsync_RedirectsToRoot_WhenUserDataIsNullInSession_ForAnyUser()
	{
		// Arrange
		_sessionMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(new FrontendSchemeRegistrationSession
		{
			UserData = null
		});

		// Act
		await _systemUnderTest.OnActionExecutionAsync(_actionExecutingContext, _delegateMock.Object);

		// Assert
		_actionExecutingContext.Result.Should().BeOfType<RedirectResult>().Subject.Url.Should().Be($"~{PagePaths.Root}");
		_delegateMock.Verify(next => next(), Times.Never);
	}

	[Test]
	public async Task OnActionExecutionAsync_RedirectsToRoot_WhenOrganisationIsNullInSession_ForAnyUser()
	{
		// Arrange
		_sessionMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(new FrontendSchemeRegistrationSession
		{
			UserData = new UserData
			{
				Organisations = null
			}
		});

		// Act
		await _systemUnderTest.OnActionExecutionAsync(_actionExecutingContext, _delegateMock.Object);

		// Assert
		_actionExecutingContext.Result.Should().BeOfType<RedirectResult>().Subject.Url.Should().Be($"~{PagePaths.Root}");
		_delegateMock.Verify(next => next(), Times.Never);
	}

	[Test]
	public async Task OnActionExecutionAsync_RedirectsToRoot_WhenOrganisationCountIsZeroInSession_ForAnyUser()
	{
		// Arrange
		_sessionMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(new FrontendSchemeRegistrationSession
		{

			UserData = new UserData
			{
				Organisations = new List<Organisation>()
			}
		});

		// Act
		await _systemUnderTest.OnActionExecutionAsync(_actionExecutingContext, _delegateMock.Object);

		// Assert
		_actionExecutingContext.Result.Should().BeOfType<RedirectResult>().Subject.Url.Should().Be($"~{PagePaths.Root}");
		_delegateMock.Verify(next => next(), Times.Never);
	}

	[Test]
	public async Task OnActionExecutionAsync_RedirectsToRoot_WhenRegistrationSessionIsNull_ForComplianceSchemeOperator()
	{
		// Arrange
		_sessionMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(new FrontendSchemeRegistrationSession
		{
			UserData = new UserData
			{
				Organisations = new List<Organisation>
				{
					new()
					{
						Name = "Test Organisation",
						OrganisationRole = OrganisationRoles.ComplianceScheme,
						NationId = 1
					}
				}
			}
		});

		// Act
		await _systemUnderTest.OnActionExecutionAsync(_actionExecutingContext, _delegateMock.Object);

		// Assert
		_actionExecutingContext.Result.Should().BeOfType<RedirectResult>().Subject.Url.Should().Be($"~{PagePaths.Root}");
		_delegateMock.Verify(next => next(), Times.Never);
	}

	[Test]
	public async Task OnActionExecutionAsync_RedirectsToRoot_WhenSelectedComplianceSchmemeIsNullInSession_ForComplianceSchemeOperator()
	{
		// Arrange
		_sessionMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(new FrontendSchemeRegistrationSession
		{
			RegistrationSession = new()
			{
				SelectedComplianceScheme = null
			},
			UserData = new UserData
			{
				Organisations = new List<Organisation>
				{
					new()
					{
						Name = "Test Organisation",
						OrganisationRole = OrganisationRoles.ComplianceScheme,
						NationId = 1
					}
				}
			}
		});

		// Act
		await _systemUnderTest.OnActionExecutionAsync(_actionExecutingContext, _delegateMock.Object);

		// Assert
		_actionExecutingContext.Result.Should().BeOfType<RedirectResult>().Subject.Url.Should().Be($"~{PagePaths.Root}");
		_delegateMock.Verify(next => next(), Times.Never);
	}
}