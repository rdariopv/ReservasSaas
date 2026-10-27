namespace ReservasSaaS.Domain.Exceptions;

/// <summary>
/// Thrown when an aggregate root or entity cannot be found.
/// </summary>
public class EntityNotFoundException : DomainException
{
    public string EntityName { get; }
    public object EntityKey { get; }

    public EntityNotFoundException(string entityName, object entityKey)
        : base($"Entity '{entityName}' with key '{entityKey}' was not found.", "ENTITY_NOT_FOUND")
    {
        EntityName = entityName;
        EntityKey = entityKey;
    }
}
