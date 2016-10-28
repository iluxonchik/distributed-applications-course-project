using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PuppetMaster;

namespace ProcessCreationProxy
{
    public interface IProcessCreationProxy
    {
       void CreateOperator(OperatorSpec opSpec);
    }
}
