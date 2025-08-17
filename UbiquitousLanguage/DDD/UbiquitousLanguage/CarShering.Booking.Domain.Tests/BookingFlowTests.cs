using Carshering.Booking.Domain.DomainEvents;
using Carshering.Booking.Domain.Enums;
using Carshering.Booking.Domain.Exceptions;
using Carshering.Booking.Domain.ValueObjects;

namespace CarShering.Booking.Domain.Tests
{
    public class BookingFlowTests
    {
        [Fact]
        public void Given_PlacedBooking_When_AuthorizeDeposit_BeforeWindowFrom_Then_DepositAuthorized_And_EventRaised()
        {
            // Arrange (UL-данные)
            var now = DateTimeOffset.UtcNow;
            var from = now.AddHours(1);
            var to = from.AddHours(1);
            var win = new TimeWindow(from, to);
            var dep = new Money(1000m, "KZT");
            var b = Carshering.Booking.Domain.Entities.Booking.Place(new(Guid.NewGuid()), new(Guid.NewGuid()), new(Guid.NewGuid()), win, dep);

            // Act
            var at = from.AddMinutes(-5);
            b.AuthorizeDeposit(at);

            // Assert (UL)
            Assert.Equal(BookingStatus.DepositAuthorized, b.Status);
            Assert.Contains(b.Events, e => e is DepositAuthorized da && da.BookingId.Equals(b.Id) && da.At == at);
        }

        [Fact]
        public void Given_DepositAuthorized_When_Activate_AtWindowFrom_Then_Active_And_EventRaised()
        {
            var now = DateTimeOffset.UtcNow;
            var from = now.AddHours(1);
            var to = from.AddHours(2);
            var win = new TimeWindow(from, to);
            var dep = new Money(1000m, "KZT");
            var b = Carshering.Booking.Domain.Entities.Booking.Place(new(Guid.NewGuid()), new(Guid.NewGuid()), new(Guid.NewGuid()), win, dep);
            b.AuthorizeDeposit(from.AddMinutes(-10));

            b.Activate(from); // ровно в From

            Assert.Equal(BookingStatus.Active, b.Status);
            Assert.Equal(from, b.ActivatedAt);
            Assert.Contains(b.Events, e => e is BookingActivated a && a.At == from);
        }

        [Fact]
        public void Given_DepositAuthorized_When_Activate_BeforeWindowFrom_Then_Throws()
        {
            var now = DateTimeOffset.UtcNow;
            var from = now.AddHours(1);
            var to = from.AddHours(2);
            var win = new TimeWindow(from, to);
            var dep = new Money(1000m, "KZT");
            var b = Carshering.Booking.Domain.Entities.Booking.Place(new(Guid.NewGuid()), new(Guid.NewGuid()), new(Guid.NewGuid()), win, dep);
            b.AuthorizeDeposit(from.AddMinutes(-10));

            var actAt = from.AddMinutes(-1);

            var ex = Assert.Throws<DomainException>(() => b.Activate(actAt));
            Assert.Contains("earlier than Window.From", ex.Message);
        }

        [Fact]
        public void Given_Active_When_Complete_EarlierThanWindowTo_Then_Completed_And_UsedIsMeasured_And_DepositReleased()
        {
            var now = DateTimeOffset.UtcNow;
            var from = now.AddHours(1);
            var to = from.AddHours(2);
            var win = new TimeWindow(from, to);
            var dep = new Money(1000m, "KZT");
            var b = Carshering.Booking.Domain.Entities.Booking.Place(new(Guid.NewGuid()), new(Guid.NewGuid()), new(Guid.NewGuid()), win, dep);
            b.AuthorizeDeposit(from.AddMinutes(-10));
            b.Activate(from);

            var completeAt = from.AddMinutes(35);
            b.Complete(completeAt);

            Assert.Equal(BookingStatus.Completed, b.Status);
            Assert.Contains(b.Events, e => e is BookingCompleted c && c.Used == TimeSpan.FromMinutes(35));
            Assert.Contains(b.Events, e => e is DepositReleased dr && dr.Reason == "Completed");
        }

        [Fact]
        public void Given_DepositAuthorized_When_Cancel_BeforeActivation_Then_Cancelled_And_DepositReleased()
        {
            var now = DateTimeOffset.UtcNow;
            var from = now.AddHours(1);
            var to = from.AddHours(2);
            var win = new TimeWindow(from, to);
            var dep = new Money(1000m, "KZT");
            var b = Carshering.Booking.Domain.Entities.Booking.Place(new(Guid.NewGuid()), new(Guid.NewGuid()), new(Guid.NewGuid()), win, dep);
            b.AuthorizeDeposit(from.AddMinutes(-10));

            b.Cancel(from.AddMinutes(-2), "ChangeOfPlans");

            Assert.Equal(BookingStatus.Cancelled, b.Status);
            Assert.Contains(b.Events, e => e is BookingCancelled);
            Assert.Contains(b.Events, e => e is DepositReleased dr && dr.Reason == "CancelledBeforeStart");
        }

        [Fact]
        public void Given_Placed_When_AuthorizeDeposit_AfterWindowFrom_Then_Throws()
        {
            // Arrange
            // TODO: подготовьте окно, депозит и Place(...)
            // Act + Assert
            // TODO: вызов AuthorizeDeposit с моментом >= Window.From и проверка DomainException
        }

        [Fact]
        public void Given_Active_When_Cancel_Then_Throws()
        {
            // Arrange
            // TODO: Place, AuthorizeDeposit, Activate
            // Act + Assert
            // TODO: попытка Cancel и проверка DomainException
        }
    }
}
