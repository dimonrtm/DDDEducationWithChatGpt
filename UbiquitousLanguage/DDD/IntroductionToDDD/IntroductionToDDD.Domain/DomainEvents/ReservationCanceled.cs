using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntroductionToDDD.Domain.DomainEvents
{
    public record ReservationCanceled(Guid reservationId, Guid readerId, Guid copyId, string reason, string? note, string canceledBy, DateTimeOffset occurredAt, int version);
}
