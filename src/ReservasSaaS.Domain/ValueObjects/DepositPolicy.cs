using ReservasSaaS.Domain.Common;
using ReservasSaaS.Domain.Enums;
using ReservasSaaS.Domain.Exceptions;

namespace ReservasSaaS.Domain.ValueObjects;

/// <summary>
/// Value object defining advance payment / deposit requirements for a service.
/// </summary>
public class DepositPolicy : ValueObject
{
    public bool RequiresDeposit { get; private set; }
    public DepositType Type { get; private set; }
    public decimal Value { get; private set; }
    public string Description { get; private set; }

    public static DepositPolicy None => new(false, DepositType.None, 0, "No se requiere anticipo");

    private DepositPolicy()
    {
        Description = string.Empty;
    }

    public DepositPolicy(bool requiresDeposit, DepositType type, decimal value, string description = "")
    {
        if (requiresDeposit)
        {
            if (type == DepositType.None)
                throw new ValidationException("A deposit type must be specified when a deposit is required.");

            if (value <= 0)
                throw new ValidationException("Deposit value must be greater than zero.");

            if (type == DepositType.Percentage && value > 100)
                throw new ValidationException("Deposit percentage cannot exceed 100%.");
        }
        else
        {
            type = DepositType.None;
            value = 0;
        }

        RequiresDeposit = requiresDeposit;
        Type = type;
        Value = value;
        Description = description.Trim();
    }

    public decimal CalculateDepositAmount(decimal totalServicePrice)
    {
        if (!RequiresDeposit) return 0m;

        return Type switch
        {
            DepositType.FixedAmount => Math.Min(Value, totalServicePrice),
            DepositType.Percentage => Math.Round(totalServicePrice * (Value / 100m), 2),
            _ => 0m
        };
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return RequiresDeposit;
        yield return Type;
        yield return Value;
        yield return Description;
    }
}
