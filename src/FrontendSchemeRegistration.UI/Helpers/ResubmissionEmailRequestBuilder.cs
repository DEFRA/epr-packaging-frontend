using EPR.Common.Authorization.Models;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.Application.RequestModels;
using FrontendSchemeRegistration.UI.Constants;
using FrontendSchemeRegistration.UI.Sessions;

namespace FrontendSchemeRegistration.UI.Helpers
{
    public static class ResubmissionEmailRequestBuilder
    {
        public static ResubmissionEmailRequestModel BuildResubmissionEmail(UserData userData, PomSubmission? submission, FrontendSchemeRegistrationSession? session)
        {
            var organisation = userData.Organisations.FirstOrDefault();

            var input = new ResubmissionEmailRequestModel
            {
                OrganisationNumber = organisation.OrganisationNumber,
                ProducerOrganisationName = organisation.Name,
                SubmissionPeriod = submission.SubmissionPeriod,
                NationId = (int)organisation.NationId,
                IsComplianceScheme = organisation.OrganisationRole == OrganisationRoles.ComplianceScheme,
            };

            if (input.IsComplianceScheme)
            {
                input.ProducerOrganisationName = session.RegistrationSession.SelectedComplianceScheme?.Name;
                input.ComplianceSchemeName = organisation.Name;
                input.ComplianceSchemePersonName = $"{userData.FirstName} {userData.LastName}";
                input.NationId = session.RegistrationSession.SelectedComplianceScheme?.NationId ?? 0;
            }

            return input;
        }
    }
}
