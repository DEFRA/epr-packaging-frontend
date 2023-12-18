namespace FrontendSchemeRegistration.UI.TagHelpers;

using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

[HtmlTargetElement("label", Attributes = ForAttributeName, TagStructure = TagStructure.NormalOrSelfClosing)]
public class LabelTagHelper : TagHelper
{
    private const string ForAttributeName = "gov-for";
    private const string ValueAttributeName = "gov-value";
    private const string FirstOptionAttributeName = "gov-first-option";

    [HtmlAttributeName(ForAttributeName)]
    public ModelExpression? For { get; set; }

    [HtmlAttributeName(ValueAttributeName)]
    public string? Value { get; set; }

    [HtmlAttributeName(FirstOptionAttributeName)]
    public bool IsFirstOption { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (For != null)
        {
            if (Value != null && !IsFirstOption)
            {
                output.Attributes.SetAttribute("for", For.Name + "-" + Value);
            }
            else
            {
                output.Attributes.SetAttribute("for", For.Name);
            }
        }
    }
}
