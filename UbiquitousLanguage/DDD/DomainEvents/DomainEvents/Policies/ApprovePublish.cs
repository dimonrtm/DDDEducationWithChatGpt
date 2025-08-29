using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainEvents.Policies
{
    public record ApprovePublish : IDomainCommand
    {
        public Guid LayerId { get; init; }
        public Guid ProjectId { get; init; }
        public string? Reason { get; init; }

        public ApprovePublish(Guid layerId, Guid projectId, string? reason = null)
        {
            LayerId = layerId;
            ProjectId = projectId;
            Reason = reason;
        }
    }
}
