using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainServices.Polisy
{
    public enum Reasons
    {
        Ok,
        DifferentCrs,
        AlreadyPublished,
        LayerLocked,
        ValidationFailed
    }

    public record PublicationDecision(bool IsAlloved, Reasons Reason, string? Message  = null, Dictionary<string, object>? Details = null);
    public interface IPublicationPolicy
    {
        PublicationDecision CanPublish(Layer layer, LayerVersion version, PublicationContext ctx);
    }
}
