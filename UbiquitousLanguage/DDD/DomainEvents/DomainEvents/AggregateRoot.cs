using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainEvents
{
    public abstract class AggregateRoot
    {
        private readonly List<IDomainEvent> _pending = new();
        protected void Raise(IDomainEvent e) => _pending.Add(e);
        public IReadOnlyCollection<IDomainEvent> DequeueEvents()
        {
            var copy = _pending.ToArray();
            _pending.Clear();
            return copy;
        }
    }
}
