using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainEvents.Policies
{
    public record OnLayerPublishRequested(Guid LayerId, Guid ProjectId, int AggregateVersion, IReadOnlyList<Guid> AlreadyPublishedLayers);
}
