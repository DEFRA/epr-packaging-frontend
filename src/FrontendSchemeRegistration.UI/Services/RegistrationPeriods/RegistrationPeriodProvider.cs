namespace FrontendSchemeRegistration.UI.Services.RegistrationPeriods;

using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Application.DTOs;
using Application.Options.RegistrationPeriodPatterns;
using Constants;
using Extensions;

/// <summary>
/// This provides access to registration window data. It is registered as a singleton, and hydrated at
/// application startup by <see cref="RegistrationPeriodProviderWarmupService"/> from the payment service
/// submission-periods lookup (via the payment facade).
/// </summary>
internal class RegistrationPeriodProvider : IRegistrationPeriodProvider
{
    private readonly TimeProvider _timeProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private IReadOnlyCollection<RegistrationWindow> _registrationWindows = [];
    private int? _loadedYear;

    private readonly object _lock = new();

    public RegistrationPeriodProvider(
        TimeProvider timeProvider,
        IHttpContextAccessor httpContextAccessor)
    {
        _timeProvider = timeProvider;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Replaces the internal window collection from the supplied submission-period rows. Called by the
    /// warmup hosted service at startup, and again on year rollover.
    /// </summary>
    public void Load(IEnumerable<SubmissionPeriodDetails> submissionPeriods)
    {
        ArgumentNullException.ThrowIfNull(submissionPeriods);

        var windows = submissionPeriods
            .Where(p => Enum.TryParse<WindowType>(p.WindowType, ignoreCase: true, out _))
            .Select(p => new RegistrationWindow(
                _timeProvider,
                p.Id,
                Enum.Parse<WindowType>(p.WindowType, ignoreCase: true),
                p.RegistrationYear,
                p.OpeningDate,
                p.DeadlineDate,
                p.ClosingDate))
            .OrderByDescending(w => w.RegistrationYear)
            .ToList();

        lock (_lock)
        {
            _registrationWindows = windows;
            _loadedYear = _timeProvider.GetUtcNow().Year;
        }
    }

    /// <summary>
    /// Reports whether the internal collection has been hydrated. Consumers should treat a false result
    /// as a transient startup condition.
    /// </summary>
    public bool IsLoaded => _registrationWindows.Count > 0;

    /// <summary>
    /// Gets active registration windows. These include closed windows, as producers may still register
    /// beyond the closing date
    /// </summary>
    /// <param name="isCso"></param>
    /// <returns></returns>
    public IReadOnlyCollection<RegistrationWindow> GetActiveRegistrationWindows(bool isCso)
    {
        var orderedWindows = _registrationWindows
            .Where(w => w.IsCso == isCso && w.GetRegistrationWindowStatus() != RegistrationWindowStatus.PriorToOpening)
            .OrderByDescending(ra => ra.RegistrationYear);

        return new ReadOnlyCollection<RegistrationWindow>(orderedWindows.ToList());
    }

    /// <summary>
    /// Returns all registration windows, whether they are closed or in the future. Will return
    /// a future window if that window's opening date is this year
    /// </summary>
    /// <param name="isCso"></param>
    /// <returns>All past, current or future registration windows</returns>
    public IReadOnlyCollection<RegistrationWindow> GetAllRegistrationWindows(bool isCso)
    {
        var orderedWindows = _registrationWindows
            .Where(w => w.IsCso == isCso)
            .OrderByDescending(ra => ra.RegistrationYear);

        return new ReadOnlyCollection<RegistrationWindow>(orderedWindows.ToList());
    }

    /// <inheritdoc />
    public RegistrationWindow GetRegistrationWindow(bool isCso, bool isSmallProducer, int registrationYear)
    {
        var windows = _registrationWindows.Where(w => w.IsCso == isCso && w.RegistrationYear == registrationYear);

        var count = windows.Count();

        return count switch
        {
            // if there are more than one windows, then use the size to filter
            > 1 => isSmallProducer
                ? windows.Single(w => w.WindowType is WindowType.CsoSmallProducer or WindowType.DirectSmallProducer)
                : windows.Single(w => w.WindowType is WindowType.CsoLargeProducer or WindowType.DirectLargeProducer),
            1 => windows.Single(),
            _ => throw new InvalidOperationException(
                $"Cannot find a registration window for arguments isCso: {isCso}, isSmallProducer: {isSmallProducer}, registrationYear: {registrationYear}")
        };
    }

    /// <inheritdoc />
    public int? ValidateRegistrationYear(string? registrationYear, bool isParamOptional = false)
    {
        bool nullRegYear = string.IsNullOrWhiteSpace(registrationYear);

        switch (nullRegYear)
        {
            case true when isParamOptional:
                return null;
            case true:
                throw new ArgumentException("Registration year missing");
        }

        if (!int.TryParse(registrationYear, out var parsedYear)) throw new ArgumentException("Registration year is not a valid number");

        // is user's org a CSO?
        var org = _httpContextAccessor.HttpContext.User.GetUserData().Organisations.FirstOrDefault();

        if (org == null) throw new InvalidOperationException("The user must have an organisation.");

        var isCso = org.OrganisationRole == OrganisationRoles.ComplianceScheme;

        // check windows.
        var regWindows = GetActiveRegistrationWindows(isCso);

        if (regWindows.Any(w => w.RegistrationYear == parsedYear))
        {
            return parsedYear;
        }
        else
        {
            throw new ArgumentException("Invalid registration year");
        }
    }
}
