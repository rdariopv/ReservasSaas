using Google.Api.Gax;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReservasSaaS.Infrastructure.Persistence.Firestore.Configuration;
using ReservasSaaS.Infrastructure.Persistence.Firestore.Interfaces;

namespace ReservasSaaS.Infrastructure.Persistence.Firestore;

/// <summary>
/// Singleton provider managing the FirestoreDb client instance.
/// Supports Local Emulator, GCP Application Default Credentials, and service account files.
/// </summary>
public class FirestoreProvider : IFirestoreProvider
{
    private readonly FirestoreOptions _options;
    private readonly ILogger<FirestoreProvider> _logger;
    private readonly Lazy<FirestoreDb> _lazyDatabase;

    public string ProjectId => _options.ProjectId;
    public string DatabaseId => _options.DatabaseId;
    public bool IsEmulator => _options.IsEmulatorConfigured;

    public FirestoreProvider(
        IOptions<FirestoreOptions> options,
        ILogger<FirestoreProvider> logger)
    {
        _options = options.Value;
        _logger = logger;

        _lazyDatabase = new Lazy<FirestoreDb>(InitializeDatabase);
    }

    public FirestoreDb GetDatabase() => _lazyDatabase.Value;

    private FirestoreDb InitializeDatabase()
    {
        _logger.LogInformation("Initializing FirestoreDb client for ProjectId '{ProjectId}', DatabaseId '{DatabaseId}'...",
            _options.ProjectId, _options.DatabaseId);

        // Check if emulator is configured
        if (!string.IsNullOrWhiteSpace(_options.EmulatorHost))
        {
            Environment.SetEnvironmentVariable("FIRESTORE_EMULATOR_HOST", _options.EmulatorHost);
            _logger.LogWarning("FIRESTORE_EMULATOR_HOST configured to '{EmulatorHost}'", _options.EmulatorHost);
        }

        var builder = new FirestoreDbBuilder
        {
            ProjectId = _options.ProjectId,
            DatabaseId = _options.DatabaseId,
            EmulatorDetection = _options.IsEmulatorConfigured 
                ? EmulatorDetection.EmulatorOrProduction 
                : EmulatorDetection.None
        };

        // Explicit JSON credentials path (optional override; default is Application Default Credentials / GOOGLE_APPLICATION_CREDENTIALS)
        if (!string.IsNullOrWhiteSpace(_options.CredentialsPath))
        {
            if (File.Exists(_options.CredentialsPath))
            {
                _logger.LogInformation("Loading Firestore credentials from configured path: {CredentialsPath}", _options.CredentialsPath);
                builder.CredentialsPath = _options.CredentialsPath;
            }
            else
            {
                _logger.LogWarning("Configured CredentialsPath '{CredentialsPath}' does not exist on disk. Falling back to Application Default Credentials (ADC).", _options.CredentialsPath);
            }
        }

        try
        {
            var db = builder.Build();
            _logger.LogInformation("FirestoreDb client built successfully. Target Project: {ProjectId}, EmulatorMode: {IsEmulator}",
                _options.ProjectId, IsEmulator);
            return db;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize FirestoreDb client. Verify Google credentials or emulator settings.");
            throw;
        }
    }

    public async Task<bool> CanConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var db = GetDatabase();
            // Perform lightweight metadata read or collection reference probe
            //var testCollection = db.Collection("__health_probe__");
            var testCollection = db.Collection("healthChecks");
            var query = testCollection.Limit(1);
            var snapshot = await query.GetSnapshotAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Firestore connectivity probe failed for ProjectId '{ProjectId}'", _options.ProjectId);
            return false;
        }
    }
}
