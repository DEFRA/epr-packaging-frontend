namespace FrontendSchemeRegistration.Application.UnitTests.Services;

using System.Net;
using DTOs.ComplianceScheme;
using DTOs.UserAccount;

public abstract class ServiceTestBase<T>
    where T : class
{
    private readonly Guid _newGuid = Guid.NewGuid();

    protected ProducerComplianceSchemeDto CurrentComplianceScheme => new()
    {
        ComplianceSchemeId = _newGuid,
        ComplianceSchemeName = "currentTestScheme",
        ComplianceSchemeOperatorName = "testOperator",
    };

    protected ProducerComplianceSchemeDto SelectedComplianceScheme => new()
    {
        ComplianceSchemeId = _newGuid,
        ComplianceSchemeName = "newTestScheme",
        ComplianceSchemeOperatorName = "testOperator",
    };

    protected User UserAccount => new()
    {
        Id = _newGuid,
        FirstName = "Joe",
        LastName = "Test",
        Email = "JoeTest@something.com",
        RoleInOrganisation = "Test Role",
        EnrolmentStatus = "Enrolled",
        ServiceRole = "Test service role",
        Service = "Test service",
        Organisations = new List<Organisation>
        {
            new()
            {
                Id = _newGuid,
                OrganisationName = "TestCo",
                OrganisationRole = "Producer",
                OrganisationType = "test type"
            }
        }
    };

    protected abstract T MockService(HttpStatusCode expectedStatusCode, HttpContent expectedContent, bool raiseServiceException = false);
}