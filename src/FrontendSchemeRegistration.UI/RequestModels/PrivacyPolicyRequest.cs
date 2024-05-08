namespace FrontendSchemeRegistration.UI.RequestModels;

using System.Diagnostics.CodeAnalysis;
using Attributes.Validation;
using Resources;

[ExcludeFromCodeCoverage]
public class PrivacyPolicyRequest
{
    [MustTrue(ErrorMessageResourceName = "privacy_policy_not_approved", ErrorMessageResourceType = typeof(ErrorMessages))]
    public bool Approved { get; set; }
}