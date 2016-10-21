using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Operator
{
    public class CountOperator : OperatorImpl
    {
        /// <summary>
        /// save the state of the counter
        /// </summary>
        public int countResult { get; set; }

        public CountOperator() : base()
        {
            countResult = 0;
        }

        public override List<string> Operation(List<string> tuple)
        {
            countResult += tuple.Count;

            /* check value later */
            return null;
        }
       
    }
}
