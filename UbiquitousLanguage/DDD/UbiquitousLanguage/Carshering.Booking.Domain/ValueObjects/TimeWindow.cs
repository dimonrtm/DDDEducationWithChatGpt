using Carshering.Booking.Domain.Exceptions;

namespace Carshering.Booking.Domain.ValueObjects
{
    public sealed class TimeWindow
    {
        public DateTimeOffset From { get; }
        public DateTimeOffset To { get; }

        public TimeSpan Duration => To - From;

        public TimeWindow(DateTimeOffset from, DateTimeOffset to)
        {
            if (to <= from) throw new DomainException("TimeWindow must be strictly positive.");
            From = from;
            To = to;
        }

        public bool Contains(DateTimeOffset at) => at >= From && at <= To;

        public override string ToString() => $"{From:O}..{To:O}";
    }
}
