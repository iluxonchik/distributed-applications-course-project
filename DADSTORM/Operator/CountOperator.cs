﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OperatorProxys;

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

        public override OperatorTuple Operation(OperatorTuple tuple)
        {
            countResult++;

            /* check value later */
            return null;
        }
        
    }
}
