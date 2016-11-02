using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OperatorProxys;
using PuppetMaster;
namespace Operator
{
    public class FilterOperator : OperatorImpl
    {
        /// <summary>
        /// save the id of the field_number
        /// </summary>
        private int id;

        /// <summary>
        /// save the condition to be tested ( < or > or = )
        /// </summary>
        private string cond;
        
        /// <summary>
        /// save the value to be compared
        /// </summary>
        private string compare;

        public FilterOperator(OperatorSpec spec,int id_, string cond_, string compare_) : base(spec)
        {
            id = id_;
            cond = cond_;
            compare = compare_;
        }

        public FilterOperator(int id_, string cond_, string compare_) : base()
        {
            id = id_;
            cond = cond_;
            compare = compare_;
        }
        
        /* WARNING verififcar a operacao é assim: se a string for igual a operacao desejada devolve tuplo */
        public override OperatorTuple Operation(OperatorTuple tuple)
        {
            switch(cond)
            {
                case "<":
                        if (String.Compare(tuple.Tuple[id], compare) < 0)
                        {
                            return tuple;
                        }
                    break;
                case "=":
                    if (String.Compare(tuple.Tuple[id], compare) == 0)
                    {
                        return tuple;
                    }
                    break;
                case ">":
                    if (String.Compare(tuple.Tuple[id], compare) > 0)
                    {
                        return tuple;
                    }
                    break;
                default:
                    return tuple; /* SHOULD NEVER HAPPEN */
            }
            return null;
        }

        public override void Status()
        {
            generalStatus();
            Console.WriteLine("Id: " + id + " | Condition: " + cond + " | Compare: " + compare);
        }
    }
}