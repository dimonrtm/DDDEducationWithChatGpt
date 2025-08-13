using IntroductionToDDD.Domain.DomainEvents;
using IntroductionToDDD.Domain.Enums;
using IntroductionToDDD.Domain.Exceptions;
using IntroductionToDDD.Domain.Policies;

namespace IntroductionToDDD.Domain.Entities
{
    public sealed class Reservation
    {
        private readonly List<object> _changes = new();
        public Guid Id { get; private set; }
        public Guid ReaderId { get; private set; }
        public Guid CopyId { get; private set; }
        public ReservationStatus Status { get; private set; }
        public int PriorityLevel { get; private set; } // 0=staff,1=vip,2=regular
        public DateTimeOffset? WaitDeadline { get; private set; }
        public int Version { get; private set; }

        private Reservation() { }

        public static Reservation Place(
            Guid id, Guid readerId, Guid copyId,
            IReaderClassifier acl, ILoanPolicy loanPolicy,
            IWaitPolicy waitPolicy, IClock clock)
        {
            if (id == Guid.Empty) throw new DomainException("Empty id");
            if (readerId == Guid.Empty) throw new DomainException("Empty readerId");
            if (copyId == Guid.Empty) throw new DomainException("Empty copyId");
            var profile = acl.Classify(readerId);
            if (!profile.Eligible) throw new DomainException("Reader ineligible");

            // Инвариант: не более одной активной выдачи на экземпляр
            if (loanPolicy.HasActiveLoan(copyId))
                throw new DomainException("Copy already on active loan");

            var now = clock.Now();

            var res = new Reservation
            {
                Id = id,
                ReaderId = readerId,
                CopyId = copyId,
                Status = ReservationStatus.Pending,
                PriorityLevel = profile.PriorityLevel,
                WaitDeadline = now.Add(waitPolicy.WaitDuration()),
                Version = 0
            };

            res.Raise(new BookReserved(
                reservationId: res.Id,
                readerId: res.ReaderId,
                copyId: res.CopyId,
                status: res.Status,
                priorityLevel: res.PriorityLevel,
                waitDeadline: res.WaitDeadline,
                occurredAt: now,
                version: res.Version
            ));
            return res;
        }

        public void ActivateLoan(ILoanPolicy loanPolicy, IClock clock)
        {
            if (Status != ReservationStatus.Pending)
                throw new DomainException("Only pending can activate");

            var now = clock.Now();

            if (WaitDeadline is null || now > WaitDeadline.Value)
                throw new DomainException("Wait deadline expired");

            // TODO: повторная проверка активной выдачи перед активацией
            // if ( ... ) throw ...
            if (loanPolicy.HasActiveLoan(CopyId))
                throw new DomainException("Copy already on active loan");

            Status = ReservationStatus.ActiveLoan;
            Version++;
            Raise(new LoanActivated(
                reservationId: Id,
                readerId: ReaderId,
                copyId: CopyId,
                status: Status,
                occurredAt: now,
                version: Version
            ));
        }

        public void ExpireWait(IClock clock)
        {
            if (Status != ReservationStatus.Pending) return; // идемпотентность
            var now = clock.Now();
            if (WaitDeadline is null || now <= WaitDeadline.Value) return;

            Status = ReservationStatus.Canceled;
            Version++;
            Raise(new WaitDeadlineExpired(
                reservationId: Id,
                readerId: ReaderId,
                copyId: CopyId,
                waitDeadline: WaitDeadline,
                occurredAt: now,
                version: Version
            ));
        }

        public void Cancel(CancelReason reason, CanceledBy canceledBy, IClock clock, string? note = null)
        {
            if (Status == ReservationStatus.Canceled) return; // идемпотентность
            Status = ReservationStatus.Canceled;
            Version++;
            Raise(new ReservationCanceled(
                 reservationId: Id,
                 readerId: ReaderId,
                 copyId: CopyId,
                 reason: reason.ToString(),
                 canceledBy: canceledBy.ToString(),
                 note: note,
                 occurredAt: clock.Now(),
                 version: Version
             ));
        }

        public IReadOnlyCollection<object> DequeueUncommittedEvents()
        {
            var copy = _changes.ToArray();
            _changes.Clear();
            return copy;
        }

        private void Raise(object @event) => _changes.Add(@event);
    }
}
