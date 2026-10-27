using FluentAssertions;
using ReservasSaaS.Domain.Entities;
using ReservasSaaS.Domain.Enums;
using ReservasSaaS.Domain.Exceptions;
using ReservasSaaS.Domain.ValueObjects;
using Xunit;

namespace ReservasSaaS.UnitTests.Agenda;

public class ServiceDomainTests
{
    [Fact]
    public void Constructor_WhenDurationIsZeroOrNegative_ThrowsValidationException()
    {
        var act = () => new Service(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Clase de Tenis",
            "Clase individual",
            durationMinutes: 0,
            price: 50000
        );

        act.Should().Throw<ValidationException>().WithMessage("*greater than zero*");
    }

    [Fact]
    public void DepositPolicy_WhenPercentageExceeds100_ThrowsValidationException()
    {
        var act = () => new DepositPolicy(
            requiresDeposit: true,
            type: DepositType.Percentage,
            value: 120,
            description: "Invalid deposit"
        );

        act.Should().Throw<ValidationException>().WithMessage("*cannot exceed 100%*");
    }

    [Fact]
    public void DepositPolicy_CalculateDepositAmount_CalculatesCorrectly()
    {
        var policyFixed = new DepositPolicy(true, DepositType.FixedAmount, 15000);
        var policyPercent = new DepositPolicy(true, DepositType.Percentage, 30);
        var policyNone = DepositPolicy.None;

        policyFixed.CalculateDepositAmount(50000).Should().Be(15000);
        policyPercent.CalculateDepositAmount(50000).Should().Be(15000); // 30% of 50000
        policyNone.CalculateDepositAmount(50000).Should().Be(0);
    }

    [Fact]
    public void Service_TotalOccupiedMinutes_SumsDurationAndBuffers()
    {
        var service = new Service(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Sesión Fisioterapia",
            "Rehabilitación",
            durationMinutes: 45,
            price: 80000,
            bufferBeforeMinutes: 5,
            bufferAfterMinutes: 10
        );

        service.TotalOccupiedMinutes.Should().Be(60); // 45 + 5 + 10
    }
}
