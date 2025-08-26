using DomainServices.Policy;
using DomainServices.PublishVersionHandler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainServices.PublishHandler
{
    public interface ICoordinateTransformer
    {
        LayerVersion TransformTo(LayerVersion version, Crs targetCrs);
    }

    public sealed class PublishWithTransformHandler
    {
        private readonly ILayerRepository _layers;
        private readonly ILayerVersionRepository _versions;
        private readonly Policy.IPublicationPolicy _policy;
        private readonly ICoordinateTransformer _transformer;
        private readonly IUnitOfWork _uow;
        private readonly IEventBus _bus;

        public PublishWithTransformHandler(
            ILayerRepository layers,
            ILayerVersionRepository versions,
            Policy.IPublicationPolicy policy,
            ICoordinateTransformer transformer,
            IUnitOfWork uow,
            IEventBus bus)
        { _layers = layers; _versions = versions; _policy = policy; _transformer = transformer; _uow = uow; _bus = bus; }

        public async Task<PublishResult> EnsureCrsAndPublishAsync(Guid layerId, Guid versionId, CancellationToken ct)
        {
            var layer = await _layers.GetAsync(layerId, ct);
            var version = await _versions.GetAsync(versionId, ct);
            if (layer is null) return new(false, "Layer not found");
            if (version is null) return new(false, "LayerVersion not found");
            if (version.LayerId != layer.Id) return new(false, "Version does not belong to Layer");

            var ctx = new PublicationContext(DateTimeOffset.UtcNow);
            var decision = _policy.CanPublish(layer, version, ctx);

            if (!decision.IsAllowed && decision.Reason == PublicationReason.DifferentCrs)
            {
                var fixedVersion = _transformer.TransformTo(version, layer.Crs);
                decision = _policy.CanPublish(layer, fixedVersion, ctx);
                if (!decision.IsAllowed) return new(false, decision.Message ?? decision.Reason.ToString());

                // здесь используйте fixedVersion далее (сохранение как нужно вашему хранилищу)
                version = fixedVersion;
            }

            if (!decision.IsAllowed) return new(false, decision.Message ?? decision.Reason.ToString());

            layer.Publish(version.Id);
            await _layers.SaveAsync(layer, ct);
            await _uow.CommitAsync(ct);
            await _bus.PublishAsync(new LayerVersionPublished(layer.Id, version.Id, ctx.Now), ct);
            return new(true, null);
        }
    }
}
