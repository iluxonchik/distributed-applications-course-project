using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Operator
{
    public class DupOperator : OperatorImpl
    {
        public override List<string> Operation(List<string> tuple)
        {
            return tuple;
        }
    }
}
