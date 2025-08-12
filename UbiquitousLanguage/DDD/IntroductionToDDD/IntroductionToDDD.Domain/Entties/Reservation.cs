using IntroductionToDDD.Domain.DomainEvents;
using IntroductionToDDD.Domain.Enums;
using IntroductionToDDD.Domain.Exceptions;
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

            res.Raise(new BookReserved(res.Id, res.ReaderId, res.CopyId, res.Status, res.PriorityLevel, res.WaitDeadline));
            return res;
        }

        public void ActivateLoan(ILoanPolicy loanPolicy, IClock clock)
        {
            if (Status != ReservationStatus.Pending)
                throw new DomainException("Only pending can activate");

            // TODO: повторная проверка активной выдачи перед активацией
            // if ( ... ) throw ...
            if (loanPolicy.HasActiveLoan(CopyId))
                throw new DomainException("Copy already on active loan");

            Status = ReservationStatus.ActiveLoan;
            Raise(new LoanActivated(Id, ReaderId, CopyId, Status, clock.Now()));
        }

        public void ExpireWait(IClock clock)
        {
            if (Status != ReservationStatus.Pending) return; // идемпотентность
            if (WaitDeadline is null || clock.Now() <= WaitDeadline.Value) return;

            Status = ReservationStatus.Canceled;
            Raise(new WaitDeadlineExpired(Id, ReaderId, CopyId, WaitDeadline, clock.Now()));
        }

        public void Cancel(string reason, string canceledBy, string? note = null)
        {
            if (Status == ReservationStatus.Canceled) return; // идемпотентность
            Status = ReservationStatus.Canceled;
            Raise(new ReservationCanceled(Id, ReaderId, CopyId, reason, note, DateTimeOffset.Now));
        }

        private void Raise(object @event) { /* outbox/changes */ }
    }
}
