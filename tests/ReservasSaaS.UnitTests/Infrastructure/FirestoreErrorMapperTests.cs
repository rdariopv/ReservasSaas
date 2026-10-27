using FluentAssertions;
using Grpc.Core;
using ReservasSaaS.Domain.Exceptions;
using ReservasSaaS.Infrastructure.Persistence.Firestore.Errors;
using Xunit;

namespace ReservasSaaS.UnitTests.Infrastructure;

public class FirestoreErrorMapperTests
{
    [Fact]
    public void Map_WhenStatusCodeIsNotFound_ReturnsEntityNotFoundException()
    {
        // Arrange
        var rpcException = new RpcException(new Status(StatusCode.NotFound, "Document not found"));

        // Act
        var mapped = FirestoreErrorMapper.Map(rpcException, "Court", "court-123");

        // Assert
        mapped.Should().BeOfType<EntityNotFoundException>();
        var notFoundEx = (EntityNotFoundException)mapped;
        notFoundEx.EntityName.Should().Be("Court");
        notFoundEx.EntityKey.Should().Be("court-123");
        notFoundEx.Code.Should().Be("ENTITY_NOT_FOUND");
    }

    [Fact]
    public void Map_WhenStatusCodeIsAlreadyExists_ReturnsPersistenceException()
    {
        // Arrange
        var rpcException = new RpcException(new Status(StatusCode.AlreadyExists, "Document already exists"));

        // Act
        var mapped = FirestoreErrorMapper.Map(rpcException, "Tenant", "tenant-alpha");

        // Assert
        mapped.Should().BeOfType<PersistenceException>();
        mapped.Message.Should().Contain("already exists");
    }

    [Fact]
    public void Map_WhenStatusCodeIsAborted_ReturnsConcurrencyException()
    {
        // Arrange
        var rpcException = new RpcException(new Status(StatusCode.Aborted, "Transaction aborted due to concurrent modification"));

        // Act
        var mapped = FirestoreErrorMapper.Map(rpcException, "ReservationSlot", "slot-456");

        // Assert
        mapped.Should().BeOfType<ConcurrencyException>();
        var concurrencyEx = (ConcurrencyException)mapped;
        concurrencyEx.Code.Should().Be("CONCURRENCY_CONFLICT");
    }

    [Fact]
    public void Map_WhenStatusCodeIsPermissionDenied_ReturnsPersistenceExceptionWithGuidance()
    {
        // Arrange
        var rpcException = new RpcException(new Status(StatusCode.PermissionDenied, "Missing IAM permission"));

        // Act
        var mapped = FirestoreErrorMapper.Map(rpcException, "PaymentConfig", "config-1");

        // Assert
        mapped.Should().BeOfType<PersistenceException>();
        mapped.Message.Should().Contain("Permission denied");
    }

    [Fact]
    public void Map_WhenStatusCodeIsUnavailable_ReturnsPersistenceException()
    {
        // Arrange
        var rpcException = new RpcException(new Status(StatusCode.Unavailable, "Transport endpoint is not connected"));

        // Act
        var mapped = FirestoreErrorMapper.Map(rpcException, "Facility", "fac-1");

        // Assert
        mapped.Should().BeOfType<PersistenceException>();
        mapped.Message.Should().Contain("temporarily unavailable");
    }
}
