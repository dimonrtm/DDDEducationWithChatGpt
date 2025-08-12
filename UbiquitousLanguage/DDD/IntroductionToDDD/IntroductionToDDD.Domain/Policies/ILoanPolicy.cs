using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntroductionToDDD.Domain.Policies
{
    public interface ILoanPolicy
    {
        bool HasActiveLoan(Guid copyId);
    }
