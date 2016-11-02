using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OperatorProxys;
using PuppetMaster;
namespace Operator
{
    public class DupOperator : OperatorImpl
    {
        public DupOperator(OperatorSpec spec) : base(spec)
        {
        }

        public DupOperator() : base()
        {
        }
        public override OperatorTuple Operation(OperatorTuple tuple)
        {
            return tuple;
        }

        public override void Status()
        {
            generalStatus();
            Console.WriteLine("I'm dupping");
        }
    }
}
