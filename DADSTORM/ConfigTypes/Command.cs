using ConfigTypes.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConfigTypes
{
    public class Command
    {
        private List<OperatorSpec> operators;
        private OperatorSpec opSpec;
        private int repId;
        //the time that OP must wait between 2 commands
        //this needs to be int because it is going to be used for Thread.spleep that only acepts ints
        // and we do not like casts.
        private int ms;

        //the time that puppetMaster must wait between 2 commands
        public int wait { get; }

        public CommandType Type { get; set; }


        // corresponds to the OPERATOR_ID argument
        public OperatorSpec Operator
        {
            get
            {

                if (opSpec != null)
                {
                    return opSpec;
                }
                throw new NullReferrencePropertyException("Operator property is null");
            }
            set { opSpec = value; }
        }

        public int Op_ms
        {
            get
            {
                if (ms > 0)
                {
                    return ms;
                }
                throw new NullReferrencePropertyException("Milliseconds property is null");
            }
            set { ms = value; }
        }
        //public uint? RepId
        //{
        //    get
        //    {
        //        if (repId != null)
        //        {
        //            return repId;
        //        }
        //        throw new NullReferrencePropertyException("RepId property is null");
        //    }
        //    set { repId = value; }
        //}

            public int RepId
        {
            get
            {
                return this.repId;
            }
        }
        

        // this is an instance of Config.Operators, just make it point to that
        public List<OperatorSpec> Operators
        {
            get
            {
                if (operators != null)
                {
                    return operators;
                }
                throw new NullReferrencePropertyException("Operators property is null");
            }
            set { operators = value; }
        }


    }
}
