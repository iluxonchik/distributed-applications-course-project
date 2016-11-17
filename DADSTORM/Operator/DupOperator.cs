using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OperatorProxys;
using ConfigTypes;

namespace Operator
{
    public class DupOperator : OperatorImpl
    {
        public DupOperator(OperatorSpec spec, string myAddr, int repId) : base(spec, myAddr, repId)
        {
        }

        public DupOperator() : base()
        {
        }
        public override List<OperatorTuple> Operation(OperatorTuple tuple)
        {
            List<OperatorTuple> list = new List<OperatorTuple>();
            list.Add(tuple);
            return list;
        }

        public override void Status()
        {
            generalStatus();
            Console.WriteLine("Dupping");
        }
    }
}
