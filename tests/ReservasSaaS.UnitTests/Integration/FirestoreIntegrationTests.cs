using FluentAssertions;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ReservasSaaS.Domain.Common;
using ReservasSaaS.Infrastructure.Persistence.Firestore;
using ReservasSaaS.Infrastructure.Persistence.Firestore.Abstractions;
using ReservasSaaS.Infrastructure.Persistence.Firestore.Configuration;
using Xunit;

namespace ReservasSaaS.UnitTests.Integration;

#region Test Entity and Document
public class TenantSettingEntity : BaseEntity<string>, IAggregateRoot
{
    public string Name { get; private set; }
    public string Value { get; private set; }

    public TenantSettingEntity(string id, string name, string value) : base(id)
    {
        Name = name;
        Value = value;
    }

    public void UpdateValue(string newValue)
    {
        Value = newValue;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

[FirestoreData]
public class TenantSettingDocument : IFirestoreDocument
{
    [FirestoreDocumentId]
    public string Id { get; set; } = string.Empty;

    [FirestoreProperty]
    public string Name { get; set; } = string.Empty;

    [FirestoreProperty]
    public string Value { get; set; } = string.Empty;

    [FirestoreProperty]
    public DateTime CreatedAtUtc { get; set; }

    [FirestoreProperty]
    public DateTime? UpdatedAtUtc { get; set; }
}

public class TenantSettingRepository : FirestoreRepository<TenantSettingEntity, TenantSettingDocument, string>
{
    public override string CollectionName => "test_tenant_settings";

    public TenantSettingRepository(FirestoreProvider provider)
        : base(provider, NullLogger<TenantSettingRepository>.Instance)
    {
    }

    protected override string ToDocumentId(string id) => id;

    protected override TenantSettingDocument ToDocument(TenantSettingEntity entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Value = entity.Value,
        CreatedAtUtc = entity.CreatedAtUtc,
        UpdatedAtUtc = entity.UpdatedAtUtc
    };

    protected override TenantSettingEntity ToEntity(TenantSettingDocument document) =>
        new(document.Id, document.Name, document.Value);
}
#endregion

/// <summary>
/// Optional integration test suite against Firebase Cloud Firestore.
/// Uses either an external Firebase test project or local Firestore Emulator.
/// Gracefully skips when external credentials are not present in the environment.
/// </summary>
public class FirestoreIntegrationTests
{
    private readonly string? _testProjectId;
    private readonly bool _isConfigured;
    private readonly FirestoreOptions _options;

    public FirestoreIntegrationTests()
    {
        // Look for external Firebase Project ID or Firestore Emulator host
        _testProjectId = Environment.GetEnvironmentVariable("FIREBASE_TEST_PROJECT_ID")
                         ?? Environment.GetEnvironmentVariable("FIREBASE_PROJECT_ID")
                         ?? Environment.GetEnvironmentVariable("GOOGLE_CLOUD_PROJECT");

        var emulatorHost = Environment.GetEnvironmentVariable("FIRESTORE_EMULATOR_HOST");

        _isConfigured = !string.IsNullOrWhiteSpace(_testProjectId) || !string.IsNullOrWhiteSpace(emulatorHost);

        _options = new FirestoreOptions
        {
            ProjectId = _testProjectId ?? "demo-test-project",
            DatabaseId = "(default)",
            EmulatorHost = emulatorHost
        };
    }

    [Fact]
    public async Task Optional_Firestore_EndToEnd_Crud_Lifecycle()
    {
        if (!_isConfigured)
        {
            // Skip test gracefully if no external Firebase credentials / emulator are supplied
            Assert.True(true, "SKIPPED: Firebase integration test skipped. Set FIREBASE_TEST_PROJECT_ID or FIRESTORE_EMULATOR_HOST to execute against external Firebase.");
            return;
        }

        // Arrange
        var optionsWrapper = Options.Create(_options);
        var provider = new FirestoreProvider(optionsWrapper, NullLogger<FirestoreProvider>.Instance);
        var repository = new TenantSettingRepository(provider);

        var testId = $"test-{Guid.NewGuid():N}";
        var entity = new TenantSettingEntity(testId, "MaxReservationsPerUser", "5");

        try
        {
            // 1. Create (AddAsync)
            var created = await repository.AddAsync(entity);
            created.Should().NotBeNull();
            created.Id.Should().Be(testId);

            // 2. Read by ID (GetByIdAsync)
            var fetched = await repository.GetByIdAsync(testId);
            fetched.Should().NotBeNull();
            fetched!.Name.Should().Be("MaxReservationsPerUser");
            fetched.Value.Should().Be("5");

            // 3. Update (UpdateAsync)
            fetched.UpdateValue("10");
            await repository.UpdateAsync(fetched);

            var updatedFetched = await repository.GetByIdAsync(testId);
            updatedFetched.Should().NotBeNull();
            updatedFetched!.Value.Should().Be("10");

            // 4. Exists (ExistsAsync)
            var exists = await repository.ExistsAsync(testId);
            exists.Should().BeTrue();

            // 5. Delete (DeleteAsync)
            await repository.DeleteAsync(testId);

            var deletedFetched = await repository.GetByIdAsync(testId);
            deletedFetched.Should().BeNull();
        }
        finally
        {
            // Cleanup in case test failed before delete
            try
            {
                await repository.DeleteAsync(testId);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
