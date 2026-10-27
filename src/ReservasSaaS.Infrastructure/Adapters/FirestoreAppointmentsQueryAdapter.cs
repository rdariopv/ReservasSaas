using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;
using ReservasSaaS.Domain.Enums;
using ReservasSaaS.Domain.Ports.Outgoing;
using ReservasSaaS.Infrastructure.Persistence.Firestore.Interfaces;

namespace ReservasSaaS.Infrastructure.Adapters;

/// <summary>
/// Secondary port adapter that queries existing booked intervals from Cloud Firestore.
/// Enforces real capacity constraints across all booking channels (WhatsApp, Flutter, Web).
/// </summary>
public class FirestoreAppointmentsQueryAdapter : IExistingAppointmentsQueryPort
{
    private readonly IFirestoreProvider _firestoreProvider;
    private readonly ILogger<FirestoreAppointmentsQueryAdapter> _logger;

    public FirestoreAppointmentsQueryAdapter(
        IFirestoreProvider firestoreProvider,
        ILogger<FirestoreAppointmentsQueryAdapter> logger)
    {
        _firestoreProvider = firestoreProvider;
        _logger = logger;
    }

    public async Task<IReadOnlyList<TimeInterval>> GetBusyIntervalsAsync(
        Guid companyId, 
        Guid professionalId, 
        DateOnly date, 
        CancellationToken cancellationToken = default)
    {
        var compStr = companyId.ToString("N");
        var profStr = professionalId.ToString("N");
        var startOfDayUtc = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endOfDayUtc = date.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var intervals = new List<TimeInterval>();

        try
        {
            var db = _firestoreProvider.GetDatabase();
            var bookingsCollection = db.Collection("bookings");

            var query = bookingsCollection
                .WhereEqualTo("CompanyId", compStr)
                .WhereEqualTo("ProfessionalId", profStr);

            var snapshot = await query.GetSnapshotAsync(cancellationToken);

            var inactiveStatuses = new HashSet<int>
            {
                (int)BookingStatus.Cancelled,
                (int)BookingStatus.Expired,
                (int)BookingStatus.Rejected,
                (int)BookingStatus.NoShow
            };

            foreach (var doc in snapshot.Documents)
            {
                if (doc.ContainsField("Status") && inactiveStatuses.Contains(doc.GetValue<int>("Status")))
                    continue;

                DateTime? start = null;
                DateTime? end = null;

                if (doc.ContainsField("ScheduledStartTimeUtc") && doc.ContainsField("ScheduledEndTimeUtc"))
                {
                    start = doc.GetValue<DateTime>("ScheduledStartTimeUtc");
                    end = doc.GetValue<DateTime>("ScheduledEndTimeUtc");
                }
                else if (doc.ContainsField("StartUtc") && doc.ContainsField("EndUtc"))
                {
                    start = doc.GetValue<DateTime>("StartUtc");
                    end = doc.GetValue<DateTime>("EndUtc");
                }

                if (start.HasValue && end.HasValue && start.Value >= startOfDayUtc && start.Value <= endOfDayUtc)
                {
                    intervals.Add(new TimeInterval(start.Value, end.Value, doc.Id));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not query busy intervals from Firestore for Professional '{ProfessionalId}'. Assuming empty schedule.", profStr);
        }

        return intervals;
    }
}
