using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainEvents.Policies
{
    public static class PublicationPolicy
    {
        // Пример: запрет публикации слоя без bbox и с неразрешённым типом геометрии
        public static IEnumerable<IDomainCommand> Decide(OnLayerPublishRequested e)
        {
            if (e.AlreadyPublishedLayers.Count != 0)
            {
               foreach (var layerId in e.AlreadyPublishedLayers)
               {
                    yield return new Unpublish(layerId, "Не могу опубликовать слой, потому что другие слои уже публикуютя.");
                }
            }
            yield return new ApprovePublish(e.LayerId);

        }
    }
}
