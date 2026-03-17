namespace FrontendSchemeRegistration.UI.Component.UnitTests.Infrastructure;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Session;

public class SessionStore : ISessionStore
{
    public readonly Session Session = new();

    public ISession Create(
        string sessionKey, TimeSpan idleTimeout, TimeSpan ioTimeout,
        Func<bool> tryEstablishSession,
        bool isNewSessionKey) => Session;
}