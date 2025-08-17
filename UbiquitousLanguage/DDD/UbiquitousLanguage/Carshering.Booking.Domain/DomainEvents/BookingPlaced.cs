using Carshering.Booking.Domain.ValueObjects;

namespace Carshering.Booking.Domain.DomainEvents
{
    public sealed record BookingPlaced(
    BookingId BookingId, CarId CarId, DriverId DriverId, TimeWindow Window, Money Deposit
) : IDomainEvent;
}
