namespace FrontendSchemeRegistration.UI.RequestModels;

using System.Diagnostics.CodeAnalysis;
using Attributes.Validation;
using Resources;

[ExcludeFromCodeCoverage]
public class DeclarationRequest
{
    [MustTrue(ErrorMessageResourceName = "declaration_not_approved", ErrorMessageResourceType = typeof(ErrorMessages))]
    public bool Approved { get; set; }
}