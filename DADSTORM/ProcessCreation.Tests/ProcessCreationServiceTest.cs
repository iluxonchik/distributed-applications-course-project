using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProcessCreationProxy;
using ConfigTypes;

namespace ProcessCreation.Tests
{
    class ProcessCreationServiceTest
    {
        static void Main()
        {
            Console.WriteLine("press enter to call pcs service to boot operator");
            Console.ReadLine();
            IProcessCreationProxy pcs =(IProcessCreationProxy) Activator.GetObject(typeof(IProcessCreationProxy), "tcp://localhost:10000/ProcessCreation");
            //-------------
            List<OperatorInput> expInputs = new List<OperatorInput>();
            //just simple test
            expInputs.Add(new OperatorInput() { Name = "OP1", Type = InputType.Operator, Addresses = new List<string> { "tcp://localhost:11000/op" } });

            OperatorRouting expRouting = new OperatorRouting() { Type = RoutingType.Primary };

            List<OperatorOutput> expOutput = new List<OperatorOutput>();
            expOutput.Add(new OperatorOutput() { Name = "OP2", Addresses = new List<string>() { "tcp://localhost:9500/op" } });

            OperatorSpec spec = new OperatorSpec()
            {
                Id = "OP1",
                Inputs = expInputs,
                ReplicationFactor = 1,
                //Addrs = expAddrs,
                //Args = expArgs,
                Routing = expRouting,
                OutputOperators = expOutput,
                Type = OperatorType.Dup,
                Url = "tcp://localHost:9001/op",
                loginLevel = LoggingLevel.Light,
                semantics = Semantics.AtLeastOnce,
                //puppetMasterUrl= "tcp://localHost:7000",

            };
            //--------
            pcs.CreateOperator(spec);
        }
    }
}
