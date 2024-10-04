namespace FrontendSchemeRegistration.UI.UnitTests.Controllers.Prns
{
    using System.Threading.Tasks;
    using EPR.Common.Authorization.Models;
    using EPR.Common.Authorization.Sessions;
    using FluentAssertions;
    using FrontendSchemeRegistration.Application.Constants;
    using FrontendSchemeRegistration.UI.Constants;
    using FrontendSchemeRegistration.UI.Controllers.Prns;
    using FrontendSchemeRegistration.UI.Sessions;
    using FrontendSchemeRegistration.UI.ViewModels.Prns;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Routing;
    using Moq;
    using NUnit.Framework;

    [TestFixture]
    public class PrnsObligationControllerTests
    {
        private Mock<IUrlHelper> _urlHelperMock;
        private Mock<ISessionManager<FrontendSchemeRegistrationSession>> _sessionManagerMock;
        private PrnsObligationController _controller;

        [SetUp]
        public void SetUp()
        {
            _urlHelperMock = new Mock<IUrlHelper>();
            _urlHelperMock.Setup(url => url.Action(It.Is<UrlActionContext>(uac => uac.Action == "ObloigationsHome")))
                .Returns(PagePaths.Prns.ObligationsHome);
            _sessionManagerMock = new Mock<ISessionManager<FrontendSchemeRegistrationSession>>();

            _controller = new PrnsObligationController(_sessionManagerMock.Object)
            {
                Url = _urlHelperMock.Object
            };

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    Session = new Mock<ISession>().Object
                }
            };
        }


        [Theory]
        [TestCase(OrganisationRoles.Producer, 1)]
        [TestCase(OrganisationRoles.ComplianceScheme, 2)]
        [TestCase(OrganisationRoles.Producer, 3)]
        [TestCase(OrganisationRoles.ComplianceScheme, 4)]
        public async Task ObligationsHome_Returns_View_With_Valid_ViewModel(string organisationRole, int nationId)
        {
            // Arrange
            var session = new FrontendSchemeRegistrationSession
            {
                UserData = new UserData
                {
                    Organisations = new List<Organisation>
                    {
                        new() {
                            OrganisationRole = organisationRole,
                            Name = "Test Organisation",
                            NationId = nationId
                        }
                    }
                }
            };
            _sessionManagerMock.Setup(m => m.GetSessionAsync(It.IsAny<ISession>()))
                .ReturnsAsync(session);

            // Act
            var result = await _controller.ObligationsHome() as ViewResult;

            // Assert
            result.Should().NotBeNull();
            result.Model.Should().BeOfType<PrnObligationViewModel>();

            var model = result.Model as PrnObligationViewModel;
            model.OrganisationRole.Should().BeEquivalentTo(organisationRole);
            model.OrganisationName.Should().BeEquivalentTo("Test Organisation");
            model.NationId.Should().NotBeNull();
            model.NationId.Value.Should().Be(nationId);
            model.CurrentYear.Should().Be(DateTime.Now.Year);
            model.DeadlineYear.Should().Be(DateTime.Now.Year + 1);
        }

        [Test]
        public async Task ObligationsHome_Returns_View_With_DefaultSession_When_No_Session_Exists()
        {
            // Arrange
            _sessionManagerMock.Setup(m => m.GetSessionAsync(It.IsAny<ISession>()))
                .ReturnsAsync((FrontendSchemeRegistrationSession)null);
            // Act
            var result = await _controller.ObligationsHome() as ViewResult;

            // Assert
            result.Should().NotBeNull();
            result.Model.Should().BeOfType<PrnObligationViewModel>();

            var model = result.Model as PrnObligationViewModel;
            model.OrganisationRole.Should().BeEquivalentTo(null);
            model.OrganisationName.Should().BeEquivalentTo(null);
            model.NationId.Should().BeNull(null);
            model.CurrentYear.Should().Be(0);
            model.DeadlineYear.Should().Be(0);
        }
    }
}
