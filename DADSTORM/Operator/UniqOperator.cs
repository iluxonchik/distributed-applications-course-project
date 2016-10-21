using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Operator
{
    public class UniqOperator : OperatorImpl
    {
        /// <summary>
        /// save the id of the field_number
        /// </summary>
        private int id;

        public UniqOperator(int id_) : base()
        {
            id = id_;
        }

        /* WARNING verififcar a operacao é assim: retorna tuplo original 
         * se n repetir a string em + nenhuma posicao
         */
        public override List<string> Operation(List<string> tuple)
        {
            for (int i = 0; i < tuple.Count; i++)
            {
                if ((tuple[id].Equals(tuple[i])) && (id != i))
                {
                    return null;
                }

            }
            return tuple;
        }
    }
}
