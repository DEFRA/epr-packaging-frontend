namespace FrontendSchemeRegistration.UI.UnitTests.TagHelpers;

using FluentAssertions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using UI.TagHelpers;

[TestFixture]
public class LabelTagHelperTests
{
    private static TagHelperContext CreateContext()
    {
        return new TagHelperContext(
            new TagHelperAttributeList(),
            new Dictionary<object, object>(),
            Guid.NewGuid().ToString());
    }

    private static TagHelperOutput CreateOutput(string tagName = "label")
    {
        return new TagHelperOutput(
            tagName,
            new TagHelperAttributeList(),
            (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
    }

    private static ModelExpression CreateModelExpression(string name)
    {
        IModelMetadataProvider provider = new EmptyModelMetadataProvider();
        var modelExplorer = provider.GetModelExplorerForType(typeof(string), "some-model");
        return new ModelExpression(name, modelExplorer);
    }

    [Test]
    public void Process_WhenForIsNull_DoesNotSetForAttribute()
    {
        var sut = new LabelTagHelper { For = null };
        var output = CreateOutput();

        sut.Process(CreateContext(), output);

        output.Attributes.ContainsName("for").Should().BeFalse();
    }

    [Test]
    public void Process_WhenValueSetAndNotFirstOption_SetsForWithValueSuffix()
    {
        var sut = new LabelTagHelper
        {
            For = CreateModelExpression("MyProperty"),
            Value = "option-1",
            IsFirstOption = false
        };
        var output = CreateOutput();

        sut.Process(CreateContext(), output);

        output.Attributes["for"].Value.Should().Be("MyProperty-option-1");
    }

    [Test]
    public void Process_WhenValueSetAndIsFirstOption_SetsForWithoutSuffix()
    {
        var sut = new LabelTagHelper
        {
            For = CreateModelExpression("MyProperty"),
            Value = "option-1",
            IsFirstOption = true
        };
        var output = CreateOutput();

        sut.Process(CreateContext(), output);

        output.Attributes["for"].Value.Should().Be("MyProperty");
    }

    [Test]
    public void Process_WhenValueIsNull_SetsForWithoutSuffix()
    {
        var sut = new LabelTagHelper
        {
            For = CreateModelExpression("MyProperty"),
            Value = null,
            IsFirstOption = false
        };
        var output = CreateOutput();

        sut.Process(CreateContext(), output);

        output.Attributes["for"].Value.Should().Be("MyProperty");
    }
}
