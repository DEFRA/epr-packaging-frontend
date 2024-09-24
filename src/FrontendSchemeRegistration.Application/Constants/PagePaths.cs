namespace FrontendSchemeRegistration.Application.Constants;

public static class PagePaths
{
    public const string Root = "/";
    public const string ComplianceSchemeSelectionConfirmation = "confirmation";
    public const string ComplianceSchemeStop = "stop";
    public const string ComplianceSchemeMemberLanding = "manage-compliance-scheme";
    public const string ChangeComplianceSchemeOptions = "change-compliance-scheme-options";
    public const string Culture = "culture";
    public const string FileUpload = "/file-upload";
    public const string FileUploadBrands = "/upload-brand-details";
    public const string FileUploadBrandsSuccess = "/file-upload-brands-success";
    public const string FileUploadSubLanding = "/file-upload-sub-landing";
    public const string FileUploadSuccess = "/file-upload-success";
    public const string FileUploadFailure = "/file-upload-failure";
    public const string FileUploadWarning = "/check-warnings";
    public const string FileUploadErrorReport = "/file-upload-error-report";
    public const string FileUploadCompanyDetailsErrorReport = "/file-upload-company-details-error-report";
    public const string FileUploading = "/file-uploading";
    public const string FileUploadCompanyDetails = "/upload-organisation-details";
    public const string FileUploadCompanyDetailsErrors = "/file-upload-company-details-errors";
    public const string FileUploadPartnerships = "/upload-partner-details";
    public const string FileUploadCompanyDetailsSubLanding = "/report-organisation-details";
    public const string FileUploadSubsidiaries = "/subsidiaries-list";
    public const string ExportSubsidiaries = "/subsidiaries-export";
    public const string FileUploadSubsidiariesSuccess = "/subsidiaries-details-success";
    public const string SubsidiariesCompleteFile = "/subsidiaries-complete-file";
    public const string OrganisationDetailsUploaded = "/organisation-details-uploaded";
    public const string FileUploadingCompanyDetails = "/file-uploading-company-details";
    public const string FileReUploadCompanyDetailsConfirmation = "/file-upload-company-details/confirm-upload";
    public const string FileUploadCompanyDetailsSubmissionHistory = "/report-organisation-history";
    public const string UploadNewFileToSubmit = "/upload-new-file-to-submit";
    public const string HomePageComplianceScheme = "producer-compliance-scheme";
    public const string HomePageSelfManaged = "home-self-managed";
    public const string LandingPage = "landing";
    public const string ComplianceSchemeLanding = "home-compliance-scheme";
    public const string SelectComplianceScheme = "select-compliance-scheme";
    public const string SignedOut = "signed-out";
    public const string UsingAComplianceScheme = "using-a-compliance-scheme";
    public const string ReviewOrganisationData = "review-organisation-data";
    public const string FileUploadingPartnerships = "/file-uploading-partnerships";
    public const string FileUploadPartnershipsSuccess = "/file-upload-partnerships-success";
    public const string FileUploadingBrands = "/file-uploading-brands";
    public const string InviteChangePermissions = "/invite-change-permissions";
    public const string InviteChangePermissionsAP = "/invite-change-permissions-ap";
    public const string RoleInOrganisation = "/role-in-Organisation-ap";
    public const string ManualInputRoleInOrganisation = "/manual-input-role-in-organisation-ap";
    public const string TelephoneNumber = "/telephone-number";
    public const string TelephoneNumberAP = "/telephone-number-ap";
    public const string ConfirmPermissionSubmitData = "/confirm-permission-submit-data";
    public const string ConfirmDetailsAP = "/confirm-details-ap";
    public const string DeclarationWithFullName = "/declaration-enter-full-name";
    public const string DeclarationWithFullNameAP = "/declaration-enter-full-name-ap";
    public const string OrganisationDetailsSubmissionFailed = "/organisation-details-submission-failed";
    public const string FileUploadCheckFileAndSubmit = "/file-upload-check-file-and-submit";
    public const string FileUploadSubmissionDeclaration = "/file-upload-submission-declaration";
    public const string FileUploadSubmissionConfirmation = "/file-upload-submission-confirmation";
    public const string CompanyDetailsConfirmation = "/organisation-details-confirmation";
    public const string FileUploadSubmissionError = "/file-upload-submission-error";
    public const string Privacy = "privacy";
    public const string Cookies = "cookies";
    public const string SchemeMembers = "/scheme-members";
    public const string MemberDetails = "/member-details";
    public const string ReasonsForRemoval = "/reason-for-removal";
    public const string TellUsMore = "/tell-us-more";
    public const string ConfirmRemoval = "/confirm-removal";
    public const string ConfirmationOfRemoval = "/confirmation-of-removal";
    public const string UpdateCookieAcceptance = "update-cookie-acceptance";
    public const string AcknowledgeCookieAcceptance = "acknowledge-cookie-acceptance";
    public const string ApprovedPersonCreated = "/approved-person-created";
    public const string FileUploadSubmissionHistory = "/history";
    public const string FileUploadHistoryPreviousSubmissions = "/history-previous-submissions";
    public const string FileUploadHistoryPackagingDataFiles = "/history-packaging-data-files";
    public const string FileUploadNoSubmissionHistory = "/no-submission-history";
    public const string SubsidiaryAdded = "/subsidiary-added";
    public const string SubsidiaryCompaniesHouseNumberSearch = "/subsidiary-companies-house-number-search";
    public const string SubsidiaryConfirmCompanyDetails = "/subsidiary-confirm-company-details";
    public const string RegisteredWithCompaniesHouse = "/subsidiary-registered-with-companies-house";
    public const string SubsidiaryCheckYourDetails = "/subsidiary-check-your-details";
    public const string SubsidiaryUkNation = "/subsidiary-uk-nation";
    public const string SubsidiaryLocation = "/subsidiary-location";
    public const string SubsidiaryCheckDetails = "/subsidiary-check-details";
    public const string SubsidiariesDownload = "/subsidiaries-download";
    public const string SubsidiariesDownloadView = "/subsidiaries-download-view";
    public const string SubsidiariesDownloadFailed = "/subsidiaries-download-failed";
    public const string SubsidiaryTemplateDownload = "/subsidiary-template-download";
    public const string SubsidiaryTemplateDownloadView = "/subsidiary-template-download-view";
    public const string SubsidiaryTemplateDownloadFailed = "/subsidiary-template-download-failed";

    public static class Prns
    {
        public const string Home = "manage-prn-home-complete";
        public const string Search = "view-awaiting-acceptance";
        public const string ShowAwaitingAcceptance = "view-awaiting-acceptance-alt";
        public const string ShowSelected = "selected-prn";

        // Accept single PRN
        public const string AskToAccept = "accept-prn";
        public const string ConfirmAccept = "confirm-accept-prn";
        public const string Accepted = "accepted-prn";

        // Accept multiples PRNS
        public const string BeforeAskToAcceptMany = "accept-bulk-passthrough";
        public const string AskToAcceptMany = "accept-bulk";
        public const string ConfirmAcceptMany = "confirm-accept-bulk";
        public const string AcceptedMany = "accepted-prns";

        // Reject single PRN
        public const string AskToReject = "reject-prn";
        public const string ConfirmReject = "confirm-reject-prn";
        public const string Rejected = "rejected-prn";
    }
}