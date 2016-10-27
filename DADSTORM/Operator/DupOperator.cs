using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OperatorProxys;
namespace Operator
{
    public class DupOperator : OperatorImpl
    {
        public override OperatorTuple Operation(OperatorTuple tuple)
        {
            return tuple;
        }
        
    }
}
