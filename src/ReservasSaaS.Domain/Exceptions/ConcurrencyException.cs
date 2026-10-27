namespace ReservasSaaS.Domain.Exceptions;

/// <summary>
/// Thrown when an optimistic concurrency conflict occurs during document update.
/// </summary>
public class ConcurrencyException : DomainException
{
    public ConcurrencyException(string message) 
        : base(message, "CONCURRENCY_CONFLICT")
    {
    }

    public ConcurrencyException(string message, Exception innerException) 
        : base(message, innerException, "CONCURRENCY_CONFLICT")
    {
    }
}
