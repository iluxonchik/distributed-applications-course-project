using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OperatorProxys;

namespace PuppetMasterProxy
{
    public interface IPuppetMasterProxy
    {
         void ReportTuple(string OpId, int RepId, OperatorTuple tuple);
    }
}
