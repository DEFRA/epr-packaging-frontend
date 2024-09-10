using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.Options
{
    /// <summary>
    /// Allows the current date to be set to facilitate tesing of PRNs.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class PrnOptions
    {
        public int Year { get; set; }

        public int Month { get; set; }

        public int Day { get; set; }
    }
}