using PuppetMaster.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuppetMaster
{
    class Command
    {
        private List<OperatorSpec> operators;
        private OperatorSpec opSpec;
        private uint? repId;
        private uint? ms;

        CommandType Type { get; set; }


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

        public uint? Milliseconds
        {
            get
            {
                if (ms != null)
                {
                    return ms;
                }
                throw new NullReferrencePropertyException("Milliseconds property is null");
            }
            set { ms = value; }
        }
        public uint? RepId
        {
            get
            {
                if (repId != null)
                {
                    return repId;
                }
                throw new NullReferrencePropertyException("RepId property is null");
            }
            set { repId = value; }
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
