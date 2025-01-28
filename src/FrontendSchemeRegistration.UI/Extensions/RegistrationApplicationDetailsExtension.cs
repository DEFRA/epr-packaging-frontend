using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.UI.Sessions;
using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.UI.Extensions
{
    [ExcludeFromCodeCoverage]
    public static class RegistrationApplicationDetailsExtension
    {
        public static async Task GetRegistrationApplicationStatus(
                                 FrontendSchemeRegistrationSession session,
                                 RegistrationApplicationDetails registrationApplicationDetails,
                                 DateTime? configurableDeadline)
        {
            if (registrationApplicationDetails is not null)
            {
                session.RegistrationApplicationSession = new RegistrationApplicationSession
                {
                    SubmissionId = registrationApplicationDetails.SubmissionId,
                    IsSubmitted = registrationApplicationDetails.IsSubmitted,
                    ApplicationReferenceNumber = registrationApplicationDetails.ApplicationReferenceNumber,
                    RegistrationReferenceNumber = registrationApplicationDetails.RegistrationReferenceNumber,
                    LastSubmittedFile = registrationApplicationDetails.LastSubmittedFile,
                    RegistrationFeePaymentMethod = registrationApplicationDetails.RegistrationFeePaymentMethod,
                    RegistrationApplicationSubmittedDate = registrationApplicationDetails.RegistrationApplicationSubmittedDate,
                    RegistrationApplicationSubmittedComment = registrationApplicationDetails.RegistrationApplicationSubmittedComment,
                    ApplicationStatus = registrationApplicationDetails.ApplicationStatus,
                    ProducerDetails = registrationApplicationDetails.ProducerDetails,
                    CsoMemberDetails = new Application.DTOs.ComplianceSchemeDetailsDto { Members = registrationApplicationDetails.CsoMemberDetails },
                    FileReachedSynapse = registrationApplicationDetails.ProducerDetails is not null ||
                    (registrationApplicationDetails.CsoMemberDetails is not null && registrationApplicationDetails.CsoMemberDetails.Any()),
                    IsLateFeeApplicable = IsApplicationSubmissionLate(registrationApplicationDetails.RegistrationApplicationSubmittedDate, configurableDeadline)
                };
            }
            else
            {
                session.RegistrationApplicationSession = new RegistrationApplicationSession();
            }
        }

        private static bool IsApplicationSubmissionLate(DateTime? applicationSubmissionDate, DateTime? configurableDeadline)
        {
            DateTime ApplicationDeadline = new(DateTime.Today.Year, 4, 1);

            if (configurableDeadline != null && configurableDeadline <= ApplicationDeadline)
            {
                ApplicationDeadline = configurableDeadline.Value;
            }

            if (applicationSubmissionDate is null)  // no initial application
            {
                return DateTime.Today > ApplicationDeadline;
            }
            else
            {
                return applicationSubmissionDate > ApplicationDeadline;
            }
        }
    }
}
