using ReservasSaaS.Domain.Enums;

namespace ReservasSaaS.Application.DTOs.Agenda;

public record DepositPolicyDto(
    bool RequiresDeposit,
    string Type,
    decimal Value,
    string Description);

public record ServiceDto(
    Guid Id,
    Guid CompanyId,
    string Name,
    string Description,
    int DurationMinutes,
    int BufferBeforeMinutes,
    int BufferAfterMinutes,
    int TotalOccupiedMinutes,
    decimal Price,
    string Currency,
    ServiceStatus Status,
    DepositPolicyDto DepositPolicy,
    DateTime CreatedAtUtc);

public record CreateServiceCommand(
    string Name,
    string Description,
    int DurationMinutes,
    decimal Price,
    string Currency = "COP",
    int BufferBeforeMinutes = 0,
    int BufferAfterMinutes = 0,
    bool RequiresDeposit = false,
    DepositType DepositType = DepositType.None,
    decimal DepositValue = 0,
    string DepositDescription = "");

public record UpdateServiceCommand(
    string Name,
    string Description,
    int DurationMinutes,
    decimal Price,
    string Currency,
    int BufferBeforeMinutes,
    int BufferAfterMinutes,
    bool RequiresDeposit,
    DepositType DepositType,
    decimal DepositValue,
    string DepositDescription,
    ServiceStatus Status);
