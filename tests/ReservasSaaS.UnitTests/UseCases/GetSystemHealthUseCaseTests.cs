using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ReservasSaaS.Application.UseCases;
using ReservasSaaS.Domain.Enums;
using ReservasSaaS.Domain.Ports.Outgoing;
using Xunit;

namespace ReservasSaaS.UnitTests.UseCases;

public class GetSystemHealthUseCaseTests
{
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock;
    private readonly Mock<ISystemHealthCheckPort> _healthCheckPortMock;
    private readonly Mock<ILogger<GetSystemHealthUseCase>> _loggerMock;
    private readonly GetSystemHealthUseCase _sut;

    public GetSystemHealthUseCaseTests()
    {
        _dateTimeProviderMock = new Mock<IDateTimeProvider>();
        _healthCheckPortMock = new Mock<ISystemHealthCheckPort>();
        _loggerMock = new Mock<ILogger<GetSystemHealthUseCase>>();

        _dateTimeProviderMock.Setup(d => d.UtcNow).Returns(new DateTime(2026, 10, 17, 12, 0, 0, DateTimeKind.Utc));

        _sut = new GetSystemHealthUseCase(
            _dateTimeProviderMock.Object,
            _healthCheckPortMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUnderlyingPortIsHealthy_ReturnsHealthyStatusAndSubsystems()
    {
        // Arrange
        var probeResult = new HealthProbeResult(
            Status: HealthStatus.Healthy,
            Description: "OS test probe is healthy",
            Duration: TimeSpan.FromMilliseconds(5),
            Data: new Dictionary<string, object> { ["CpuCount"] = 8 }
        );

        _healthCheckPortMock
            .Setup(p => p.CheckHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(probeResult);

        // Act
        var result = await _sut.ExecuteAsync(CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be("Healthy");
        result.Architecture.Should().Contain("Hexagonal");
        result.Subsystems.Should().ContainKey("OperatingSystem");
        result.Subsystems.Should().ContainKey("HexagonalPorts");
        result.Subsystems["OperatingSystem"].Status.Should().Be("Healthy");
        result.Subsystems["OperatingSystem"].Description.Should().Be("OS test probe is healthy");
    }

    [Fact]
    public async Task ExecuteAsync_WhenUnderlyingPortIsDegraded_ReturnsDegradedStatus()
    {
        // Arrange
        var probeResult = new HealthProbeResult(
            Status: HealthStatus.Degraded,
            Description: "High memory utilization",
            Duration: TimeSpan.FromMilliseconds(20),
            Data: new Dictionary<string, object> { ["MemoryUsage"] = "95%" }
        );

        _healthCheckPortMock
            .Setup(p => p.CheckHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(probeResult);

        // Act
        var result = await _sut.ExecuteAsync(CancellationToken.None);

        // Assert
        result.Status.Should().Be("Degraded");
        result.Subsystems["OperatingSystem"].Status.Should().Be("Degraded");
    }
}
