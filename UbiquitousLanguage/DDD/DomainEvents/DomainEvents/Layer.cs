using Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainEvents
{
    public sealed class Layer : AggregateRoot
    {
        public Guid Id { get; private set; }
        public Guid ProjectId { get; private set; }
        public LayerStatus Status { get; private set; }
        public int AggregateVersion { get; private set; }

        public void Unpublish(string? correlationId = null, string? causationId = null)
        {
            // 1) Проверки инвариантов (подсказка: нельзя анпаблишить, если уже Unpublished)
            // 2) Изменить состояние
            // 3) Увеличить AggregateVersion
            // 4) Raise(new LayerUnpublished(...){ CorrelationId = ..., CausationId = ... })
            if ( Status == LayerStatus.Unpublished)
            {
                throw new DomainException("Слой уже снят с публикации");
            }
            Status = LayerStatus.Unpublished;
            AggregateVersion++;
            Raise(new LayerUnpublished(Id, AggregateVersion, ProjectId)
            {
                CorrelationId = correlationId,
                CausationId = causationId
            });
        }
    }

    public record LayerUnpublished(Guid LayerId, int VersionNumber, Guid ProjectId) : IDomainEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
        public int Version { get; } = 1;
        public string? CorrelationId { get; init; }
        public string? CausationId { get; init; }

        public int SchemaVersion => throw new NotImplementedException();
    }
}
