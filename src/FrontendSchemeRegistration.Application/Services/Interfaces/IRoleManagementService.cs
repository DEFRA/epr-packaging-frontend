namespace FrontendSchemeRegistration.Application.Services.Interfaces;

using DTOs;

public interface IRoleManagementService
{
    public Task<DelegatedPersonNominatorDto> GetDelegatedPersonNominator(Guid enrolmentId, Guid? organisationId);

    public Task<HttpResponseMessage> AcceptNominationToDelegatedPerson(Guid enrolmentId, Guid organisationId, string serviceKey, AcceptNominationRequest acceptNominationRequest);
}
