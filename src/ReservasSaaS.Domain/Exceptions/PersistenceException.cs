namespace ReservasSaaS.Domain.Exceptions;

/// <summary>
/// Thrown when a persistence, infrastructure connection, or database operation fails.
/// </summary>
public class PersistenceException : DomainException
{
    public PersistenceException(string message) 
        : base(message, "PERSISTENCE_ERROR")
    {
    }

    public PersistenceException(string message, Exception innerException) 
        : base(message, innerException, "PERSISTENCE_ERROR")
    {
    }
}
