using FluentAssertions;
using FrontendSchemeRegistration.Application.DTOs.ComplianceSchemeMember;
using FrontendSchemeRegistration.Application.DTOs.Subsidiary.OrganisationSubsidiaryList;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Constants;
using FrontendSchemeRegistration.UI.Services;
using Moq;

namespace FrontendSchemeRegistration.UI.UnitTests.Services
{
    [TestFixture]
    public class SubsidiaryUtilityServiceTests
    {
        private SubsidiaryUtilityService _service;
        private Mock<ISubsidiaryService> _mockSubsidiaryService;
        private Mock<IComplianceSchemeMemberService> _mockComplianceSchemeMemberService;

        [SetUp]
        public void Setup()
        {
            _mockSubsidiaryService = new Mock<ISubsidiaryService>();
            _mockComplianceSchemeMemberService = new Mock<IComplianceSchemeMemberService>();

            _service = new SubsidiaryUtilityService(_mockSubsidiaryService.Object, _mockComplianceSchemeMemberService.Object);
        }

        [Test]
        public async Task GetSubsidiariesCount_ShouldReturnCountFromSubsidiaryService_WhenOrganisationIsDirectProducer()
        {
            // Arrange
            var organisationRole = OrganisationRoles.Producer;
            var organisationId = Guid.NewGuid();
            var subsidiaryResponse = new OrganisationRelationshipModel()
            {
                Relationships = new List<RelationshipResponseModel> { new RelationshipResponseModel(), new RelationshipResponseModel() } // count = 2
            };
            _mockSubsidiaryService.Setup(s => s.GetOrganisationSubsidiaries(organisationId))
                .ReturnsAsync(subsidiaryResponse);

            // Act
            var result = await _service.GetSubsidiariesCount(organisationRole, organisationId, null);

            // Assert
            result.Should().Be(2);
            _mockSubsidiaryService.Verify(s => s.GetOrganisationSubsidiaries(organisationId), Times.Once);
            _mockComplianceSchemeMemberService.Verify(s => s.GetComplianceSchemeMembers(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>()), Times.Never);
        }

        [Test]
        public async Task GetSubsidiariesCount_ShouldReturnCountFromSubsidiaryService_WhenOrganisationIsDirectProducerAndNullRepsonse()
        {
            // Arrange
            var organisationRole = OrganisationRoles.Producer;
            var organisationId = Guid.NewGuid();

            _mockSubsidiaryService.Setup(s => s.GetOrganisationSubsidiaries(organisationId))
               .ReturnsAsync((OrganisationRelationshipModel)null);

            // Act
            var result = await _service.GetSubsidiariesCount(organisationRole, organisationId, null);

            // Assert
            result.Should().Be(0);
            _mockSubsidiaryService.Verify(s => s.GetOrganisationSubsidiaries(organisationId), Times.Once);
            _mockComplianceSchemeMemberService.Verify(s => s.GetComplianceSchemeMembers(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>()), Times.Never);
        }

        [Test]
        public async Task GetSubsidiariesCount_ShouldReturnCountFromComplianceSchemeService_WhenOrganisationIsNotDirectProducer()
        {
            // Arrange
            var organisationRole = "SomeOtherRole";
            var organisationId = Guid.NewGuid();
            var schemeId = Guid.NewGuid();
            var complianceSchemeMembershipResponse = new ComplianceSchemeMembershipResponse
            {
                SubsidiariesCount = 5
            };
            _mockComplianceSchemeMemberService.Setup(c => c.GetComplianceSchemeMembers(organisationId, schemeId, 1, string.Empty, 1, true))
                .ReturnsAsync(complianceSchemeMembershipResponse);

            // Act
            var result = await _service.GetSubsidiariesCount(organisationRole, organisationId, schemeId);

            // Assert
            result.Should().Be(5);
            _mockComplianceSchemeMemberService.Verify(c => c.GetComplianceSchemeMembers(organisationId, schemeId, 1, string.Empty, 1, true), Times.Once);
            _mockSubsidiaryService.Verify(s => s.GetOrganisationSubsidiaries(It.IsAny<Guid>()), Times.Never);
        }
     
        [Test]
        public void GetSubsidiariesCount_ShouldThrowArgumentException_WhenSelectedSchemeIdIsNullForNonProducerRole()
        {
            // Arrange
            var organisationRole = "SomeOtherRole";
            var organisationId = Guid.NewGuid();
            Guid? selectedSchemeId = null;

            // Act
            Func<Task> act = async () => await _service.GetSubsidiariesCount(organisationRole, organisationId, selectedSchemeId);

            // Assert
            act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("Selected scheme ID cannot be null for complience scheme.");
        }

        [Test]
        public async Task GetSubsidiariesCount_ShouldReturnZero_WhenSubsidiaryService_ReturnsNull_WhenOrganisationIsDirectProducer()
        {
            // Arrange
            var organisationRole = OrganisationRoles.Producer;
            var organisationId = Guid.NewGuid();
            _mockSubsidiaryService.Setup(s => s.GetOrganisationSubsidiaries(organisationId))
                .ReturnsAsync((OrganisationRelationshipModel)null);
            // Act
            var result = await _service.GetSubsidiariesCount(organisationRole, organisationId, null);
            // Assert
            result.Should().Be(0);
            _mockSubsidiaryService.Verify(s => s.GetOrganisationSubsidiaries(organisationId), Times.Once);
            _mockComplianceSchemeMemberService.Verify(s => s.GetComplianceSchemeMembers(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>()), Times.Never);
        }

        [Test]
        public async Task GetSubsidiariesCount_ShouldReturnZero_WhenSubsidiaryService_ReturnsNullRelationship_WhenOrganisationIsDirectProducer()
        {
            // Arrange
            var organisationRole = OrganisationRoles.Producer;
            var organisationId = Guid.NewGuid();
            var subsidiaryResponse = new OrganisationRelationshipModel()
            {
                Relationships = null
            };
            _mockSubsidiaryService.Setup(s => s.GetOrganisationSubsidiaries(organisationId))
                .ReturnsAsync(subsidiaryResponse);
            // Act
            var result = await _service.GetSubsidiariesCount(organisationRole, organisationId, null);
            // Assert
            result.Should().Be(0);
            _mockSubsidiaryService.Verify(s => s.GetOrganisationSubsidiaries(organisationId), Times.Once);
            _mockComplianceSchemeMemberService.Verify(s => s.GetComplianceSchemeMembers(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>()), Times.Never);
        }

        [Test]
        public async Task GetSubsidiariesCount_ShouldReturnZero_WhenSubsidiaryService_ReturnsNull_WhenOrganisationIsNotDirectProducer()
        {
            // Arrange
            var organisationRole = "SomeOtherRole";
            var organisationId = Guid.NewGuid();
            var schemeId = Guid.NewGuid();
            _mockComplianceSchemeMemberService.Setup(c => c.GetComplianceSchemeMembers(organisationId, schemeId, 1, string.Empty, 1, true))
                .ReturnsAsync((ComplianceSchemeMembershipResponse)null);
            // Act
            var result = await _service.GetSubsidiariesCount(organisationRole, organisationId, schemeId);
            // Assert
            result.Should().Be(0);
            _mockComplianceSchemeMemberService.Verify(c => c.GetComplianceSchemeMembers(organisationId, schemeId, 1, string.Empty, 1, true), Times.Once);
            _mockSubsidiaryService.Verify(s => s.GetOrganisationSubsidiaries(It.IsAny<Guid>()), Times.Never);
        }
    }
}
