namespace FrontendSchemeRegistration.UI.UnitTests.TagHelpers;

using System.Text.Encodings.Web;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using UI.TagHelpers;

[TestFixture]
public class InputTagHelperTests
{
    private static TagHelperContext CreateContext()
    {
        return new TagHelperContext(
            new TagHelperAttributeList(),
            new Dictionary<object, object>(),
            Guid.NewGuid().ToString());
    }

    private static TagHelperOutput CreateOutput(string tagName = "input")
    {
        return new TagHelperOutput(
            tagName,
            new TagHelperAttributeList(),
            (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
    }

    private static ModelExpression CreateModelExpression(string name, object model)
    {
        IModelMetadataProvider provider = new EmptyModelMetadataProvider();
        var modelExplorer = provider.GetModelExplorerForType(model?.GetType() ?? typeof(string), model);
        return new ModelExpression(name, modelExplorer);
    }

    [Test]
    public void Process_WhenForIsNull_DoesNotSetAnyAttributes()
    {
        var sut = new InputTagHelper { For = null };
        var output = CreateOutput();

        sut.Process(CreateContext(), output);

        output.Attributes.Should().BeEmpty();
    }

    [Test]
    public void Process_WhenValueIsNull_SetsIdNameAndTypeOnly()
    {
        var sut = new InputTagHelper
        {
            For = CreateModelExpression("MyProperty", "some-model"),
            Type = "text",
            Value = null
        };
        var output = CreateOutput();

        sut.Process(CreateContext(), output);

        output.Attributes["id"].Value.Should().Be("MyProperty");
        output.Attributes["name"].Value.Should().Be("MyProperty");
        output.Attributes["type"].Value.Should().Be("text");
        output.Attributes.ContainsName("value").Should().BeFalse();
    }

    [Test]
    public void Process_WhenTypeIsNotRadio_DoesNotOverrideId()
    {
        var sut = new InputTagHelper
        {
            For = CreateModelExpression("MyProperty", "some-model"),
            Type = "text",
            Value = "some-value"
        };
        var output = CreateOutput();

        sut.Process(CreateContext(), output);

        output.Attributes["id"].Value.Should().Be("MyProperty");
        output.Attributes["value"].Value.Should().Be("some-value");
        output.Attributes.ContainsName("checked").Should().BeFalse();
    }

    [Test]
    public void Process_WhenTypeIsRadioAndNotFirstOption_OverridesIdWithValueSuffix()
    {
        var sut = new InputTagHelper
        {
            For = CreateModelExpression("MyProperty", "other-value"),
            Type = "radio",
            Value = "option-1",
            IsFirstOption = false
        };
        var output = CreateOutput();

        sut.Process(CreateContext(), output);

        output.Attributes["id"].Value.Should().Be("MyProperty-option-1");
    }

    [Test]
    public void Process_WhenTypeIsRadioAndIsFirstOption_DoesNotOverrideId()
    {
        var sut = new InputTagHelper
        {
            For = CreateModelExpression("MyProperty", "other-value"),
            Type = "radio",
            Value = "option-1",
            IsFirstOption = true
        };
        var output = CreateOutput();

        sut.Process(CreateContext(), output);

        output.Attributes["id"].Value.Should().Be("MyProperty");
    }

    [Test]
    public void Process_WhenTypeIsRadioAndModelMatchesValue_SetsCheckedAttribute()
    {
        var sut = new InputTagHelper
        {
            For = CreateModelExpression("MyProperty", "option-1"),
            Type = "radio",
            Value = "option-1",
            IsFirstOption = true
        };
        var output = CreateOutput();

        sut.Process(CreateContext(), output);

        output.Attributes["checked"].Value.Should().Be("checked");
    }

    [Test]
    public void Process_WhenTypeIsRadioAndModelDoesNotMatchValue_DoesNotSetCheckedAttribute()
    {
        var sut = new InputTagHelper
        {
            For = CreateModelExpression("MyProperty", "option-2"),
            Type = "radio",
            Value = "option-1",
            IsFirstOption = true
        };
        var output = CreateOutput();

        sut.Process(CreateContext(), output);

        output.Attributes.ContainsName("checked").Should().BeFalse();
    }
}
