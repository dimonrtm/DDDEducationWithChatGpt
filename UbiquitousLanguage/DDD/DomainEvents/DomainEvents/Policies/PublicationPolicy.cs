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
            var others = e.AlreadyPublishedLayers
            .Where(id => id != e.LayerId)   // не трогаем текущий
            .Distinct()                      // без дублей
            .OrderBy(id => id);              // стабильный порядок

            foreach (var layerId in others)
                yield return new Unpublish(layerId, reason: "Exclusive publish required");

            yield return new ApprovePublish(e.LayerId, e.ProjectId);

        }
    }
}
