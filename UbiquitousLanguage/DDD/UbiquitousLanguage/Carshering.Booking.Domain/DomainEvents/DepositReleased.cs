using Carshering.Booking.Domain.ValueObjects;

namespace Carshering.Booking.Domain.DomainEvents
{
    public sealed record DepositReleased(BookingId BookingId, DateTimeOffset At, string Reason) : IDomainEvent;
}
