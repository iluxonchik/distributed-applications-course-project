using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OperatorProxys
{ [Serializable]
   public  class OperatorTuple
    {
     public   List<string> Tuple { get; set; }

    public OperatorTuple (List<String> tuple)
        {
            this.Tuple = tuple;
        }
        
    }
}
