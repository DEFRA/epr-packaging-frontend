namespace FrontendSchemeRegistration.UI.TagHelpers;

using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

[HtmlTargetElement("input", Attributes = ForAttributeName, TagStructure = TagStructure.WithoutEndTag)]
public class InputTagHelper : TagHelper
{
    private const string ForAttributeName = "gov-for";
    private const string ValueAttributeName = "gov-value";
    private const string TypeAttributeName = "type";
    private const string FirstOptionAttributeName = "gov-first-option";

    private const string RadioType = "radio";
    private const string IdAttributeKey = "id";
    private const string NameAttributeKey = "name";
    private const string TypeAttributeKey = "type";
    private const string ValueAttributeKey = "value";
    private const string CheckedAttribute = "checked";

    [HtmlAttributeName(ForAttributeName)]
    public ModelExpression? For { get; set; }

    [HtmlAttributeName(ValueAttributeName)]
    public string? Value { get; set; }

    [HtmlAttributeName(TypeAttributeName)]
    public string? Type { get; set; }

    [HtmlAttributeName(FirstOptionAttributeName)]
    public bool IsFirstOption { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (For != null)
        {
            output.Attributes.SetAttribute(IdAttributeKey, For.Name);
            output.Attributes.SetAttribute(NameAttributeKey, For.Name);
            output.Attributes.SetAttribute(TypeAttributeKey, Type);

            if (Value != null)
            {
                output.Attributes.SetAttribute(ValueAttributeKey, Value);

                if (Type == RadioType && !IsFirstOption)
                {
                    output.Attributes.SetAttribute(IdAttributeKey, For.Name + "-" + Value);
                }

                if (Type == RadioType && For.Model?.ToString() == Value)
                {
                    output.Attributes.SetAttribute(CheckedAttribute, CheckedAttribute);
                }
            }
        }
    }
}