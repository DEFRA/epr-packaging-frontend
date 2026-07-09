using FluentAssertions;
using FrontendSchemeRegistration.Application.DTOs;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Services.RegistrationPeriods;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
    private RegistrationPeriodProviderWarmupService _sut = null!;

    [SetUp]
    public void Setup()
    {
        _paymentCalculationServiceMock = new Mock<IPaymentCalculationService>();
        _registrationPeriodProviderMock = new Mock<IRegistrationPeriodProvider>();
        _loggerMock = new Mock<ILogger<RegistrationPeriodProviderWarmupService>>();

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
            _loggerMock.Object);
    }

    [Test]
    public async Task StartAsync_PeriodsReturned_LoadsIntoProvider()
    {
        var periods = new[]
        {
            new SubmissionPeriodDetails { Id = 1, WindowType = "Cso", RegistrationYear = 2025 },
            new SubmissionPeriodDetails { Id = 3, WindowType = "CsoLargeProducer", RegistrationYear = 2026 }
        };
        _paymentCalculationServiceMock.Setup(s => s.GetSubmissionPeriods()).ReturnsAsync(periods);

        await _sut.StartAsync(CancellationToken.None);

        _registrationPeriodProviderMock.Verify(
            p => p.Load(It.Is<IEnumerable<SubmissionPeriodDetails>>(x => x.SequenceEqual(periods))),
            Times.Once);
        VerifyLog(LogLevel.Error, Times.Never());
    }

    [Test]
    public async Task StartAsync_NullPeriods_LogsErrorAndSkipsLoad()
    {
        _paymentCalculationServiceMock.Setup(s => s.GetSubmissionPeriods()).ReturnsAsync((SubmissionPeriodDetails[]?)null);

        await _sut.StartAsync(CancellationToken.None);

        _registrationPeriodProviderMock.Verify(p => p.Load(It.IsAny<IEnumerable<SubmissionPeriodDetails>>()), Times.Never);
        VerifyLog(LogLevel.Error, Times.Once());
    }

    [Test]
    public async Task StartAsync_EmptyPeriods_LogsErrorAndSkipsLoad()
    {
        _paymentCalculationServiceMock.Setup(s => s.GetSubmissionPeriods()).ReturnsAsync(Array.Empty<SubmissionPeriodDetails>());

        await _sut.StartAsync(CancellationToken.None);

        _registrationPeriodProviderMock.Verify(p => p.Load(It.IsAny<IEnumerable<SubmissionPeriodDetails>>()), Times.Never);
        VerifyLog(LogLevel.Error, Times.Once());
    }

    [Test]
    public async Task StartAsync_PaymentServiceThrows_LogsErrorAndDoesNotBubble()
    {
        _paymentCalculationServiceMock.Setup(s => s.GetSubmissionPeriods()).ThrowsAsync(new InvalidOperationException("boom"));

        Func<Task> act = () => _sut.StartAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
        _registrationPeriodProviderMock.Verify(p => p.Load(It.IsAny<IEnumerable<SubmissionPeriodDetails>>()), Times.Never);
        VerifyLog(LogLevel.Error, Times.Once());
    }

    [Test]
    public async Task StopAsync_ReturnsCompletedTask()
    {
        var task = _sut.StopAsync(CancellationToken.None);

        await task;
        task.IsCompletedSuccessfully.Should().BeTrue();
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
