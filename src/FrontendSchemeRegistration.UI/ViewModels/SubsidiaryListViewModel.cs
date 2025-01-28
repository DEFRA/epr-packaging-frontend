using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.UI.ViewModels;

[ExcludeFromCodeCoverage]
public class SubsidiaryListViewModel
{
    public List<SubsidiaryOrganisationViewModel> Organisations { get; set; }

    public PagingDetail PagingDetail { get; set; } = new();

    public bool IsDirectProducer { get; set; }

    public bool IsFileUploadInProgress { get; set; }

    public int MemberCount { get; set; }
}