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
        private string t;
        public List<string> Tuple { get; private set; }


        public OperatorTuple(List<String> tuple)
        {
            this.Tuple = tuple;
            this.t = null;
        }

        public void SetTuple(List<String> tuple)
        {
            this.Tuple = tuple;
            this.t = null;
        }

        public override string ToString()
        {
            if (t != null)
                return t;
            t = "";
            foreach (string s in this.Tuple)
            {
                t += s + " ";
            }
            return t;
        }

    }

}
