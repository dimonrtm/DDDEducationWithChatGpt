using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntroductionToDDD.Domain.DomainEvents
{
    public sealed record LoanActivationAuthorized(Guid CopyId, Guid ReservationId, Guid ReaderId, DateTimeOffset OccurredAt, int Version);
