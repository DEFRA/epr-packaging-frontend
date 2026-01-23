namespace FrontendSchemeRegistration.UI.ComponentTests.Data;

public class Pages
{

    public static List<Page> GetPages()
    {
        return 
        [
            new Page {Url = "/report-data/home-compliance-scheme", Name = "Compliance Scheme Landing Page"},
            new Page {Url = "/report-data", Name = "Report Data Page"},
            new Page {Url = "/services/account-details", Name = "Login Page"}
        ];
    }
    
    public class Page
    {
        public string Url { get; set; }
        public string Name { get; set; }
    }
}