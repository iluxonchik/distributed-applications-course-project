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

        // indicates wether an exception should be thrown when an invalid value is accessed
        // this is a hack and not the most elegant one, but this is very useful for testing,
        // because it allows the whole object to be serialized to JSON without any issuses.
        public bool ThrowExceptionOnInvalidGet { get; set; } = true;

        private List<OperatorSpec> operators;
        private OperatorSpec opSpec;
        private int repId = -1; // NOTE: any value < 0 corresponds to "null"
        //the time that OP must wait between 2 commands
        //this needs to be int because it is going to be used for Thread.spleep that only acepts ints
        // and we do not like casts.
        private int ms;

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

                if (ThrowExceptionOnInvalidGet)
                {

                    throw new NullReferrencePropertyException("Operator property is null");
                }
                else
                {
                    return opSpec;
                }
            }
            set { opSpec = value; }
        }

        public int MS

        {
            get
            {
                if (ms >= 0)
                {
                    return ms;
                }

                if (ThrowExceptionOnInvalidGet)
                {
                    throw new NullReferrencePropertyException("Milliseconds property is less than zero");
                } else
                {
                    return ms;
                }
            }
            set
            {
                if (ms < 0)
                {
                    throw new NullReferrencePropertyException("Milliseconds property is less than zero");
                }
                ms = value;
            }
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
                if (repId >= 0)
                {
                    return repId;
                }
                else
                {
                    if (ThrowExceptionOnInvalidGet)
                    {
                        throw new NullReferrencePropertyException("repId property is null (i.e. < 0)");
                    } else
                    {
                        return repId;
                    }
                }
            }
            set
            {
                this.repId = value;
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
                if (ThrowExceptionOnInvalidGet)
                {
                    throw new NullReferrencePropertyException("Operators property is null");
                } else
                {
                    return operators;
                }
            }
            set { operators = value; }
        }

    }
}
