using FrontendSchemeRegistration.Application.Attributes;

namespace FrontendSchemeRegistration.Application.Enums
{
    public enum InProgressSubmissionPeriodStatus
    {
        [LocalizedName("inprogress_resubmission_fileinsynapse_feesnotviewed_notsubmitted")]
        InProgress_Resubmission_FileInSynapse_FeesNotViewed_NotSubmitted,

        [LocalizedName("inprogress_resubmission_feesviewed_notsubmitted")]
        InProgress_Resubmission_FeesViewed_NotSubmitted
    }
}
