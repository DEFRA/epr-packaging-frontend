namespace FrontendSchemeRegistration.UI.RequestModels;

using Attributes.Validation;
using Resources;

public class PrivacyPolicyRequest
{
    [MustTrue(ErrorMessageResourceName = "privacy_policy_not_approved", ErrorMessageResourceType = typeof(ErrorMessages))]
    public bool Approved { get; set; }
}