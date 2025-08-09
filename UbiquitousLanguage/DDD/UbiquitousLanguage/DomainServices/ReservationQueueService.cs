using System.Reflection;
using UbiquitousLanguage.Repositories;

namespace UbiquitousLanguage.DomainServices
{
    public class ReservationQueueService
    {
        private readonly IReservationRepository _reservations;
        private readonly IReaderRepository _readers;
        private readonly int _maxActivePerReader;

        public ReservationQueueService(IReservationRepository reservations, IReaderRepository readers, int maxActivePerReader)
        {
            _reservations = reservations;
            _readers = readers;
            _maxActivePerReader = maxActivePerReader;
        }

        public async Task<Reservation> EnqueueAsync(BookCopyId copyId, ReaderId readerId, ReservationPriority priority)
        {
            var count = await _readers.CountActiveReservationsAsync(readerId);
            if (count >= _maxActivePerReader)
                throw new InvalidOperationException("Reader reached reservation limit.");
            var active = (await _reservations.GetActiveByCopyAsync(copyId)).ToList();         // упорядочены по Position
            var nextPos = (active.Count == 0) ? 1 : active.Max(r => r.Position) + 1;

            var reservation = CreateReservation(copyId, readerId, priority);        // фабрика агрегата
            await _reservations.AddAsync(reservation);
            active = active
             .OrderBy(r => r.Priority)
             .ThenBy(r => r.CreatedAt)
              .ToList();
            for (int i = 0; i < active.Count; i++)
                active[i].SetPosition(i + 1);
            await _reservations.UpsertRangeAsync(active);
            return reservation;
        }

        public async Task PromoteAsync(BookCopyId copyId)
        {
            var active = (await _reservations.GetActiveByCopyAsync(copyId))
                .OrderBy(r => r.Priority)
                .ThenBy(r => r.CreatedAt)
                .ToList();

            if (active.Count == 0) return;
            var first = active[0];

            if (first.Status == ReservationStatus.Created)      // “подошла очередь”
                first.Activate();

            // Сдвиг позиций после выдачи:
            // вызов после first.Fulfill() или Cancel() — переиндексация:
            active = active.Where(r => r.Status is ReservationStatus.Created or ReservationStatus.Active)
                   .OrderBy(r => r.Priority)
                   .ThenBy(r => r.CreatedAt)
                   .ToList();

            for (int i = 0; i < active.Count; i++)
                active[i].SetPosition(i + 1);

            // persist...
            await _reservations.UpsertRangeAsync(active);
        }

        private static Reservation CreateReservation(BookCopyId copyId, ReaderId readerId, ReservationPriority priority)
        {
            var ctor = typeof(Reservation).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { typeof(ReservationId), typeof(BookCopyId), typeof(ReaderId), typeof(ReservationPriority), typeof(int) },
                null)!;

            return (Reservation)ctor.Invoke(new object?[] { new ReservationId(Guid.NewGuid()), copyId, readerId, priority, 0 });
        }
    }
}
