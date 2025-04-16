using FrontendSchemeRegistration.Application.DTOs.Subsidiary.OrganisationSubsidiaryList;
using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.UI.ViewModels;

[ExcludeFromCodeCoverage]
public class AllSubsidiaryListViewModel
{
    public List<RelationshipResponseModel> Subsidiaries { get; set; }

    public PagingDetail PagingDetail { get; set; } = new();

    public bool IsDirectProducer { get; set; }

    public bool IsFileUploadInProgress { get; set; }

    public string? SearchTerm { get; set; }

    public List<string> TypeAhead { get; set; } = [];
}
