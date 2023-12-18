namespace FrontendSchemeRegistration.UI.RequestModels;

using Attributes.Validation;
using Resources;

public class DeclarationRequest
{
    [MustTrue(ErrorMessageResourceName = "declaration_not_approved", ErrorMessageResourceType = typeof(ErrorMessages))]
    public bool Approved { get; set; }
}