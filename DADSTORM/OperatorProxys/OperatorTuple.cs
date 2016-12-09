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
        /// <summary>
        /// Indicates wether the receiver is a "parent" (i.e. if he needs to forward the result to the next operator in chain
        /// or if he should just compute and store it.
        /// </summary>
        public bool YouAreParent { get; set; } = true;

        /// <summary>
        /// URL of the Operator that sent this tuple. This is needed to send the ACK in Exactly-Once-Semantics.
        /// </summary>
        public string SenderUrl { get; set; }

        public OperatorTuple(List<String> tuple, string id, string senderUrl)
        {
            Id = id;
            SenderUrl = senderUrl;
            Tuple = tuple;
            stringRepr = null;

        }

        public void SetTuple(List<String> tuple, string id, string senderUrl)
        {
            Id = id;
            SenderUrl = senderUrl;
            Tuple = tuple;
            stringRepr = null;
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
