using Carshering.Booking.Domain.ValueObjects;

namespace Carshering.Booking.Domain.DomainEvents
{
    public sealed record BookingActivated(BookingId BookingId, DateTimeOffset At) : IDomainEvent;
