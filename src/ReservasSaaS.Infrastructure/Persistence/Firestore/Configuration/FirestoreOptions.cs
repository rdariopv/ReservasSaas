namespace ReservasSaaS.Infrastructure.Persistence.Firestore.Configuration;

/// <summary>
/// Strongly-typed configuration options for Firebase Cloud Firestore integration.
/// Credentials are NEVER stored directly in code or repositories.
/// Supported modes:
/// 1. Cloud Run / GCP: Application Default Credentials (ADC) / Workload Identity.
/// 2. Local Emulator: FIRESTORE_EMULATOR_HOST or EmulatorHost setting.
/// 3. Service Account JSON file path: configured via GOOGLE_APPLICATION_CREDENTIALS or CredentialsPath.
/// </summary>
public class FirestoreOptions
{
    public const string SectionName = "Firestore";

    /// <summary>
    /// Google Cloud / Firebase Project ID (e.g., "reservas-saas-dev").
    /// </summary>
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>
    /// Firestore Database ID. Defaults to "(default)".
    /// </summary>
    public string DatabaseId { get; set; } = "(default)";

    /// <summary>
    /// Host for Firestore Local Emulator (e.g., "localhost:8080" or "127.0.0.1:8080").
    /// </summary>
    public string? EmulatorHost { get; set; }

    /// <summary>
    /// Optional relative or absolute path to a service account JSON file.
    /// Recommended to use GOOGLE_APPLICATION_CREDENTIALS environment variable instead.
    /// </summary>
    public string? CredentialsPath { get; set; }

    /// <summary>
    /// Indicates if emulator host is active either via config or FIRESTORE_EMULATOR_HOST environment variable.
    /// </summary>
    public bool IsEmulatorConfigured =>
        !string.IsNullOrWhiteSpace(EmulatorHost) ||
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("FIRESTORE_EMULATOR_HOST"));
}
