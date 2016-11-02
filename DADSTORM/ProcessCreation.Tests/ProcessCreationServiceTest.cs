using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProcessCreationProxy;
using PuppetMaster;

namespace ProcessCreation.Tests
{
    class ProcessCreationServiceTest
    {
        static void Main()
        {
            Console.ReadLine();
            IProcessCreationProxy pcs =(IProcessCreationProxy) Activator.GetObject(typeof(IProcessCreationProxy), "tcp://localhost:10000/ProcessCreation");
            OperatorSpec spec = new OperatorSpec();
            spec.Type = PuppetMaster.OperatorType.Count;
            pcs.CreateOperator(spec);
        }
    }
}
