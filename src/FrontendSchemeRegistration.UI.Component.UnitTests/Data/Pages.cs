namespace FrontendSchemeRegistration.UI.Component.UnitTests.Data;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class Pages
{

    public static List<Page> GetPages()
    {
        return
        [
            new Page {Url = "/report-data/home-compliance-scheme", Name = "Compliance Scheme Landing Page"},
            new Page {Url = "/report-data", Name = "Report Data Page"},
            new Page {Url = "/services/account-details", Name = "Login Page"},
            new Page {Url = "/report-data/home-self-managed", Name = "Producer Landing Page"},
            new Page {Url = "/report-data/manage-compliance-scheme", Name = "Compliance Scheme Member Landing Page"},
            new Page {Url = "/report-data/file-upload-sub-landing", Name = "File Upload Sub Landing Page"},
            new Page {Url = "/report-data/report-organisation-details", Name = "Company Details Sub Landing Page"},
            new Page {Url = "/report-data/upload-organisation-details", Name = "Company Details Upload Page"},
            new Page {Url = "/report-data/uploading-organisation-details", Name = "Uploading Organisation Details Page"},
            new Page {Url = "/report-data/organisation-details-uploaded", Name = "Company Details Success Page"},
            new Page {Url = "/report-data/file-upload-company-details-warnings", Name = "Company Details Warnings Page"},
            new Page {Url = "/report-data/file-upload-company-details-errors", Name = "Company Details Errors Page"},
            new Page {Url = "/report-data/review-organisation-data", Name = "Review Company Details Page"},
            new Page {Url = "/report-data/producer-registration-guidance?registrationyear=2026", Name = "Registration Guidance Page"},
            new Page {Url = "/report-data/registration-task-list?registrationyear=2026", Name = "Registration Task List Page"},
            new Page {Url = "/report-data/cso-registration?nation=England", Name = "CSO Registration Page"},
            new Page {Url = "/report-data/file-upload", Name = "POM File Upload Page"},
            new Page {Url = "/report-data/file-upload-check-file-and-submit", Name = "POM File Upload Success Page"},
            new Page {Url = "/report-data/check-warnings", Name = "POM File Upload Warnings Page"},
            new Page {Url = "/report-data/file-upload-failure", Name = "POM File Upload Errors Page"},
            new Page {Url = "/report-data/privacy", Name = "Privacy Page"},
            new Page {Url = "/report-data/cookies", Name = "Cookies Page"},
            new Page {Url = "/report-data/subsidiaries-list", Name = "Subsidiaries Page"},
            new Page {Url = "/report-data/view-awaiting-acceptance", Name = "PRNs Page"},
            new Page {Url = "/report-data/resubmission-task-list", Name = "Resubmission Task List Page"},
            new Page {Url = "/report-data/resubmission-fee-calculations", Name = "Resubmission Fee Calculations Page"},
            new Page {Url = "/report-data/file-upload-submission-confirmation", Name = "POM Submission Confirmation Page"}
        ];
    }
    
    public class Page
    {
        public string Url { get; set; }
        public string Name { get; set; }
    }
}