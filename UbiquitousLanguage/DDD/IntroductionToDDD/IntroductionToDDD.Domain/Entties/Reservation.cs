using IntroductionToDDD.Domain.Enums;
using IntroductionToDDD.Domain.Policies;

namespace IntroductionToDDD.Domain.Entties
{
    public sealed class Reservation
    {
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
            var profile = acl.Classify(readerId);
            if (!profile.Eligible) throw new DomainException("Reader ineligible");

            // Инвариант: не более одной активной выдачи на экземпляр
            if (loanPolicy.HasActiveLoan(copyId))
                throw new DomainException("Copy already on active loan");

            var res = new Reservation
            {
                Id = id,
                ReaderId = readerId,
                CopyId = copyId,
                Status = ReservationStatus.Pending,
                PriorityLevel = profile.PriorityLevel,
                WaitDeadline = clock.Now().Add(waitPolicy.WaitDuration())
            };

            res.Raise(new BookReserved(/* заполни поля */));
            return res;
        }

        public void ActivateLoan(ILoanPolicy loanPolicy, IClock clock)
        {
            if (Status != ReservationStatus.Pending)
                throw new DomainException("Only pending can activate");

            // TODO: повторная проверка активной выдачи перед активацией
            // if ( ... ) throw ...

            Status = ReservationStatus.ActiveLoan;
            Raise(new LoanActivated(/* заполни поля */));
        }

        public void ExpireWait(IClock clock)
        {
            if (Status != ReservationStatus.Pending) return; // идемпотентность
            if (WaitDeadline is null || clock.Now() <= WaitDeadline.Value) return;

            Status = ReservationStatus.Canceled;
            Raise(new WaitDeadlineExpired(/* occurredAt, reservationId, readerId, copyId, waitDeadline */));
        }

        public void Cancel(string reason, string canceledBy, string? note = null)
        {
            if (Status == ReservationStatus.Canceled) return; // идемпотентность
            Status = ReservationStatus.Canceled;
            Raise(new ReservationCanceled(/* + reason, canceledBy, note */));
        }

        private void Raise(object @event) { /* outbox/changes */ }
    }
}
