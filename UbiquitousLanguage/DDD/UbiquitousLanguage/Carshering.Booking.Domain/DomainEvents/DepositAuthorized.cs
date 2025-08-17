using Carshering.Booking.Domain.ValueObjects;

namespace Carshering.Booking.Domain.DomainEvents
{
    public sealed record DepositAuthorized(BookingId BookingId, DateTimeOffset At) : IDomainEvent;
}
