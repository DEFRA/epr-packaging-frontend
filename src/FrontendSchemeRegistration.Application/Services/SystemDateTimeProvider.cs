namespace FrontendSchemeRegistration.Application.Services;

using System.Diagnostics.CodeAnalysis;
using Interfaces;

[ExcludeFromCodeCoverage]
public class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}