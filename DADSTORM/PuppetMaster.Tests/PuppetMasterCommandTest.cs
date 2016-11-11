using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PuppetMaster;
using ConfigTypes;
using NUnit.Framework;

namespace PuppetMaster.Tests
{
    [TestFixture]
    class PuppetMasterCommandTest : PuppetMasterBaseTestFixture
    {
        private Config config;
        OperatorSpec op1;
        OperatorSpec op2;
        OperatorSpec op3;
        [SetUp]
        public void SetUp()
        {
            /* 
             * Quick Hack: uncomment line below to make break points 
             * (select your VisualStudio instance when a window pops up). 
             */

            //System.Diagnostics.Debugger.Launch();
            config = new Config()
            {
                LoggingLevel = LoggingLevel.Light,
                Operators = new List<OperatorSpec>() { op1, op2, op3 },
                Semantics = Semantics.AtLeastOnce,
                commands = new Queue<Command>()
            };

            // Build expected operator
            List<OperatorInput> expInputs = new List<OperatorInput>();
            expInputs.Add(new OperatorInput() { Name = "sameFIle", Type = InputType.File, Addresses = new List<string> { "tcp://localhost:11000/op" } });
            List<string> expAddrs = new List<string>();
            expAddrs.AddRange(new string[] { "tcp://localhost:9500/op", "tcp://localhost:500/op" });
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
            expAddrs2.AddRange(new string[] { "tcp://localhost:8086/op", "tcp://localhost:8080/op" });
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

            config = new Config()
            {
                LoggingLevel = LoggingLevel.Light,
                Operators = new List<OperatorSpec>() { op1, op2, op3 },
                Semantics = Semantics.AtLeastOnce,
                commands = new Queue<Command>()
            };


        }
        [Test]
        public void TestRemoveDeadRep()
        {

            PuppetMasterControler ppm = new PuppetMasterControler(config);
            Assert.That(Is.Equals(config.Operators.Count, 3));
            Assert.That(Is.Equals(config.Operators[0].Addrs.Count, 2));
            Assert.That(Is.Equals(config.Operators[1].Addrs.Count, 2));
            Assert.That(Is.Equals(config.Operators[2].Addrs.Count, 1));

            ComparesAdds(config.Operators[0].Addrs, op1.Addrs);
            ComparesAdds(config.Operators[1].Addrs, op2.Addrs);
            ComparesAdds(config.Operators[2].Addrs, op3.Addrs);



            ppm.removeUrl("tcp://localhost:8080/op");
            Assert.That(Is.Equals(config.Operators.Count, 3));
            Assert.That(Is.Equals(config.Operators[0].Addrs.Count, 2));
            Assert.That(Is.Equals(config.Operators[1].Addrs.Count, 1));
            Assert.That(Is.Equals(config.Operators[2].Addrs.Count, 1));

            ComparesAdds(config.Operators[1].Addrs, new List<string>() { "tcp://localhost:8086/op" });

        }

        private void ComparesAdds(List<string> addrs1, List<string> addrs2)
        {
            for (int i = 0; i < addrs1.Count; i++)
            {
                Assert.That(Is.Equals(addrs1[i], addrs2[i]));

            }

        }




    }
}
