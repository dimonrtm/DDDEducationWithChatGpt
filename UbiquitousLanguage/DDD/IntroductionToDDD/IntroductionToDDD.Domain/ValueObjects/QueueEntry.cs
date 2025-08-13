using IntroductionToDDD.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntroductionToDDD.Domain.ValueObjects
{
    public sealed record QueueEntry(
        Guid ReservationId,
        Guid ReaderId,
        PriorityLevel PriorityLevel,
        DateTimeOffset WaitDeadline,
        DateTimeOffset PlacedAt,
        long Seq
    );
}
