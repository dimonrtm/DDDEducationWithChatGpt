using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainEvents.Policies
{
    public record  Unpublish : IDomainCommand
    {
        public Guid LayerId { get; init; }
        public string? Reason { get; init; }
        public Unpublish(Guid layerId, string? reason = null)
        {
            LayerId = layerId;
            Reason = reason;
        }
    }
    {
    }
}
