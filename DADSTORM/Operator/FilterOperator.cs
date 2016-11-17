using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OperatorProxys;
using ConfigTypes;

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

        public FilterOperator(OperatorSpec spec,int id_, string cond_, string compare_, string myAddr, int repId) : base(spec, myAddr, repId)
        {
            id = id_ - 1;
            cond = cond_;
            compare = compare_;
        }

        public FilterOperator(int id_, string cond_, string compare_) : base()
        {
            id = id_ - 1;
            cond = cond_;
            compare = compare_;
        }
        
        
        public override List<OperatorTuple> Operation(OperatorTuple tuple)
        {
            List<OperatorTuple> list = new List<OperatorTuple>();
            
            switch (cond)
            {
                case "<":
                        if (String.Compare(tuple.Tuple[id], compare) < 0)
                        {
                        list.Add(tuple);
                        return list;


                    }
                    break;
                case "=":
                    if (String.Compare(tuple.Tuple[id], compare) == 0)
                    {
                        list.Add(tuple);
                        return list;

                    }
                    break;
                case ">":
                    if (String.Compare(tuple.Tuple[id], compare) > 0)
                    {
                        list.Add(tuple);
                        return list;
                    }
                    break;
                default:
                    return list; /* SHOULD NEVER HAPPEN */
            }
            return list;
        }

        public override void Status()
        {
            generalStatus();
            Console.WriteLine("Id: " + id + " | Condition: " + cond + " | Compare: " + compare);
        }
    }
}