namespace FrontendSchemeRegistration.UI.Component.UnitTests.Steps;

using System.Diagnostics.CodeAnalysis;
using Infrastructure;
using Reqnroll;

[ExcludeFromCodeCoverage]
[Binding]
public class AuthSteps(ScenarioContext context)
{
    [Given("I am logged in with email (.*)")]
    public async Task GivenIAmLoggedInWithEmail(string email)
    {
        var client = context.Get<ITestHttpClient>(ContextKeys.ComponentTestClient);
        await TestEnvironmentManagement.AuthenticateWithEmail(client, email);
    }
}
