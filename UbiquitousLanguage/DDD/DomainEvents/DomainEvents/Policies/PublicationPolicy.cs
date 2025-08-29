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

           
        }
    }
}
