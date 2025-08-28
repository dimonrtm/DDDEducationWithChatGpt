using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainEvents
{
    public interface IDomainEvent
    {
        Guid EventId { get; }
        DateTime OccurredOn { get; }
        int SchemaVersion { get; }
        string? CorrelationId { get; }
        string? CausationId { get; }
    }
}
