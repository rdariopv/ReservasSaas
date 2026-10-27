using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using ReservasSaaS.Domain.Enums;
using ReservasSaaS.Domain.Ports.Outgoing;
using ReservasSaaS.Infrastructure.Persistence.Firestore.Interfaces;

namespace ReservasSaaS.Infrastructure.Adapters;

/// <summary>
/// Infrastructure adapter implementing ISystemHealthCheckPort to diagnose OS, runtime, and Firestore connectivity.
/// </summary>
public class SystemHealthCheckAdapter : ISystemHealthCheckPort
{
    private readonly IServiceProvider _serviceProvider;

    public SystemHealthCheckAdapter(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<HealthProbeResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        var process = Process.GetCurrentProcess();
        var memoryAllocatedMb = process.WorkingSet64 / (1024.0 * 1024.0);
        var threadCount = process.Threads.Count;

        var data = new Dictionary<string, object>
        {
            ["OsDescription"] = RuntimeInformation.OSDescription,
            ["OsArchitecture"] = RuntimeInformation.OSArchitecture.ToString(),
            ["ProcessArchitecture"] = RuntimeInformation.ProcessArchitecture.ToString(),
            ["DotNetFrameworkVersion"] = RuntimeInformation.FrameworkDescription,
            ["ProcessorCount"] = Environment.ProcessorCount,
            ["WorkingSetMemoryMB"] = Math.Round(memoryAllocatedMb, 2),
            ["ThreadCount"] = threadCount
        };

        var overallStatus = HealthStatus.Healthy;
        var firestoreStatus = "NotConfigured";

        var firestoreProvider = _serviceProvider.GetService<IFirestoreProvider>();
        if (firestoreProvider != null && !string.IsNullOrWhiteSpace(firestoreProvider.ProjectId))
        {
            var canConnect = await firestoreProvider.CanConnectAsync(cancellationToken);
            firestoreStatus = canConnect ? "Connected" : "Disconnected";
            data["Firestore_ProjectId"] = firestoreProvider.ProjectId;
            data["Firestore_DatabaseId"] = firestoreProvider.DatabaseId;
            data["Firestore_IsEmulator"] = firestoreProvider.IsEmulator;
            data["Firestore_Status"] = firestoreStatus;

            if (!canConnect)
            {
                overallStatus = HealthStatus.Degraded;
            }
        }
        else
        {
            data["Firestore_Status"] = "Unconfigured";
        }

        sw.Stop();

        var description = overallStatus == HealthStatus.Healthy
            ? "Underlying operating system, runtime, and persistence adapters are responsive."
            : "System is degraded: Firestore connection probe failed.";

        return new HealthProbeResult(
            Status: overallStatus,
            Description: description,
            Duration: sw.Elapsed,
            Data: data
        );
    }
}
