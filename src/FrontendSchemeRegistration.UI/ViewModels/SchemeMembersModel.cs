using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.UI.ViewModels
{
    [ExcludeFromCodeCoverage]
    public class SchemeMembersModel
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string MemberCount { get; set; }

        public string LastUpdated { get; set; }

        public string SearchText { get; set; }

        public string ResetLink { get; set; }

        public PagingDetail PagingDetail { get; set; } = new();

        public List<(string ReferenceNumber, string Name, string Link)> MemberList { get; set; } = new();
    }
}
