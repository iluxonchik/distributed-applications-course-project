using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConfigTypes;
using System.Diagnostics;

namespace ProcessCreationProxy
{
    public interface IProcessCreationProxy
    {
       Process CreateOperator(OperatorSpec opSpec, string myAddr, int replicaId);
       void Crash();
    }
}
