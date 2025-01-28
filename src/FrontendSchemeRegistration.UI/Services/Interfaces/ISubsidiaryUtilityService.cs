namespace FrontendSchemeRegistration.UI.Services.Interfaces
{
    public interface ISubsidiaryUtilityService
    {
        Task<int> GetSubsidiariesCount(string organisationRole, Guid organisationId, Guid? selectedSchemeId);
    }
}
