using Google.Cloud.Firestore;
using Grpc.Core;
using ReservasSaaS.Domain.Exceptions;

namespace ReservasSaaS.Infrastructure.Persistence.Firestore.Errors;

/// <summary>
/// Centralized error mapper translating Google Cloud Firestore / gRPC exceptions into Domain and Persistence exceptions.
/// </summary>
public static class FirestoreErrorMapper
{

    public static Exception Map(
       Exception exception,
       string entityName,
       object? entityId = null,
       string? operation = null)
    {
        var operationText = string.IsNullOrWhiteSpace(operation)
            ? string.Empty
            : $" Operación: {operation}.";

        return new PersistenceException(
            $"Error al acceder a '{entityName}'. " +
            $"Identificador: '{entityId ?? "Unknown"}'." +
            operationText +
            $" Detalle: {exception.Message}",
            exception);
    }

    //public static Exception Map(Exception exception, string entityName, object? entityId = null)
    //{
    //    if (exception is RpcException rpcEx)
    //    {
    //        return MapRpcException(rpcEx, entityName, entityId);
    //    }

    //    if (exception is FirestoreException firestoreEx)
    //    {
    //        if (firestoreEx.InnerException is RpcException innerRpc)
    //        {
    //            return MapRpcException(innerRpc, entityName, entityId);
    //        }

    //        return new PersistenceException($"Firestore error while accessing {entityName}: {firestoreEx.Message}", firestoreEx);
    //    }

    //    return exception;
    //}

    private static Exception MapRpcException(RpcException rpcEx, string entityName, object? entityId)
    {
        return rpcEx.StatusCode switch
        {
            StatusCode.NotFound => new EntityNotFoundException(
                entityName, 
                entityId ?? "Unknown"),

            StatusCode.AlreadyExists => new PersistenceException(
                $"Entity '{entityName}' with identifier '{entityId}' already exists in Firestore.", 
                rpcEx),

            StatusCode.FailedPrecondition or StatusCode.Aborted => new ConcurrencyException(
                $"Optimistic concurrency violation or precondition failed in Firestore for '{entityName}'. Details: {rpcEx.Status.Detail}", 
                rpcEx),

            StatusCode.InvalidArgument => new ValidationException(
                $"Invalid argument provided to Firestore operation for '{entityName}': {rpcEx.Status.Detail}"),

            StatusCode.PermissionDenied => new PersistenceException(
                $"Permission denied accessing Firestore for '{entityName}'. Verify IAM roles (e.g. Cloud Datastore User) and security rules.", 
                rpcEx),

            StatusCode.Unauthenticated => new PersistenceException(
                "Firestore authentication failed. Ensure GOOGLE_APPLICATION_CREDENTIALS or Application Default Credentials (ADC) are valid.", 
                rpcEx),

            StatusCode.Unavailable => new PersistenceException(
                $"Firestore service is temporarily unavailable. Failed operation on '{entityName}'.", 
                rpcEx),

            StatusCode.DeadlineExceeded => new PersistenceException(
                $"Firestore request timed out while processing '{entityName}'.", 
                rpcEx),

            _ => new PersistenceException(
                $"Firestore gRPC operation failed for '{entityName}' with status code {rpcEx.StatusCode}: {rpcEx.Status.Detail}", 
                rpcEx)
        };
    }
}
