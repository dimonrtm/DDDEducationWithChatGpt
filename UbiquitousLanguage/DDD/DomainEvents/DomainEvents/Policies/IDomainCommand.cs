using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainEvents.Policies
{
    public interface IDomainCommand
    {
        public Guid LayerId { get; }
        public string? Reason { get; }
    }
}
