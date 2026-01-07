namespace FrontendSchemeRegistration.Application.Options.ReistrationPeriodPatterns;

public class RegistrationPeriodPattern
{
    public const string ConfigSection = "RegistrationPeriodPatterns";
    public int InitialRegistrationYear { get; set; }
    public int? FinalRegistrationYear { get; set; }
    public IEnumerable<Window> Windows { get; set; }
}