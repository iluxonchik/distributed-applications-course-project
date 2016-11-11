using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConfigTypes;
using NUnit.Framework;

namespace PuppetMaster.Tests
{
    public class TestPpmInitOP
    {
       static  OperatorSpec op1;
        static OperatorSpec op2;
        static OperatorSpec op3;
        static Command start;
        static Command freeze;
        static Command unFreeze;
        static Command interval;
        static Command crash;
        static Command status;
        protected readonly string BASE_DIR = TestContext.CurrentContext.TestDirectory;
        protected readonly string RESOURCES_DIR = TestContext.CurrentContext.TestDirectory + "../../../resources/";
        private static string inputFile = TestContext.CurrentContext.TestDirectory + "../../../resources/followers.dat";
        
        static void Main()
        {


            initOP();
            
            PuppetMasterControler ppm = new PuppetMasterControler();
            
            Console.WriteLine("press enter for ppm call pcs and create OP");
            Console.Read();
            Console.WriteLine("pressed enter");
            ppm.CreateOperators(op1);
            Console.WriteLine("press enter for ppm call pcs and create OP");
            Console.Read();
            Console.WriteLine("pressed enter");
        }

        private static void initOP()
        {// Empty
            // Build expected operator
            List<OperatorInput> expInputs = new List<OperatorInput>();
            expInputs.Add(new OperatorInput() { Name = inputFile, Type = InputType.File, Addresses = new List<string> { "tcp://localhost:11000/op" } });

            //List<string> expAddrs = new List<string>();
            //expAddrs.AddRange(new string[] { "tcp://1.2.3.8:11000/op", "tcp://1.2.3.9:11000/op" });

            //List<string> expArgs = new List<string>();
            //expArgs.Add("1");

            OperatorRouting expRouting = new OperatorRouting() { Type = RoutingType.Primary };

            List<OperatorOutput> expOutput = new List<OperatorOutput>();
            expOutput.Add(new OperatorOutput() { Name = "OP2", Addresses = new List<string>() { "tcp://localhost:9500/op" } });

            op1 = new OperatorSpec()
            {
                Id = "OP1",
                Inputs = expInputs,
                ReplicationFactor = 1,
                Routing = expRouting,
                OutputOperators = expOutput,
                Type = OperatorType.Dup,
                loginLevel = LoggingLevel.Light,
                semantics = Semantics.AtLeastOnce,
                //puppetMasterUrl= "tcp://localHost:7000",


            };

            //OP 2--------------------
            List<OperatorInput> expInputs1 = new List<OperatorInput>();
            expInputs1.Add(new OperatorInput() { Name = "OP1", Type = InputType.Operator, Addresses = new List<string> { "tcp://localHost:9000/op" } });
            List<OperatorOutput> expOutput1 = new List<OperatorOutput>();
            expOutput1.Add(new OperatorOutput() { Name = "OP3", Addresses = new List<string>() { "tcp://localhost:8086/op" } });


            op2 = new OperatorSpec()
            {
                Id = "OP2",
                Inputs = expInputs1,
                loginLevel = LoggingLevel.Light,
                semantics = Semantics.AtLeastOnce,
                Type = OperatorType.Dup,
                OutputOperators = expOutput1,
                ReplicationFactor = 1,
                Routing = expRouting

            };

            //OP 3--------------------
            List<OperatorInput> expInputs2 = new List<OperatorInput>();
            expInputs2.Add(new OperatorInput() { Name = "OP2", Type = InputType.Operator, Addresses = new List<string> { "tcp://localhost:9500/op" } });

            op3 = new OperatorSpec()
            {
                Id = "OP3",
                Inputs = expInputs2,
                loginLevel = LoggingLevel.Light,
                semantics = Semantics.AtLeastOnce,
                Routing = expRouting,
                Type = OperatorType.Count,
                ReplicationFactor = 1
            };


        }

        private static void InitCommands()
        {
            start = new Command()
            {
                Operator = op1,
                Type = CommandType.Start,
                RepId = 0,
                

            };
            status = new Command()
            {
                Operators = new List<OperatorSpec>() { op1, op2, op3 },
                Type = CommandType.Status,

            };
            freeze = new Command()
            {
                Operator = op1,
                Type = CommandType.Freeze,
                RepId = 0,
            };

            unFreeze = new Command()
            {
                Operator = op1,
                Type = CommandType.Unfreeze,
                RepId = 0,

            };

            interval = new Command()
            {
                Operator = op1,
                Type = CommandType.Interval,
                RepId = 0,
                MS = 100,
            };
            crash = new Command()
            {
                Operator = op1,
                Type = CommandType.Unfreeze,
                RepId = 0,
                MS = 100,
            };
            

        }
    }
}
