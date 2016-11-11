using ConfigTypes;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PuppetMaster;
using Newtonsoft.Json;

namespace PuppetMaster.Tests
{
    [TestFixture]
    class PuppetMasterCommandParsingTest : PuppetMasterBaseTestFixture
    {
        private readonly string PROVIDED_CONF;
        private StreamReader provided_config;

        public PuppetMasterCommandParsingTest()
        {
            PROVIDED_CONF = RESOURCES_DIR + "provided_config.config";
        }

        [SetUp]
        public void SetUp()
        {
            /* 
             * Quick Hack: uncomment line below to make break points 
             * (select your VisualStudio instance when a window pops up). 
             */

            //System.Diagnostics.Debugger.Launch();
            provided_config = new StreamReader(PROVIDED_CONF);
        }

        [Test]
        public void TestCommandCount()
        {
            ConfigParser cp = new ConfigParser(PROVIDED_CONF);
            Config conf = cp.Parse();
            CommandParser comParser = new CommandParser(PROVIDED_CONF, conf);
            Queue<Command> commands = comParser.Parse();
            Assert.That(Is.Equals(12, commands.Count), String.Format("{0} != {1} [Obtained:Expected]", commands.Count, 12));
        }

        /// <summary>
        /// Test order + command attributes, yes mixing two things in one test, but it's 3:30 AM, time is tight and both will still be tested.
        /// </summary>
        [Test]
        public void TestCommandOrder()
        {
            ConfigParser cp = new ConfigParser(PROVIDED_CONF);
            Config conf = cp.Parse();
            CommandParser comParser = new CommandParser(PROVIDED_CONF, conf);
            Queue<Command> commands = comParser.Parse();

            Command c1 = new Command() { Type = CommandType.Interval, Operator = conf.OPnameToOpSpec["OP1"], MS = 500};
            Command c2 = new Command() { Type = CommandType.Status };
            Command c3 = new Command() { Type = CommandType.Start, Operator = conf.OPnameToOpSpec["OP1"] };
            Command c4 = new Command() { Type = CommandType.Start, Operator = conf.OPnameToOpSpec["OP2"] };
            Command c5 = new Command() { Type = CommandType.Start, Operator = conf.OPnameToOpSpec["OP3"] };
            Command c6 = new Command() { Type = CommandType.Start, Operator = conf.OPnameToOpSpec["OP4"] };
            Command c7 = new Command() { Type = CommandType.Status };
            Command c8 = new Command() { Type = CommandType.Crash, Operator = conf.OPnameToOpSpec["OP1"], RepId = 0 };
            Command c9 = new Command() { Type = CommandType.Freeze, Operator = conf.OPnameToOpSpec["OP2"], RepId = 1 };
            Command c10 = new Command() { Type = CommandType.Wait, MS = 10000 };
            Command c11 = new Command() { Type = CommandType.Unfreeze, Operator = conf.OPnameToOpSpec["OP2"], RepId = 1 };
            Command c12 = new Command() { Type = CommandType.Status };

            Queue<Command> expectedQueue = new Queue<Command>();
            expectedQueue.Enqueue(c1);
            expectedQueue.Enqueue(c2);
            expectedQueue.Enqueue(c3);
            expectedQueue.Enqueue(c4);
            expectedQueue.Enqueue(c5);
            expectedQueue.Enqueue(c6);
            expectedQueue.Enqueue(c7);
            expectedQueue.Enqueue(c8);
            expectedQueue.Enqueue(c9);
            expectedQueue.Enqueue(c10);
            expectedQueue.Enqueue(c11);
            expectedQueue.Enqueue(c12);

            for (int i = 0; i < commands.Count; i++)
            {
                Command actual = commands.Dequeue();
                Command expected = expectedQueue.Dequeue();
                actual.ThrowExceptionOnInvalidGet = false;
                expected.ThrowExceptionOnInvalidGet = false;

                AreEqualByJson(expected, actual);
            }

        }
        [TearDown]
        public void TearDown()
        {
            provided_config.Close();
        }

        public static void AreEqualByJson(object expected, object actual)
        {
            bool areEqual = (JsonConvert.SerializeObject(expected).CompareTo(JsonConvert.SerializeObject(actual)) == 0);
            Assert.That(areEqual);
        }
    }
}
