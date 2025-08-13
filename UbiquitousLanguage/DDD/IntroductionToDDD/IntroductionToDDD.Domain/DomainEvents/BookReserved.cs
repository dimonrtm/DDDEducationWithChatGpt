using IntroductionToDDD.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntroductionToDDD.Domain.DomainEvents
{
    public  record BookReserved(Guid reservationId, Guid readerId, Guid copyId, ReservationStatus status, int priorityLevel, DateTimeOffset? waitDeadline, DateTimeOffset occurredAt, int version);
    
}
