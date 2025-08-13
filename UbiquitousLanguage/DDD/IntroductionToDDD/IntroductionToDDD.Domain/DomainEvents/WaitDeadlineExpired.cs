using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntroductionToDDD.Domain.DomainEvents
{
    public record WaitDeadlineExpired(Guid reservationId, Guid readerId, Guid copyId, DateTimeOffset? waitDeadline, DateTimeOffset occurredAt, int version);
}
