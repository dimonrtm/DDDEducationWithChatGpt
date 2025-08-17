using Carshering.Booking.Domain.ValueObjects;

namespace Carshering.Booking.Domain.DomainEvents
{
    public sealed record BookingCancelled(BookingId BookingId, DateTimeOffset At, string? Reason) : IDomainEvent;
}
