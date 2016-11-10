using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OperatorProxys;
using ConfigTypes;

namespace Operator
{
    public class CountOperator : OperatorImpl
    {
        /// <summary>
        /// save the state of the counter
        /// </summary>
        public int countResult { get; set; }

        public CountOperator(OperatorSpec spec, string myAddr, int repId) : base(spec, myAddr, repId)
        {
            countResult = 0;
        }

        public CountOperator() : base()
        {
            countResult = 0;
        }

        public override OperatorTuple Operation(OperatorTuple tuple)
        {
            countResult++;

           
            return tuple;
        }

        public override void Status()
        {
            generalStatus();
            Console.WriteLine("Count result: " + countResult);
        }
    }
}
