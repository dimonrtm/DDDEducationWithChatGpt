using IntroductionToDDD.Domain.DomainEvents;
using IntroductionToDDD.Domain.Enums;
using IntroductionToDDD.Domain.Exceptions;
using IntroductionToDDD.Domain.Policies;
using IntroductionToDDD.Domain.ValueObjects;

namespace IntroductionToDDD.Domain.Entities
{
    public sealed class ReservationQueue
    {
        public Guid CopyId { get; }
        public Guid? ActiveReservationId { get; private set; }
        public int Version { get; private set; }

        private readonly List<QueueEntry> _entries = new();
        private long _seq; // стабильный FIFO-счётчик для равных приоритетов
        private readonly List<object> _changes = new();

        public IReadOnlyList<QueueEntry> Entries => _entries.AsReadOnly();
        public QueueEntry? Head => _entries.Count > 0 ? _entries[0] : null;

        public ReservationQueue(Guid copyId)
        {
            if (copyId == Guid.Empty) throw new DomainException("Empty copyId");
            CopyId = copyId;
            Version = 0;
            _seq = 0;
        }
        public void Place(Guid reservationId, Guid readerId, PriorityLevel priorityLevel, DateTimeOffset waitDeadline, IClock clock)
        {
            if (reservationId == Guid.Empty) throw new DomainException("Empty reservationId");
            if (readerId == Guid.Empty) throw new DomainException("Empty readerId");

            if (_entries.Any(e => e.ReservationId == reservationId)) return;

            var now = clock.Now();
            var entry = new QueueEntry(reservationId, readerId, priorityLevel, waitDeadline, now, _seq++);

            var idx = FindInsertIndex(entry);
            _entries.Insert(idx, entry);

            Version++;
            Raise(new ReservationQueued(CopyId, reservationId, readerId, (int)priorityLevel, waitDeadline, now, Version));

            if (idx == 0)
                Raise(new QueueHeadChanged(CopyId, reservationId, now, Version));

        }

        public void AuthorizeActivation(Guid reservationId, ILoanPolicy loanPolicy, IClock clock)
        {
            if (Head is null || Head.ReservationId != reservationId)
                throw new DomainException("Only head of queue can be activated");

            if (ActiveReservationId != null)
                throw new DomainException("There is already an active loan for this copy");

            if (loanPolicy.HasActiveLoan(CopyId))
                throw new DomainException("Copy already has an active loan per policy");

            var now = clock.Now();
            if (now > Head.WaitDeadline)
                throw new DomainException("Head reservation wait deadline expired");

            _entries.RemoveAt(0);
            ActiveReservationId = reservationId;
            Version++;

            Raise(new LoanActivationAuthorized(CopyId, reservationId, Head?.ReaderId ?? Guid.Empty, now, Version));

            if (_entries.Count > 0)
                Raise(new QueueHeadChanged(CopyId, _entries[0].ReservationId, now, Version));
        }

        public void ExpireOverdue(IClock clock)
        {
            var now = clock.Now();
            var changedHead = false;

            while (Head != null && now > Head.WaitDeadline)
            {
                var removed = Head!;
                _entries.RemoveAt(0);
                Version++;
                Raise(new WaitDeadlineExpiredFromQueue(CopyId, removed.ReservationId, removed.ReaderId, removed.WaitDeadline, now, Version));
                changedHead = true;
            }

            if (changedHead && _entries.Count > 0)
                Raise(new QueueHeadChanged(CopyId, _entries[0].ReservationId, now, Version));
        }

        public void Remove(Guid reservationId, CancelReason reason, CanceledBy canceledBy, IClock clock, string? note = null)
        {
            if (ActiveReservationId == reservationId)
                throw new DomainException("Cannot remove active reservation from queue");

            var idx = _entries.FindIndex(e => e.ReservationId == reservationId);
            if (idx < 0) return;

            var removed = _entries[idx];
            _entries.RemoveAt(idx);
            Version++;

            var now = clock.Now();
            Raise(new ReservationRemovedFromQueue(CopyId, reservationId, removed.ReaderId, reason.ToString(), canceledBy.ToString(), note, now, Version));

            if (idx == 0 && _entries.Count > 0)
                Raise(new QueueHeadChanged(CopyId, _entries[0].ReservationId, now, Version));
        }

        public void ReleaseActive(Guid reservationId, IClock clock)
        {
            if (ActiveReservationId != reservationId) return; // идемпотентность
            ActiveReservationId = null;
            Version++;
            Raise(new ActiveLoanCleared(CopyId, reservationId, clock.Now(), Version));

            if (_entries.Count > 0)
                Raise(new QueueHeadChanged(CopyId, _entries[0].ReservationId, clock.Now(), Version));
        }

        public IReadOnlyCollection<object> DequeueUncommittedEvents()
        {
            var copy = _changes.ToArray();
            _changes.Clear();
            return copy;
        }

        private int FindInsertIndex(QueueEntry entry)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                var e = _entries[i];
                if (entry.PriorityLevel < e.PriorityLevel) return i;      // более высокий приоритет — раньше
                if (entry.PriorityLevel == e.PriorityLevel && entry.Seq < e.Seq) return i; // FIFO внутри приоритета
            }
            return _entries.Count;
        }

        private void Raise(object @event) => _changes.Add(@event);
    }
}
