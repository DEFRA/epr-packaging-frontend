using FluentAssertions;
using FrontendSchemeRegistration.Application.DTOs;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Services.RegistrationPeriods;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace FrontendSchemeRegistration.UI.UnitTests.Services.RegistrationPeriods;

[TestFixture]
public class RegistrationPeriodProviderWarmupServiceTests
{
    private Mock<IServiceScopeFactory> _scopeFactoryMock = null!;
    private Mock<IServiceScope> _scopeMock = null!;
    private Mock<IServiceProvider> _scopeServiceProviderMock = null!;
    private Mock<IPaymentCalculationService> _paymentCalculationServiceMock = null!;
    private Mock<IRegistrationPeriodProvider> _registrationPeriodProviderMock = null!;
    private Mock<ILogger<RegistrationPeriodProviderWarmupService>> _loggerMock = null!;
    private FakeTimeProvider _timeProvider = null!;
    private RegistrationPeriodProviderWarmupService _sut = null!;

    [SetUp]
    public void Setup()
    {
        _paymentCalculationServiceMock = new Mock<IPaymentCalculationService>();
        _registrationPeriodProviderMock = new Mock<IRegistrationPeriodProvider>();
        _loggerMock = new Mock<ILogger<RegistrationPeriodProviderWarmupService>>();
        _timeProvider = new FakeTimeProvider();

        _scopeServiceProviderMock = new Mock<IServiceProvider>();
        _scopeServiceProviderMock
            .Setup(sp => sp.GetService(typeof(IPaymentCalculationService)))
            .Returns(_paymentCalculationServiceMock.Object);

        _scopeMock = new Mock<IServiceScope>();
        _scopeMock.Setup(s => s.ServiceProvider).Returns(_scopeServiceProviderMock.Object);

        _scopeFactoryMock = new Mock<IServiceScopeFactory>();
        _scopeFactoryMock.Setup(f => f.CreateScope()).Returns(_scopeMock.Object);

        _sut = new RegistrationPeriodProviderWarmupService(
            _scopeFactoryMock.Object,
            _registrationPeriodProviderMock.Object,
            _loggerMock.Object,
            _timeProvider);
    }

    [TearDown]
    public async Task TearDown()
    {
        await _sut.StopAsync(CancellationToken.None);
        _sut.Dispose();
    }

    [Test]
    public async Task ExecuteAsync_PeriodsReturned_LoadsIntoProviderAndExits()
    {
        var periods = new[]
        {
            new SubmissionPeriodDetails { Id = 1, WindowType = "Cso", RegistrationYear = 2025 },
            new SubmissionPeriodDetails { Id = 3, WindowType = "CsoLargeProducer", RegistrationYear = 2026 }
        };
        _paymentCalculationServiceMock.Setup(s => s.GetSubmissionPeriods()).ReturnsAsync(periods);

        await _sut.StartAsync(CancellationToken.None);
        await _sut.ExecuteTask!;

        _registrationPeriodProviderMock.Verify(
            p => p.Load(It.Is<IEnumerable<SubmissionPeriodDetails>>(x => x.SequenceEqual(periods))),
            Times.Once);
        VerifyLog(LogLevel.Error, Times.Never());
    }

    [Test]
    public async Task ExecuteAsync_EmptyPeriods_LoadsEmptyAndExits()
    {
        _paymentCalculationServiceMock.Setup(s => s.GetSubmissionPeriods()).ReturnsAsync(Array.Empty<SubmissionPeriodDetails>());

        await _sut.StartAsync(CancellationToken.None);
        await _sut.ExecuteTask!;

        _registrationPeriodProviderMock.Verify(
            p => p.Load(It.Is<IEnumerable<SubmissionPeriodDetails>>(x => !x.Any())),
            Times.Once);
        VerifyLog(LogLevel.Error, Times.Never());
    }

    [Test]
    public async Task ExecuteAsync_PaymentServiceThrows_LogsErrorAndSchedulesRetry()
    {
        var firstCallCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var callCount = 0;
        _paymentCalculationServiceMock.Setup(s => s.GetSubmissionPeriods())
            .Returns(() =>
            {
                if (Interlocked.Increment(ref callCount) == 1)
                {
                    firstCallCompleted.TrySetResult();
                }

                throw new InvalidOperationException("boom");
            });

        await _sut.StartAsync(CancellationToken.None);
        await firstCallCompleted.Task;
        await _sut.StopAsync(CancellationToken.None);

        _registrationPeriodProviderMock.Verify(
            p => p.Load(It.IsAny<IEnumerable<SubmissionPeriodDetails>>()),
            Times.Never);
        VerifyLog(LogLevel.Error, Times.AtLeastOnce());
    }

    [Test]
    public async Task StopAsync_CancelsRetryLoopCleanly()
    {
        _paymentCalculationServiceMock.Setup(s => s.GetSubmissionPeriods())
            .ReturnsAsync((SubmissionPeriodDetails[]?)null);

        await _sut.StartAsync(CancellationToken.None);
        var stopTask = _sut.StopAsync(CancellationToken.None);
        await stopTask;

        stopTask.IsCompletedSuccessfully.Should().BeTrue();
    }

    [Test]
    public async Task ExecuteAsync_FailsThenSucceeds_LoadsExactlyOnceAndExits()
    {
        var periods = new[]
        {
            new SubmissionPeriodDetails { Id = 1, WindowType = "Cso", RegistrationYear = 2025 },
        };
        var firstCallCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var callCount = 0;
        _paymentCalculationServiceMock.Setup(s => s.GetSubmissionPeriods())
            .Returns(() =>
            {
                var n = Interlocked.Increment(ref callCount);
                if (n == 1)
                {
                    firstCallCompleted.TrySetResult();
                    throw new InvalidOperationException("transient");
                }

                return Task.FromResult<SubmissionPeriodDetails[]?>(periods);
            });

        await _sut.StartAsync(CancellationToken.None);
        await firstCallCompleted.Task;

        // Let the loop reach Task.Delay before advancing the fake clock; without a public
        // "timer scheduled" hook on FakeTimeProvider this brief yield is the pragmatic option.
        await Task.Delay(50);
        _timeProvider.Advance(TimeSpan.FromSeconds(30));

        await _sut.ExecuteTask!;

        _registrationPeriodProviderMock.Verify(
            p => p.Load(It.Is<IEnumerable<SubmissionPeriodDetails>>(x => x.SequenceEqual(periods))),
            Times.Once);
    }

    private void VerifyLog(LogLevel level, Times times)
    {
        _loggerMock.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times);
    }
}
