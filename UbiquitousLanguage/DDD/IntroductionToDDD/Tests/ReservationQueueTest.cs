using Xunit;
using IntroductionToDDD.Domain.Entities;
using IntroductionToDDD.Domain.Enums;
using IntroductionToDDD.Domain.Policies;

namespace Tests
{
    public class ReservationQueueTest
    {
        private static Guid G() => Guid.NewGuid();

        private static (Guid resId, Guid readerId) Pair() => (G(), G());

        private sealed class ControlledClock : IClock
        {
            private DateTimeOffset _now;
            public ControlledClock(DateTimeOffset now) => _now = now;
            public DateTimeOffset Now() => _now;
            public void Advance(TimeSpan delta) => _now = _now.Add(delta);
            public void Set(DateTimeOffset dt) => _now = dt;
        }

        private sealed class StubLoanPolicy : ILoanPolicy
        {
            private bool _hasActive;
            public StubLoanPolicy(bool hasActive = false) { _hasActive = hasActive; }
            public bool HasActiveLoan(Guid copyId) => _hasActive;
            public void SetActive(bool val) => _hasActive = val;
        }

        [Fact]
        public void Place_orders_by_priority_then_fifo()
        {
            var copyId = G();
            var clock = new ControlledClock(new DateTimeOffset(2025, 2, 1, 10, 0, 0, TimeSpan.FromHours(5)));
            var q = new ReservationQueue(copyId);

            var (reg1, r1) = Pair();
            var (vip1, r2) = Pair();
            var (staff1, r3) = Pair();
            var (vip2, r4) = Pair();

            q.Place(reg1, r1, PriorityLevel.Regular, clock.Now().AddDays(2), clock);
            q.Place(vip1, r2, PriorityLevel.Vip, clock.Now().AddDays(2), clock);
            q.Place(staff1, r3, PriorityLevel.Staff, clock.Now().AddDays(2), clock);
            q.Place(vip2, r4, PriorityLevel.Vip, clock.Now().AddDays(2), clock);

            var order = q.Entries.Select(e => e.ReservationId).ToArray();
            Assert.Equal([staff1, vip1, vip2, reg1], order);
        }
    }
}
