namespace ReservasSaaS.Domain.Exceptions;

/// <summary>
/// Thrown when entity or command validation fails.
/// </summary>
public class ValidationException : DomainException
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException(string message)
        : base(message, "VALIDATION_FAILED")
    {
        Errors = new Dictionary<string, string[]>
        {
            { "General", new[] { message } }
        };
    }

    public ValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("One or more validation failures occurred.", "VALIDATION_FAILED")
    {
        Errors = errors;
    }
}
