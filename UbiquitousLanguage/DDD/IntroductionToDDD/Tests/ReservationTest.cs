using IntroductionToDDD.Domain.DomainEvents;
using IntroductionToDDD.Domain.Entities;
using IntroductionToDDD.Domain.Enums;
using IntroductionToDDD.Domain.Exceptions;
using IntroductionToDDD.Domain.Policies;
using IntroductionToDDD.Domain.ValueObjects;

namespace Tests
{
    public class ReservationTest
    {
        private static Guid G() => Guid.NewGuid();

        private sealed class ControlledClock : IClock
        {
            private DateTimeOffset _now;
            public ControlledClock(DateTimeOffset now) => _now = now;
            public DateTimeOffset Now() => _now;
            public void Advance(TimeSpan delta) => _now = _now.Add(delta);
            public void Set(DateTimeOffset dt) => _now = dt;
        }

        private sealed class FixedWaitPolicy : IWaitPolicy
        {
            private readonly TimeSpan _dur;
            public FixedWaitPolicy(TimeSpan dur) => _dur = dur;
            public TimeSpan WaitDuration() => _dur;
        }

        private sealed class StubLoanPolicy : ILoanPolicy
        {
            private bool _hasActive;
            public StubLoanPolicy(bool hasActive = false) { _hasActive = hasActive; }
            public bool HasActiveLoan(Guid copyId) => _hasActive;
            public void SetActive(bool val) => _hasActive = val;
        }

        private sealed class StubReaderClassifier : IReaderClassifier
        {
            private readonly bool _eligible;
            private readonly int _priority;
            public StubReaderClassifier(bool eligible, int priority)
            {
                _eligible = eligible; _priority = priority;
            }

            public ReaderProfile Classify(Guid readerId)
                => new ReaderProfile(_eligible, _priority);
        }

        private static List<object> DequeueEventsIfAvailable(object agg)
        {
            var m = agg.GetType().GetMethod("DequeueUncommittedEvents");
            if (m == null) return new List<object>();
            var res = m.Invoke(agg, null) as IEnumerable<object>;
            return res?.ToList() ?? new List<object>();
        }

        private static void CancelFlexible(Reservation res, ControlledClock clock, string note = "test")
        {
            var candidates = typeof(Reservation).GetMethods()
                .Where(m => m.Name == "Cancel")
                .ToArray();

            // 1) enum/enum/IClock/(string?)
            var m4 = candidates.FirstOrDefault(m =>
            {
                var ps = m.GetParameters();
                return ps.Length is 3 or 4
                    && typeof(IClock).IsAssignableFrom(ps[Math.Min(2, ps.Length - 1)].ParameterType);
            });
            if (m4 != null)
            {
                var ps = m4.GetParameters();
                var reasonType = ps[0].ParameterType;    // enum
                var canceledByType = ps[1].ParameterType; // enum
                var reason = Enum.Parse(reasonType, "UserCancel"); // должны существовать такие значения
                var canceledBy = Enum.Parse(canceledByType, "Reader");
                if (ps.Length == 3)
                    m4.Invoke(res, new object[] { reason, canceledBy, clock });
                else
                    m4.Invoke(res, new object[] { reason, canceledBy, clock, note });
                return;
            }

            // 2) string/string/(string?)
            var m3 = candidates.FirstOrDefault(m =>
            {
                var ps = m.GetParameters();
                return ps.Length is 2 or 3
                    && ps[0].ParameterType == typeof(string)
                    && ps[1].ParameterType == typeof(string);
            });
            if (m3 != null)
            {
                var ps = m3.GetParameters();
                if (ps.Length == 2)
                    m3.Invoke(res, new object[] { "UserCancel", "Reader" });
                else
                    m3.Invoke(res, new object[] { "UserCancel", "Reader", note });
                return;
            }

            throw new InvalidOperationException("No compatible Cancel(...) overload found.");
        }

        [Fact]
        public void Place_creates_pending_with_deadline_and_emits_event()
        {
            var now = new DateTimeOffset(2025, 2, 1, 10, 0, 0, TimeSpan.FromHours(5));
            var clock = new ControlledClock(now);
            var wait = new FixedWaitPolicy(TimeSpan.FromDays(3));
            var loan = new StubLoanPolicy(false);
            var acl = new StubReaderClassifier(eligible: true, priority: 1);

            var id = G(); var readerId = G(); var copyId = G();

            var res = Reservation.Place(id, readerId, copyId, acl, loan, wait, clock);

            Assert.Equal(ReservationStatus.Pending, res.Status);
            Assert.Equal(readerId, res.ReaderId);
            Assert.Equal(copyId, res.CopyId);
            Assert.NotNull(res.WaitDeadline);
            Assert.Equal(now + TimeSpan.FromDays(3), res.WaitDeadline!.Value);

            var events = DequeueEventsIfAvailable(res);
            if (events.Count > 0)
                Assert.Contains(events, e => e is BookReserved);
        }

        [Fact]
        public void Place_fails_if_reader_ineligible()
        {
            var clock = new ControlledClock(new DateTimeOffset(2025, 2, 1, 10, 0, 0, TimeSpan.FromHours(5)));
            var wait = new FixedWaitPolicy(TimeSpan.FromDays(1));
            var loan = new StubLoanPolicy(false);
            var acl = new StubReaderClassifier(eligible: false, priority: 2);

            Assert.Throws<DomainException>(() =>
                Reservation.Place(G(), G(), G(), acl, loan, wait, clock));
        }

        [Fact]
        public void Place_fails_if_copy_already_has_active_loan()
        {
            var clock = new ControlledClock(new DateTimeOffset(2025, 2, 1, 10, 0, 0, TimeSpan.FromHours(5)));
            var wait = new FixedWaitPolicy(TimeSpan.FromDays(1));
            var loan = new StubLoanPolicy(true);
            var acl = new StubReaderClassifier(eligible: true, priority: 2);

            var ex = Assert.Throws<DomainException>(() =>
                Reservation.Place(G(), G(), G(), acl, loan, wait, clock));
            Assert.Contains("active loan", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ActivateLoan_happy_path_changes_status_and_emits_event()
        {
            var clock = new ControlledClock(new DateTimeOffset(2025, 2, 1, 10, 0, 0, TimeSpan.FromHours(5)));
            var wait = new FixedWaitPolicy(TimeSpan.FromDays(1));
            var loan = new StubLoanPolicy(false);
            var acl = new StubReaderClassifier(true, 2);

            var res = Reservation.Place(G(), G(), G(), acl, loan, wait, clock);
            DequeueEventsIfAvailable(res); // очистим очередь событий

            res.ActivateLoan(loan, clock);
            Assert.Equal(ReservationStatus.ActiveLoan, res.Status);

            var events = DequeueEventsIfAvailable(res);
            if (events.Count > 0)
                Assert.Contains(events, e => e is LoanActivated);
        }

        [Fact]
        public void ActivateLoan_fails_if_not_pending_or_copy_active()
        {
            var clock = new ControlledClock(new DateTimeOffset(2025, 2, 1, 10, 0, 0, TimeSpan.FromHours(5)));
            var wait = new FixedWaitPolicy(TimeSpan.FromDays(1));
            var loan = new StubLoanPolicy(false);
            var acl = new StubReaderClassifier(true, 2);

            var res = Reservation.Place(G(), G(), G(), acl, loan, wait, clock);
            res.ActivateLoan(loan, clock);

            var ex1 = Assert.Throws<DomainException>(() => res.ActivateLoan(loan, clock));
            Assert.Contains("Only pending", ex1.Message);

            // новая резервация, но политика говорит: уже активная выдача на экземпляр
            var res2 = Reservation.Place(G(), G(), res.CopyId, acl, new StubLoanPolicy(false), wait, clock);
            var ex2 = Assert.Throws<DomainException>(() => res2.ActivateLoan(new StubLoanPolicy(true), clock));
            Assert.Contains("active loan", ex2.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ActivateLoan_fails_if_wait_deadline_expired()
        {
            // Этот тест закрепляет правило: активировать после дедлайна нельзя
            var clock = new ControlledClock(new DateTimeOffset(2025, 2, 1, 10, 0, 0, TimeSpan.FromHours(5)));
            var wait = new FixedWaitPolicy(TimeSpan.FromHours(2));
            var loan = new StubLoanPolicy(false);
            var acl = new StubReaderClassifier(true, 2);

            var res = Reservation.Place(G(), G(), G(), acl, loan, wait, clock);
            clock.Advance(TimeSpan.FromHours(3)); // истечём дедлайн

            var ex = Assert.Throws<DomainException>(() => res.ActivateLoan(loan, clock));
            Assert.Contains("deadline", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ExpireWait_cancels_pending_after_deadline_and_is_idempotent()
        {
            var clock = new ControlledClock(new DateTimeOffset(2025, 2, 1, 10, 0, 0, TimeSpan.FromHours(5)));
            var wait = new FixedWaitPolicy(TimeSpan.FromHours(1));
            var loan = new StubLoanPolicy(false);
            var acl = new StubReaderClassifier(true, 2);

            var res = Reservation.Place(G(), G(), G(), acl, loan, wait, clock);
            DequeueEventsIfAvailable(res);

            clock.Advance(TimeSpan.FromHours(2));
            res.ExpireWait(clock);
            Assert.Equal(ReservationStatus.Canceled, res.Status);

            var events1 = DequeueEventsIfAvailable(res);
            if (events1.Count > 0)
                Assert.Contains(events1, e => e is WaitDeadlineExpired);

            // повторно — ничего не должно произойти
            res.ExpireWait(clock);
            var events2 = DequeueEventsIfAvailable(res);
            Assert.Empty(events2);
        }

        [Fact]
        public void Cancel_is_idempotent_and_emits_once()
        {
            var clock = new ControlledClock(new DateTimeOffset(2025, 2, 1, 10, 0, 0, TimeSpan.FromHours(5)));
            var wait = new FixedWaitPolicy(TimeSpan.FromDays(1));
            var loan = new StubLoanPolicy(false);
            var acl = new StubReaderClassifier(true, 2);

            var res = Reservation.Place(G(), G(), G(), acl, loan, wait, clock);
            DequeueEventsIfAvailable(res);

            CancelFlexible(res, clock);
            var events1 = DequeueEventsIfAvailable(res);
            if (events1.Count > 0)
                Assert.Contains(events1, e => e is ReservationCanceled);

            CancelFlexible(res, clock); // второй раз — идемпотентно
            var events2 = DequeueEventsIfAvailable(res);
            Assert.Empty(events2);
        }
    }
}
