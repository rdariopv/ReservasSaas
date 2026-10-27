namespace ReservasSaaS.Domain.Exceptions;

/// <summary>
/// Thrown when authentication credentials or token are missing, invalid, or expired.
/// </summary>
public class UnauthorizedException : DomainException
{
    public UnauthorizedException(string message = "User is not authenticated or the provided token is invalid.")
        : base(message, "UNAUTHORIZED")
    {
    }
}
