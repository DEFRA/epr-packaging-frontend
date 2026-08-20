using Microsoft.AspNetCore.Html;
using FrontendSchemeRegistration.Application.Enums;

namespace FrontendSchemeRegistration.Application.Extensions
{
    public static class NationExtensions
    {
        public static string GetNationName(string nationcode) => nationcode switch
        {
            "GB-ENG" => Nation.England.ToString(),
            "GB-SCT" => Nation.Scotland.ToString(),
            "GB-NIR" => Nation.NorthernIreland.ToString(),
            "GB-WLS" => Nation.Wales.ToString(),
            _ => ""
        };

		public static string GetNationNameFromId(int nationId) => nationId switch
		{
			(int)Nation.England => "GB-ENG",
			(int)Nation.Scotland => "GB-SCT",
			(int)Nation.Wales => "GB-WLS",
			(int)Nation.NorthernIreland => "GB-NIR",
			_ => ""
		};

        public static string GetEnvironmentAgencyName(string nationName) => nationName switch
        {
            "GB-ENG" => "Environment Agency",
            "GB-SCT" => "Scottish Environment Protection Agency",
            "GB-NIR" => "Northern Ireland Environment Agency",
            "GB-WLS" => "Natural Resources Wales",
            _ => ""
        };

        public static HtmlString GetEnvironmentAgencyEmailLink(int nationId) {  
            var mailToHref = nationId switch
            {
                (int)Nation.NorthernIreland => "packaging@daera-ni.gov.uk",
                (int)Nation.Scotland => "producer.responsibility@sepa.org.uk",
                (int)Nation.Wales => "packaging@naturalresourceswales.gov.uk",
                _ => "packagingproducers@environment-agency.gov.uk"
            };

            return new HtmlString(string.Format("<a class=\"govuk-link govuk-link--no-visited-state\" href=\"mailto:{0}\">{0}</a>", mailToHref));
        }
    }
}
