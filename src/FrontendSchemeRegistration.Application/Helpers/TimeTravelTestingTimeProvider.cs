namespace FrontendSchemeRegistration.Application.Helpers;

public class TimeTravelTestingTimeProvider : TimeProvider
{
    private readonly TimeSpan _offset;

    public TimeTravelTestingTimeProvider(DateTime initialUtcDateTime)
    {
        var currentUtcDateTime = DateTime.UtcNow;
        _offset = currentUtcDateTime - initialUtcDateTime;
    }

    public override DateTimeOffset GetUtcNow()
    {
        return base.GetUtcNow() - _offset;
    }
}