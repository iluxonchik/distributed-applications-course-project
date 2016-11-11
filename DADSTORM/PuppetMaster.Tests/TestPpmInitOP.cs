using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConfigTypes;
using System.IO;

namespace PuppetMaster.Tests
{
    public class TestPpmInitOP
    {
        
        private static string inputFile = Directory.GetCurrentDirectory() + "../../../resources/followers.dat";

        static void Main()
        {
            OperatorSpec op1;
            OperatorSpec op2;
            OperatorSpec op3;
            Command start1;
            Command freeze2;
            Command start2;
            Command unFreeze2;
            Command wait1;
            Command crash1;
            Command status;
            Command start3;
            Command interval3;
            Command wait2;
            Command crash2;
            Command crash3;

            // Build expected operator
            List<OperatorInput> expInputs = new List<OperatorInput>();
            expInputs.Add(new OperatorInput() { Name = inputFile, Type = InputType.File, Addresses = new List<string> { "tcp://localhost:11000/op" } });
            List<string> expAddrs = new List<string>();
            expAddrs.AddRange(new string[] { "tcp://localhost:9500/op" });
            OperatorRouting expRouting = new OperatorRouting() { Type = RoutingType.Primary };
            List<OperatorOutput> expOutput = new List<OperatorOutput>();
            expOutput.Add(new OperatorOutput() { Name = "OP2", Addresses = new List<string>() { "tcp://localhost:8086/op" } });

            op1 = new OperatorSpec()
            {
                Id = "OP1",
                Inputs = expInputs,
                ReplicationFactor = 1,
                Routing = expRouting,
                OutputOperators = expOutput,
                Type = OperatorType.Dup,
                LoggingLevel = LoggingLevel.Light,
                Semantics = Semantics.AtLeastOnce,
                Addrs = expAddrs,
            };

            //OP 2--------------------
            List<string> expAddrs2 = new List<string>();
            expAddrs2.AddRange(new string[] { "tcp://localhost:8086/op" });
            List<OperatorInput> expInputs1 = new List<OperatorInput>();
            expInputs1.Add(new OperatorInput() { Name = "OP1", Type = InputType.Operator, Addresses = new List<string> { "tcp://localHost:9500/op" } });
            List<OperatorOutput> expOutput1 = new List<OperatorOutput>();
            expOutput1.Add(new OperatorOutput() { Name = "OP3", Addresses = new List<string>() { "tcp://localhost:9550/op" } });

            op2 = new OperatorSpec()
            {
                Id = "OP2",
                Inputs = expInputs1,
                LoggingLevel = LoggingLevel.Light,
                Semantics = Semantics.AtLeastOnce,
                Type = OperatorType.Dup,
                OutputOperators = expOutput1,
                ReplicationFactor = 1,
                Routing = expRouting,
                Addrs = expAddrs2,
            };

            //OP 3--------------------
            List<string> expAddrs3 = new List<string>();
            expAddrs3.AddRange(new string[] { "tcp://localhost:9550/op" });
            List<OperatorInput> expInputs2 = new List<OperatorInput>();
            expInputs2.Add(new OperatorInput() { Name = "OP2", Type = InputType.Operator, Addresses = new List<string> { "tcp://localhost:8086/op" } });

            op3 = new OperatorSpec()
            {
                Id = "OP3",
                Inputs = expInputs2,
                LoggingLevel = LoggingLevel.Light,
                Semantics = Semantics.AtLeastOnce,
                Routing = expRouting,
                Type = OperatorType.Count,
                ReplicationFactor = 1,
                Addrs = expAddrs3
            };
            

            start1 = new Command()
            {
                Operator = op1,
                Type = CommandType.Start,
                RepId = 0,
            };
            freeze2 = new Command()
            {
                Operator = op2,
                Type = CommandType.Freeze,
                RepId = 0,
            };
            start2 = new Command()
            {
                Operator = op2,
                Type = CommandType.Start,
                RepId = 0,
            };
            unFreeze2 = new Command()
            {
                Operator = op2,
                Type = CommandType.Unfreeze,
                RepId = 0,
            };
            wait1 = new Command()
            {
                Type = CommandType.Wait,
                MS = 2000,
            };
            crash1 = new Command()
            {
                Operator = op1,
                Type = CommandType.Crash,
                RepId = 0
            };
            status = new Command()
            {
                Operators = new List<OperatorSpec>() { op1, op2, op3 },
                Type = CommandType.Status,
            };
            start3 = new Command()
            {
                Operator = op3,
                Type = CommandType.Start,
                RepId = 0,
            };
            interval3 = new Command()
            {
                Operator = op3,
                Type = CommandType.Interval,
                RepId = 0,
                MS = 100
            };
            wait2 = new Command()
            {
                Type = CommandType.Wait,
                MS = 2000,
            };
            crash2 = new Command()
            {
                Operator = op2,
                Type = CommandType.Crash,
                RepId = 0
            };
            crash3 = new Command()
            {
                Operator = op3,
                Type = CommandType.Crash,
                RepId = 0
            };

            List<Command> aux = new List<Command>()
            {
                start1,
             freeze2,
             start2,
             unFreeze2,
             wait1 ,
             crash1 ,
             status ,
              interval3 ,
             start3 ,
            
              status,
             wait2 ,
             crash2 ,
             crash3
            };
            
            Config config = new Config()
            {
                LoggingLevel = LoggingLevel.Light,
                Operators = new List<OperatorSpec>() { op1, op2, op3 },
                Semantics = Semantics.AtLeastOnce,
                commands = new Queue<Command>()
            };
            PuppetMasterControler ppm = new PuppetMasterControler(config);
            
            foreach (Command c in aux)
            {
                ppm.AddCommand(c);
            }
            

            Console.WriteLine("press enter for ppm call pcs and create OP");
            Console.Read();
            Console.WriteLine("pressed enter");
            ppm.CreateOperators();
            Console.Read();
            Console.Read();
            foreach (Command c in aux)
            {
                Console.WriteLine("press enter to run command");
                Console.Read();
                Console.Read();
                ppm.Step();
                Console.WriteLine("pressed enter");
            }

        }

    }
}
