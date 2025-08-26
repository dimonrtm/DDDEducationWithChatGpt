using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainServices.Policy;

public enum PublicationReason
{
    Ok,
    DifferentCrs,
    AlreadyPublished,
    LayerLocked,
    ValidationFailed
}

public sealed record PublicationDecision(
    bool IsAllowed,
    PublicationReason Reason,
    string? Message = null,
    IReadOnlyDictionary<string, object>? Details = null)
{
    public static PublicationDecision Allow(string? msg = null) =>
        new(true, PublicationReason.Ok, msg);

    public static PublicationDecision Deny(PublicationReason reason, string? msg = null,
        IReadOnlyDictionary<string, object>? details = null) =>
        new(false, reason, msg, details);
}

public interface IPublicationPolicy
{
    PublicationDecision CanPublish(Layer layer, LayerVersion version, PublicationContext ctx);
}
