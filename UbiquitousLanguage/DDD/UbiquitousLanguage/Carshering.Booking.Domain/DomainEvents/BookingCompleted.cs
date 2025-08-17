using Carshering.Booking.Domain.ValueObjects;

namespace Carshering.Booking.Domain.DomainEvents
{
    public sealed record BookingCompleted(BookingId BookingId, DateTimeOffset At, TimeSpan Used) : IDomainEvent;
}
