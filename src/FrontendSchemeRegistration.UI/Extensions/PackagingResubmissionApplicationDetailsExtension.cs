using EPR.Common.Authorization.Models;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.UI.Sessions;
using FrontendSchemeRegistration.UI.ViewModels;

namespace FrontendSchemeRegistration.UI.Extensions
{
    public static class PackagingResubmissionApplicationDetailsExtension
    {
        public static List<PackagingResubmissionApplicationSession> ToPackagingResubmissionApplicationSessionList(this List<PackagingResubmissionApplicationDetails> items, Organisation organisation)
        {
            return items.Select(x => x.ToPackagingResubmissionApplicationSession(organisation)).ToList();
        }

        public static PackagingResubmissionApplicationSession ToPackagingResubmissionApplicationSession(this PackagingResubmissionApplicationDetails item, Organisation organisation)
        {
            var packagingResubmissionApplicationDetails = new PackagingResubmissionApplicationSession
            {
                SubmissionId = item.SubmissionId,
                IsSubmitted = item.IsSubmitted,
                ApplicationReferenceNumber = item.ApplicationReferenceNumber,
                ResubmissionReferenceNumber = item.ResubmissionReferenceNumber,
                LastSubmittedFile = item.LastSubmittedFile,
                ResubmissionFeePaymentMethod = item.ResubmissionFeePaymentMethod,
                ResubmissionApplicationSubmittedDate = item.ResubmissionApplicationSubmittedDate,
                ResubmissionApplicationSubmittedComment = item.ResubmissionApplicationSubmittedComment,
                ApplicationStatus = item.ApplicationStatus,
                FileReachedSynapse = item.SynapseResponse.IsFileSynced,
                Organisation = organisation,
                IsResubmissionFeeViewed = item.IsResubmissionFeeViewed
            };

            return packagingResubmissionApplicationDetails;
        }

        public static ResubmissionTaskListViewModel ToResubmissionTaskListViewModel(this List<PackagingResubmissionApplicationDetails> resubmissions, Organisation organisation)
        {
            var resubmissionTaskListModel = new ResubmissionTaskListViewModel();

            if (resubmissions != null && resubmissions.Count > 0)
            {
                var resubmission = resubmissions.FirstOrDefault();

                if (resubmission != null)
                {
                    var resubmissionSession = resubmission.ToPackagingResubmissionApplicationSession(organisation);

                    resubmissionTaskListModel.IsSubmitted = resubmissionSession.IsSubmitted;
                    resubmissionTaskListModel.IsResubmissionInProgress = resubmissionSession.IsResubmissionInProgress;
                    resubmissionTaskListModel.IsResubmissionComplete = resubmissionSession.IsResubmissionComplete;
                    resubmissionTaskListModel.AppReferenceNumber = resubmissionSession.ApplicationReferenceNumber;
                }
            }

            return resubmissionTaskListModel;
        }
    }
}
