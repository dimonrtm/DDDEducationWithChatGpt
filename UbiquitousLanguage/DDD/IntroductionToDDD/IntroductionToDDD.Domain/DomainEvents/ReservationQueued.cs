using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntroductionToDDD.Domain.DomainEvents
{
    public sealed record ReservationQueued(Guid CopyId, Guid ReservationId, Guid ReaderId, int PriorityLevel, DateTimeOffset WaitDeadline, DateTimeOffset OccurredAt, int Version);
}
