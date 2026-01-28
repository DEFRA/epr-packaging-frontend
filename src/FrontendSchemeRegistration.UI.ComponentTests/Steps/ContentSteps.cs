namespace FrontendSchemeRegistration.UI.ComponentTests.Steps;

using FluentAssertions;
using Infrastructure;
using Reqnroll;

[Binding]
public class ContentSteps(ScenarioContext context)
{
    [Then("the page content includes the following: (.*)")]
    public void ThenThePageContentIncludesTheFollowing(string expectedContent)
    {
        var response = context.Get<string>(ContextKeys.HttpResponseContent);
        
        response.Should().Contain(expectedContent);
    }
    
    [Then("the page redirect content includes the following: (.*)")]
    public void ThenThePageRedirectContentIncludesTheFollowing(string expectedContent)
    {
        var response = context.Get<string>(ContextKeys.HttpResponseRedirectContent);
        
        response.Should().Contain(expectedContent);
    }
}