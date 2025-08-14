using IntroductionToDDD.Domain.DomainEvents;
using IntroductionToDDD.Domain.Entities;
using IntroductionToDDD.Domain.Enums;
using IntroductionToDDD.Domain.Exceptions;
using IntroductionToDDD.Domain.Policies;
using Xunit;

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
        [Fact]
        public void Staff_bypasses_queue_and_triggers_head_changed()
        {
            var copyId = G();
            var clock = new ControlledClock(new DateTimeOffset(2025, 2, 1, 10, 0, 0, TimeSpan.FromHours(5)));
            var q = new ReservationQueue(copyId);

            var (reg, r1) = Pair();
            var (vip, r2) = Pair();
            var (staff, r3) = Pair();

            q.Place(reg, r1, PriorityLevel.Regular, clock.Now().AddDays(1), clock);
            q.DequeueUncommittedEvents(); // очистим для точной проверки ниже

            q.Place(vip, r2, PriorityLevel.Vip, clock.Now().AddDays(1), clock);
            q.DequeueUncommittedEvents();

            q.Place(staff, r3, PriorityLevel.Staff, clock.Now().AddDays(1), clock);
            var events = q.DequeueUncommittedEvents().ToList();

            Assert.Equal(staff, q.Head!.ReservationId);

            // Должны быть события: ReservationQueued + QueueHeadChanged
            Assert.Contains(events, e => e is ReservationQueued rq && rq.ReservationId == staff);
            Assert.Contains(events, e => e is QueueHeadChanged ch && ch.HeadReservationId == staff);
        }
        [Fact]
        public void AuthorizeActivation_only_for_head()
        {
            var copyId = G();
            var loan = new StubLoanPolicy(false);
            var clock = new ControlledClock(new DateTimeOffset(2025, 2, 1, 10, 0, 0, TimeSpan.FromHours(5)));
            var q = new ReservationQueue(copyId);

            var (reg, r1) = Pair();
            var (vip, r2) = Pair();
            q.Place(reg, r1, PriorityLevel.Regular, clock.Now().AddDays(1), clock);
            q.Place(vip, r2, PriorityLevel.Vip, clock.Now().AddDays(1), clock);
            // VIP должен стоять перед Regular
            Assert.Equal(vip, q.Head!.ReservationId);

            var ex = Assert.Throws<DomainException>(() =>
                q.AuthorizeActivation(reg, loan, clock)); // не голова
            Assert.Contains("Only head of queue", ex.Message);
        }

        [Fact]
        public void AuthorizeActivation_sets_active_removes_head_and_emits_events()
        {
            var copyId = G();
            var loan = new StubLoanPolicy(false);
            var clock = new ControlledClock(new DateTimeOffset(2025, 2, 1, 10, 0, 0, TimeSpan.FromHours(5)));
            var q = new ReservationQueue(copyId);

            var (vip, r1) = Pair();
            var (reg, r2) = Pair();

            q.Place(vip, r1, PriorityLevel.Vip, clock.Now().AddDays(1), clock);
            q.Place(reg, r2, PriorityLevel.Regular, clock.Now().AddDays(1), clock);
            q.DequeueUncommittedEvents();

            q.AuthorizeActivation(vip, loan, clock);
            var events = q.DequeueUncommittedEvents().ToList();

            Assert.Equal(vip, q.ActiveReservationId);
            Assert.Single(q.Entries); // остался только reg
            Assert.Contains(events, e => e is LoanActivationAuthorized la && la.ReservationId == vip);
            Assert.Contains(events, e => e is QueueHeadChanged ch && ch.HeadReservationId == reg);
        }

        [Fact]
        public void AuthorizeActivation_fails_when_deadline_expired_or_policy_says_active()
        {
            var copyId = G();
            var loan = new StubLoanPolicy(hasActive: true);
            var clock = new ControlledClock(new DateTimeOffset(2025, 2, 1, 10, 0, 0, TimeSpan.FromHours(5)));
            var q = new ReservationQueue(copyId);

            var (vip, r1) = Pair();
            q.Place(vip, r1, PriorityLevel.Vip, clock.Now().AddHours(1), clock);

            // Политика: уже есть активная выдача
            var ex1 = Assert.Throws<DomainException>(() => q.AuthorizeActivation(vip, loan, clock));
            Assert.Contains("already has an active loan", ex1.Message);

            // Снимем блокировку в политике, но истечём дедлайн
            loan.SetActive(false);
            clock.Advance(TimeSpan.FromHours(2));

            var ex2 = Assert.Throws<DomainException>(() => q.AuthorizeActivation(vip, loan, clock));
            Assert.Contains("deadline expired", ex2.Message);
        }

        [Fact]
        public void ExpireOverdue_removes_all_overdue_heads_and_emits_head_changed()
        {
            var copyId = G();
            var loan = new StubLoanPolicy(false);
            var clock = new ControlledClock(new DateTimeOffset(2025, 2, 1, 10, 0, 0, TimeSpan.FromHours(5)));
            var q = new ReservationQueue(copyId);

            var (staff, rs) = Pair();
            var (vip, rv) = Pair();
            var (reg, rr) = Pair();

            q.Place(staff, rs, PriorityLevel.Staff, clock.Now().AddHours(1), clock);
            q.Place(vip, rv, PriorityLevel.Vip, clock.Now().AddHours(2), clock);
            q.Place(reg, rr, PriorityLevel.Regular, clock.Now().AddDays(1), clock);
            q.DequeueUncommittedEvents();

            // просрочим staff и vip
            clock.Advance(TimeSpan.FromHours(3));
            q.ExpireOverdue(clock);
            var events = q.DequeueUncommittedEvents().ToList();

            Assert.Equal(reg, q.Head!.ReservationId);
            Assert.Contains(events, e => e is WaitDeadlineExpiredFromQueue w1 && w1.ReservationId == staff);
            Assert.Contains(events, e => e is WaitDeadlineExpiredFromQueue w2 && w2.ReservationId == vip);
            Assert.Contains(events, e => e is QueueHeadChanged ch && ch.HeadReservationId == reg);
        }

        [Fact]
        public void Remove_non_head_removes_and_does_not_change_head()
        {
            var copyId = G();
            var clock = new ControlledClock(new DateTimeOffset(2025, 2, 1, 10, 0, 0, TimeSpan.FromHours(5)));
            var q = new ReservationQueue(copyId);

            var (staff, rs) = Pair();
            var (vip, rv) = Pair();
            var (reg, rr) = Pair();

            q.Place(staff, rs, PriorityLevel.Staff, clock.Now().AddDays(1), clock); // head
            q.Place(vip, rv, PriorityLevel.Vip, clock.Now().AddDays(1), clock);
            q.Place(reg, rr, PriorityLevel.Regular, clock.Now().AddDays(1), clock);
            q.DequeueUncommittedEvents();

            q.Remove(vip, CancelReason.UserCancel, CanceledBy.Reader, clock, "no longer needed");
            var events = q.DequeueUncommittedEvents().ToList();

            Assert.Equal(staff, q.Head!.ReservationId); // голова не изменилась
            Assert.DoesNotContain(events, e => e is QueueHeadChanged); // голова не трогалась
            Assert.Contains(events, e => e is ReservationRemovedFromQueue rem && rem.ReservationId == vip);
        }

        [Fact]
        public void Remove_nonexistent_is_idempotent()
        {
            var copyId = G();
            var clock = new ControlledClock(new DateTimeOffset(2025, 2, 1, 10, 0, 0, TimeSpan.FromHours(5)));
            var q = new ReservationQueue(copyId);

            var (res, rr) = Pair();
            q.Place(res, rr, PriorityLevel.Regular, clock.Now().AddDays(1), clock);
            q.DequeueUncommittedEvents();

            q.Remove(G(), CancelReason.UserCancel, CanceledBy.Reader, clock, "no-op");
            var events = q.DequeueUncommittedEvents();

            Assert.Empty(events); // ничего не произошло
        }

        [Fact]
        public void Place_duplicate_is_idempotent()
        {
            var copyId = G();
            var clock = new ControlledClock(new DateTimeOffset(2025, 2, 1, 10, 0, 0, TimeSpan.FromHours(5)));
            var q = new ReservationQueue(copyId);

            var (res, r) = Pair();
            q.Place(res, r, PriorityLevel.Regular, clock.Now().AddDays(1), clock);
            q.DequeueUncommittedEvents();

            q.Place(res, r, PriorityLevel.Regular, clock.Now().AddDays(1), clock); // повтор
            var events = q.DequeueUncommittedEvents();

            Assert.Single(q.Entries);
            Assert.Empty(events); // никаких доп. событий
        }

        [Fact]
        public void Remove_active_is_forbidden()
        {
            var copyId = G();
            var loan = new StubLoanPolicy(false);
            var clock = new ControlledClock(new DateTimeOffset(2025, 2, 1, 10, 0, 0, TimeSpan.FromHours(5)));
            var q = new ReservationQueue(copyId);

            var (vip, r1) = Pair();
            var (reg, r2) = Pair();

            q.Place(vip, r1, PriorityLevel.Vip, clock.Now().AddDays(1), clock);
            q.Place(reg, r2, PriorityLevel.Regular, clock.Now().AddDays(1), clock);
            q.DequeueUncommittedEvents();

            q.AuthorizeActivation(vip, loan, clock); // активируем голову
            q.DequeueUncommittedEvents();

            var ex = Assert.Throws<DomainException>(() =>
                q.Remove(vip, CancelReason.UserCancel, CanceledBy.Reader, clock));
            Assert.Contains("Cannot remove active reservation", ex.Message);
        }

        [Fact]
        public void ReleaseActive_clears_active_and_emits_head_changed_if_any()
        {
            var copyId = G();
            var loan = new StubLoanPolicy(false);
            var clock = new ControlledClock(new DateTimeOffset(2025, 2, 1, 10, 0, 0, TimeSpan.FromHours(5)));
            var q = new ReservationQueue(copyId);

            var (vip, r1) = Pair();
            var (reg, r2) = Pair();

            q.Place(vip, r1, PriorityLevel.Vip, clock.Now().AddDays(1), clock);
            q.Place(reg, r2, PriorityLevel.Regular, clock.Now().AddDays(1), clock);
            q.DequeueUncommittedEvents();

            q.AuthorizeActivation(vip, loan, clock);
            q.DequeueUncommittedEvents();

            q.ReleaseActive(vip, clock);
            var events = q.DequeueUncommittedEvents().ToList();

            Assert.Null(q.ActiveReservationId);
            Assert.Equal(reg, q.Head!.ReservationId);
            Assert.Contains(events, e => e is ActiveLoanCleared a && a.ReservationId == vip);
            Assert.Contains(events, e => e is QueueHeadChanged ch && ch.HeadReservationId == reg);
        }
    }
    }
