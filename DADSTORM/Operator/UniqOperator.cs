using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OperatorProxys;

namespace Operator
{
    public class UniqOperator : OperatorImpl
    {
        /// <summary>
        /// save the id of the field_number
        /// </summary>
        private int id;
        private ISet<string> uniq;

        public UniqOperator(int id_) : base()
        {
            this.uniq = new HashSet<string>();
            id = id_;
        }

        /* WARNING verififcar a operacao é assim: retorna tuplo original 
         * se n repetir a string em + nenhuma posicao
         */
        public override OperatorTuple Operation(OperatorTuple tuple)
        {
            if (!this.uniq.Contains(tuple.Tuple[id]))
            {
                uniq.Add(tuple.Tuple[id]);
                return tuple;
            }
            return null;
            
        }
        
    }
}
