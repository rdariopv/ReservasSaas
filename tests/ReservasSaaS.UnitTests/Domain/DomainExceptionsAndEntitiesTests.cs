using FluentAssertions;
using ReservasSaaS.Domain.Common;
using ReservasSaaS.Domain.Exceptions;
using Xunit;

namespace ReservasSaaS.UnitTests.Domain;

public class TestEntity : BaseEntity<Guid>, IAggregateRoot
{
    public string Name { get; private set; }

    public TestEntity(Guid id, string name) : base(id)
    {
        Name = name;
    }
}

public class DomainExceptionsAndEntitiesTests
{
    [Fact]
    public void ValidationException_WithSingleMessage_InitializesGeneralErrorMap()
    {
        // Act
        var ex = new ValidationException("Invalid reservation input parameter.");

        // Assert
        ex.Code.Should().Be("VALIDATION_FAILED");
        ex.Errors.Should().ContainKey("General");
        ex.Errors["General"].Should().Contain("Invalid reservation input parameter.");
    }

    [Fact]
    public void ValidationException_WithDictionary_PreservesAllFields()
    {
        // Arrange
        var errors = new Dictionary<string, string[]>
        {
            ["Email"] = new[] { "Invalid format", "Required" },
            ["Amount"] = new[] { "Must be greater than 0" }
        };

        // Act
        var ex = new ValidationException(errors);

        // Assert
        ex.Errors.Should().HaveCount(2);
        ex.Errors["Email"].Should().HaveCount(2);
    }

    [Fact]
    public void EntityNotFoundException_FormatsDescriptiveMessageAndCode()
    {
        // Act
        var ex = new EntityNotFoundException("Reservation", "RES-9988");

        // Assert
        ex.Code.Should().Be("ENTITY_NOT_FOUND");
        ex.EntityName.Should().Be("Reservation");
        ex.EntityKey.Should().Be("RES-9988");
        ex.Message.Should().Contain("Reservation");
        ex.Message.Should().Contain("RES-9988");
    }

    [Fact]
    public void BaseEntity_WhenSameId_AreEqual()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity1 = new TestEntity(id, "Court A");
        var entity2 = new TestEntity(id, "Court A Modified");

        // Assert
        entity1.Should().Be(entity2);
        (entity1 == entity2).Should().BeTrue();
    }

    [Fact]
    public void BaseEntity_WhenDifferentId_AreNotEqual()
    {
        // Arrange
        var entity1 = new TestEntity(Guid.NewGuid(), "Court A");
        var entity2 = new TestEntity(Guid.NewGuid(), "Court A");

        // Assert
        entity1.Should().NotBe(entity2);
        (entity1 != entity2).Should().BeTrue();
    }
}
