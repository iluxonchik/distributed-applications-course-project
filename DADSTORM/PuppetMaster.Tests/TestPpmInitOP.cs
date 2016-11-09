using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConfigTypes;

namespace PuppetMaster.Tests
{
    public class TestPpmInitOP
    {
        static void Main()
        {
            //Process.Start(PCSPathExec);
            List<OperatorInput> expInputs = new List<OperatorInput>();
            expInputs.Add(new OperatorInput() { Name = "sameFIle", Type = InputType.Operator, Addresses = new List<string> { "tcp://localhost:11000/op" } });

            //List<string> expAddrs = new List<string>();
            //expAddrs.AddRange(new string[] { "tcp://1.2.3.8:11000/op", "tcp://1.2.3.9:11000/op" });

            //List<string> expArgs = new List<string>();
            //expArgs.Add("1");

            OperatorRouting expRouting = new OperatorRouting() { Type = RoutingType.Primary };

            List<OperatorOutput> expOutput = new List<OperatorOutput>();
            expOutput.Add(new OperatorOutput() { Name = "OP2", Addresses = new List<string>() { "tcp://localhost:9500/op" } });

            OperatorSpec op1 = new OperatorSpec()
            {
                Id = "OP1",
                Inputs = expInputs,
                ReplicationFactor = 1,
                //Addrs = expAddrs,
                //Args = expArgs,
                Routing = expRouting,
                OutputOperators = expOutput,
                Type = OperatorType.Dup,
                Url = "tcp://localHost:9000/op",
                loginLevel = LoggingLevel.Light,
                semantics = Semantics.AtLeastOnce,
                //puppetMasterUrl= "tcp://localHost:7000",

            };

            PuppetMasterControler ppm = new PuppetMasterControler();
            Console.WriteLine("press enter for ppm call pcs and create OP");
            Console.Read();
            Console.WriteLine("pressed enter");
            ppm.CreateOperators(op1);
        }
    }
}
