using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OperatorProxys
{
    [Serializable]
    public class OperatorTuple
    {
        private string stringRepr; // used for string represenation caching
        public List<string> Tuple { get; private set; }
        /// <summary>
        /// Id of the tuple (used for Exactly-Once-Semantics, to make sure that there are not duplicate
        /// operations executed. This ID only gets set to a new value when a tuple is read from a file
        /// (this symbolizes a new "stream"), otherwise it's iherited from the parent tuple (the
        /// tuple received in Operation() method, the one that the Oprator received and that caused the
        /// creation of this new tuple).
        /// </summary>
        public string Id { get; private set; }

        public OperatorTuple(List<String> tuple, string id)
        {
            Id = id;
            this.Tuple = tuple;
            this.stringRepr = null;
        }

        public void SetTuple(List<String> tuple, string id)
        {
            Id = id;
            this.Tuple = tuple;
            this.stringRepr = null;
        }

        public override string ToString()
        {
            if (stringRepr != null)
                return stringRepr;
            stringRepr = String.Format("ID: {0} | ", Id);
            foreach (string s in this.Tuple)
            {
                stringRepr += s + " ";
            }
            return stringRepr;
        }

    }

}
