using Carshering.Booking.Domain.DomainEvents;
using Carshering.Booking.Domain.Enums;
using Carshering.Booking.Domain.Exceptions;
using Carshering.Booking.Domain.ValueObjects;

namespace Carshering.Booking.Domain.Entities
{
    public sealed class Booking
    {
        readonly List<IDomainEvent> _events = new();
        public IReadOnlyList<IDomainEvent> Events => _events;

        public BookingId Id { get; }
        public CarId CarId { get; }
        public DriverId DriverId { get; }
        public TimeWindow Window { get; }
        public Money Deposit { get; }
        public BookingStatus Status { get; private set; }
        public DateTimeOffset? DepositAuthorizedAt { get; private set; }

        private Booking(BookingId id, CarId carId, DriverId driverId, TimeWindow window, Money deposit)
        {
            Id = id;
            CarId = carId;
            DriverId = driverId;
            Window = window;
            Deposit = deposit;
            Status = BookingStatus.Placed;
        }

        public static Booking Place(BookingId id, CarId carId, DriverId driverId, TimeWindow window, Money deposit)
        {
            var booking = new Booking(id, carId, driverId, window, deposit);
            booking._events.Add(new BookingPlaced(id, carId, driverId, window, deposit));
            return booking;
        }

        public void AuthorizeDeposit(DateTimeOffset at)
        {
            if (Status != BookingStatus.Placed)
                throw new DomainException("Deposit can be authorized only for Placed booking.");
            if (at >= Window.From)
                throw new DomainException("Deposit must be authorized before rental window begins.");

            Status = BookingStatus.DepositAuthorized;
            DepositAuthorizedAt = at;
            _events.Add(new DepositAuthorized(Id, at));
        }

        public void Activate(DateTimeOffset at)
        {
            if (Status != BookingStatus.DepositAuthorized)
                throw new DomainException("Only deposit-authorized booking can be activated.");
            if (at < Window.From)
                throw new DomainException("Activation earlier than Window.From is not allowed.");
            if (at >= Window.To)
                throw new DomainException("Activation after Window.To is not allowed.");

            Status = BookingStatus.Active;
            _events.Add(new BookingActivated(Id, at));
        }
    }
}
