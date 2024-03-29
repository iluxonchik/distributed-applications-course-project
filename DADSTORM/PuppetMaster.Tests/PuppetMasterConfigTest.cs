﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using System.IO;
using System.Reflection;
using PuppetMaster;
using ConfigTypes;

namespace PuppetMaster.Tests
{
    /// <summary>
    /// Tests the simple_conf.config file and some realated variations 
    /// (for LoggingLevel and Semantics options). The "simple_conf.config" file contains all of the operator types,
    /// as well as all of the possible individual INPUT_OPS types (pointing to files and pointing to
    /// other operators). It ONLY tests the REP_FACT 1. It does not contains any lists.
    /// LoggingLevel is set to "full". Semantics are set to "at-most-once".
    /// 
    /// IT'S NOT A VALID CONFIG FILE, in a sense that
    /// you might have errros at runtime. Its syntax, is however, valid.
    /// </summary>
    [TestFixture]
    class PuppetMasterSimpleConfigTest: PuppetMasterBaseTestFixture
    {
        // File names for LoggingLevel parsing
        private readonly string SIMPLE_CONF;
        private readonly string SIMPLE_CONF_LOG_LIGHT;
        private readonly string SIMPLE_CONF_LOG_DEFAULT;

        // File names for Semanthics parsing. Uses abbreviations (ex: EO = exactly once)
        private readonly string SIMPLE_CONF_SEM_AMO;
        private readonly string SIMPLE_CONF_SEM_ALO;
        private readonly string SIMPLE_CONF_SEM_EO;
        private readonly string SIMPLE_CONF_SEM_DEFAULT;



        private StreamReader simple_conf_file;

        public PuppetMasterSimpleConfigTest()
        {
            SIMPLE_CONF = RESOURCES_DIR + "simple_conf.config";
            SIMPLE_CONF_LOG_LIGHT = RESOURCES_DIR + "simple_conf_logging_light.config";
            SIMPLE_CONF_LOG_DEFAULT = RESOURCES_DIR + "simple_conf_logging_default.config";

            SIMPLE_CONF_SEM_AMO = SIMPLE_CONF;
            SIMPLE_CONF_SEM_ALO = SIMPLE_CONF_LOG_LIGHT;
            SIMPLE_CONF_SEM_EO = SIMPLE_CONF_LOG_DEFAULT;
            SIMPLE_CONF_SEM_DEFAULT = RESOURCES_DIR + "simple_conf_semantics_default.config";
        }

        [SetUp]
        public void SetUp()
        {
            /* 
             * Quick Hack: uncomment line below to make break points 
             * (select your VisualStudio instance when a window pops up). 
             */

            // System.Diagnostics.Debugger.Launch();
            simple_conf_file = new StreamReader(SIMPLE_CONF);
        }

        [TearDown]
        public void TearDown()
        {
            // Empty
        }

        /// <summary>
        /// Test all possible variations of LoggingLevelValues.
        /// </summary>
        [Test]
        public void TestLoggingLevelParsing()
        {
            ConfigParser cp = new ConfigParser(SIMPLE_CONF);
            Config conf = cp.Parse();

            Assert.That(Is.Equals(conf.LoggingLevel, LoggingLevel.Full));

            cp = new ConfigParser(SIMPLE_CONF_LOG_LIGHT);
            conf = cp.Parse();

            Assert.That(Is.Equals(conf.LoggingLevel, LoggingLevel.Light));

            cp = new ConfigParser(SIMPLE_CONF_LOG_DEFAULT);
            conf = cp.Parse();

            Assert.That(Is.Equals(conf.LoggingLevel, LoggingLevel.Light));
        }

        /// <summary>
        /// Test all possible combinations of Semantics values.
        /// </summary>
        [Test]
        public void TestSemanticsParsing()
        {
            ConfigParser cp = new ConfigParser(SIMPLE_CONF_SEM_ALO);
            Config conf = cp.Parse();

            Assert.That(Is.Equals(conf.Semantics, Semantics.AtLeastOnce));

            cp = new ConfigParser(SIMPLE_CONF_SEM_AMO);
            conf = cp.Parse();

            Assert.That(Is.Equals(conf.Semantics, Semantics.AtMostOnce));

            cp = new ConfigParser(SIMPLE_CONF_SEM_EO);
            conf = cp.Parse();

            Assert.That(Is.Equals(conf.Semantics, Semantics.ExactlyOnce));

            cp = new ConfigParser(SIMPLE_CONF_SEM_DEFAULT);
            conf = cp.Parse();

            Assert.That(Is.Equals(conf.Semantics, Semantics.AtLeastOnce));
        }

        /* NOTE: I know that operator tests could've used more isolation, but there isn't enough
         * time for that, so the parsing will be done all at once, then just checks to make sure
         * that the desired operators are present within the parsed list.
         */

        /// <summary>
        /// Test that the expected amount of operators is parsed.
        /// </summary>
        [Test]
        public void TestOperatorCountOther()
        {
            ConfigParser cp = new ConfigParser(SIMPLE_CONF);
            Config conf = cp.Parse();
            Assert.That(conf.Operators, Is.Not.Null.Or.Empty, "Operator config list is null or empty");
            Assert.That(Is.Equals(conf.Operators.Count, 5));
        }
       
        [Test]
        public void TestUniqOperatorSimple()
        {
            ConfigParser cp = new ConfigParser(SIMPLE_CONF);
            Config conf = cp.Parse();

            // Build expected operator
            List<OperatorInput> expInputs = new List<OperatorInput>();
            expInputs.Add(new OperatorInput() { Name = "1992", Type = InputType.Operator });

            List<string> expAddrs = new List<string>();
            expAddrs.Add("tcp://1.2.3.4:11000/protege-of-the-d-r-e");
             
            List<string> expArgs = new List<string>();
            expArgs.Add("2");

            OperatorRouting expRouting = new OperatorRouting() { Type = RoutingType.Random };

            OperatorSpec expected = new OperatorSpec()
            {
                Id = "OP1",
                Inputs = expInputs,
                ReplicationFactor = 1,
                Addrs = expAddrs,
                Args = expArgs,
                Routing = expRouting,
                Type = OperatorType.Uniq,
                Semantics = Semantics.AtMostOnce,
                LoggingLevel = LoggingLevel.Full
            };

            Assert.That(conf.Operators, Does.Contain(expected).Using(new OperatorSpec.OperatorSpecComparer()));
        }

    }

    /// <summary>
    /// Test the providec config file, along with an additon to that, which includes
    /// testing the DUP operator as well as multiple input operators.
    /// </summary>
    [TestFixture]
    class PuppetMasterProvidedConfigTest : PuppetMasterBaseTestFixture
    {
        private StreamReader provided_config;
        private readonly string PROVIDED_CONF;
        private readonly string COMPLEMENTED_CONF;
        private readonly string FULL_CONFIG;

        public PuppetMasterProvidedConfigTest()
        {
            PROVIDED_CONF = RESOURCES_DIR + "provided_config.config";
            COMPLEMENTED_CONF = RESOURCES_DIR + "complemented_provided_config.config";
            FULL_CONFIG = RESOURCES_DIR + "dadstorm_config_full.conf";
            
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

        [TearDown]
        public void TearDown()
        {
            provided_config.Close();
        }

        #region ProvidedConfig Tests
        [Test]
        public void TestLoggingLevelParsingSimple()
        {
            ConfigParser cp = new ConfigParser(PROVIDED_CONF);
            Config conf = cp.Parse();

            Assert.That(Is.Equals(conf.LoggingLevel, LoggingLevel.Light));
        }

        /// <summary>
        /// Test all possible combinations of Semantics values.
        /// </summary>
        [Test]
        public void TestSemanticsParsingSimple()
        {
            ConfigParser cp = new ConfigParser(PROVIDED_CONF);
            Config conf = cp.Parse();

            Assert.That(Is.Equals(conf.Semantics, Semantics.AtMostOnce));
        }

        [Test]
        public void TestOperatorCount()
        {
            ConfigParser cp = new ConfigParser(PROVIDED_CONF);
            Config conf = cp.Parse();
            Assert.That(conf.Operators, Is.Not.Null.Or.Empty, "Operator config list is null or empty");
            Assert.That(Is.Equals(conf.Operators.Count, 4), string.Format("Expected: 4, Actual: {0}", conf.Operators.Count));
        }

        [Test]
        public void TestUniqOperator()
        {
            ConfigParser cp = new ConfigParser(PROVIDED_CONF);
            Config conf = cp.Parse();

            // Build expected operator
            List<OperatorInput> expInputs = new List<OperatorInput>();
            expInputs.Add(new OperatorInput() { Name = "OP2", Type = InputType.Operator, Addresses = new List<string> { "tcp://1.2.3.6:11000/op", "tcp://1.2.3.6:11001/op" } });

            List<string> expAddrs = new List<string>();
            expAddrs.AddRange(new string[] { "tcp://1.2.3.8:11000/op", "tcp://1.2.3.9:11000/op" });

            List<string> expArgs = new List<string>();
            expArgs.Add("1");

            OperatorRouting expRouting = new OperatorRouting() { Type = RoutingType.Hashing, Arg = 1 };

            List<OperatorOutput> expOutput = new List<OperatorOutput>();
            expOutput.Add(new OperatorOutput() { Name = "OP4", Addresses = new List<string>() { "tcp://1.2.3.10:11000/op" } });

            OperatorSpec expected = new OperatorSpec()
            {
                Id = "OP3",
                Inputs = expInputs,
                ReplicationFactor = 2,
                Addrs = expAddrs,
                Args = expArgs,
                Routing = expRouting,
                OutputOperators = expOutput,
                Type = OperatorType.Uniq,
                Semantics = Semantics.AtMostOnce,
                LoggingLevel = LoggingLevel.Light
            };

            Assert.That(conf.Operators, Does.Contain(expected).Using(new OperatorSpec.OperatorSpecComparer()));
        }

        [Test]
        public void TestFilterOperator()
        {
            ConfigParser cp = new ConfigParser(PROVIDED_CONF);
            Config conf = cp.Parse();

            // Build expected operator
            List<OperatorInput> expInputs = new List<OperatorInput>();
            expInputs.Add(new OperatorInput() { Name = "tweeters.data", Type = InputType.File });

            List<string> expAddrs = new List<string>();
            expAddrs.AddRange(new string[] { "tcp://1.2.3.4:11000/op", "tcp://1.2.3.5:11000/op" });

            List<string> expArgs = new List<string>();
            expArgs.Add("3", "=", @"""www.tecnico.ulisboa.pt""");

            OperatorRouting expRouting = new OperatorRouting() { Type = RoutingType.Hashing, Arg = 1 };

            List<OperatorOutput> expOutput = new List<OperatorOutput>();
            expOutput.Add(new OperatorOutput() { Name = "OP2", Addresses = new List<string> { "tcp://1.2.3.6:11000/op", "tcp://1.2.3.6:11001/op" } });

            OperatorSpec expected = new OperatorSpec()
            {
                Id = "OP1",
                Inputs = expInputs,
                ReplicationFactor = 2,
                Addrs = expAddrs,
                Args = expArgs,
                Routing = expRouting,
                OutputOperators = expOutput,
                Type = OperatorType.Filter,
                Semantics = Semantics.AtMostOnce,
                LoggingLevel = LoggingLevel.Light
            };

            Assert.That(conf.Operators, Does.Contain(expected).Using(new OperatorSpec.OperatorSpecComparer()));
        }

        [Test]
        public void TestCustomOperator()
        {
            ConfigParser cp = new ConfigParser(PROVIDED_CONF);
            Config conf = cp.Parse();

            // Build expected operator
            List<OperatorInput> expInputs = new List<OperatorInput>();
            expInputs.Add(new OperatorInput() { Name = "OP1", Type = InputType.Operator, Addresses = new List<string> { "tcp://1.2.3.4:11000/op", "tcp://1.2.3.5:11000/op" } });

            List<string> expAddrs = new List<string>();
            expAddrs.AddRange(new string[] { "tcp://1.2.3.6:11000/op", "tcp://1.2.3.6:11001/op" });

            List<string> expArgs = new List<string>();
            expArgs.Add("mylib.dll", "QueryFollowersFile", "getFollowers");

            OperatorRouting expRouting = new OperatorRouting() { Type = RoutingType.Random};

            List<OperatorOutput> expOutput = new List<OperatorOutput>();
            expOutput.Add(new OperatorOutput() { Name = "OP3", Addresses = new List<string> { "tcp://1.2.3.8:11000/op", "tcp://1.2.3.9:11000/op" } });

            OperatorSpec expected = new OperatorSpec()
            {
                Id = "OP2",
                Inputs = expInputs,
                ReplicationFactor = 2,
                Addrs = expAddrs,
                Args = expArgs,
                Routing = expRouting,
                OutputOperators = expOutput,
                Type = OperatorType.Custom,
                Semantics = Semantics.AtMostOnce,
                LoggingLevel = LoggingLevel.Light
            };

            Assert.That(conf.Operators, Does.Contain(expected).Using(new OperatorSpec.OperatorSpecComparer()));
        }

        [Test]
        public void TestCountOperator()
        {
            ConfigParser cp = new ConfigParser(PROVIDED_CONF);
            Config conf = cp.Parse();

            // Build expected operator
            List<OperatorInput> expInputs = new List<OperatorInput>();
            expInputs.Add(new OperatorInput() { Name = "OP3", Type = InputType.Operator, Addresses = new List<string> { "tcp://1.2.3.8:11000/op", "tcp://1.2.3.9:11000/op" } });

            List<string> expAddrs = new List<string>();
            expAddrs.Add("tcp://1.2.3.10:11000/op");

            List <string> expArgs = new List<string>();

            OperatorRouting expRouting = new OperatorRouting() { Type = RoutingType.Primary };

            OperatorSpec expected = new OperatorSpec()
            {
                Id = "OP4",
                Inputs = expInputs,
                ReplicationFactor = 1,
                Args = expArgs,
                Addrs = expAddrs,
                Routing = expRouting,
                Type = OperatorType.Count,
                Semantics = Semantics.AtMostOnce,
                LoggingLevel = LoggingLevel.Light
            };

            Assert.That(conf.Operators, Does.Contain(expected).Using(new OperatorSpec.OperatorSpecComparer()));
        }
        #endregion

        #region ComplementedConfig Tests
        /// <summary>
        /// Make sure that the extra operator in complement cofig is included in te count.
        /// </summary>
        [Test]
        public void TestComplementedConfigOperatorCount()
        {
            ConfigParser cp = new ConfigParser(COMPLEMENTED_CONF);
            Config conf = cp.Parse();
            Assert.That(conf.Operators, Is.Not.Null.Or.Empty, "Operator config list is null or empty");
            Assert.That(Is.Equals(conf.Operators.Count, 5), string.Format("Expected: 5, Actual: {0}", conf.Operators.Count));
        }

        /// <summary>
        /// This test uses the provided config with an additional, more complex DUP input
        /// operator.
        /// </summary>
        [Test]
        public void TestDupOperator()
        {
            ConfigParser cp = new ConfigParser(COMPLEMENTED_CONF);
            Config conf = cp.Parse();

            // Build expected operator
            List<OperatorInput> expInputs = new List<OperatorInput>();
            expInputs.Add(new OperatorInput() { Name = "OP1", Type = InputType.Operator, Addresses = new List<string> { "tcp://1.2.3.4:11000/op", "tcp://1.2.3.5:11000/op" } });
            expInputs.Add(new OperatorInput() { Name = "OP2", Type = InputType.Operator, Addresses = new List<string> { "tcp://1.2.3.6:11000/op", "tcp://1.2.3.6:11001/op" } });
            expInputs.Add(new OperatorInput() { Name = "Dr.Dre", Type = InputType.File });

            List<string> expAddrs = new List<string>();
            expAddrs.Add("tcp://1.9.9.2:1410/ninety-ninety-two", "tcp://2.0.0.5:1801/the-documentary");

            List <string> expArgs = new List<string>();

            OperatorRouting expRouting = new OperatorRouting() { Type = RoutingType.Primary };

            OperatorSpec expected = new OperatorSpec()
            {
                Id = "OP5",
                Inputs = expInputs,
                ReplicationFactor = 2,
                Addrs = expAddrs,
                Args = expArgs,
                Routing = expRouting,
                Type = OperatorType.Dup,
                Semantics = Semantics.AtMostOnce,
                LoggingLevel = LoggingLevel.Light
            };

            Assert.That(conf.Operators, Does.Contain(expected).Using(new OperatorSpec.OperatorSpecComparer()));
        }

        /// <summary>
        /// This test uses the provided config with an additional, more complex DUP input
        /// operator and test that OP1 has it in its OutputOperator property.
        /// </summary>
        [Test]
        public void TestOP1OutputsToDupOperator()
        {
            ConfigParser cp = new ConfigParser(COMPLEMENTED_CONF);
            Config conf = cp.Parse();

            // Build expected operator
            List<OperatorInput> expInputs = new List<OperatorInput>();
            expInputs.Add(new OperatorInput() { Name = "tweeters.data", Type = InputType.File });

            List<string> expAddrs = new List<string>();
            expAddrs.AddRange(new string[] { "tcp://1.2.3.4:11000/op", "tcp://1.2.3.5:11000/op" });

            List<string> expArgs = new List<string>();
            expArgs.Add("3", "=", @"""www.tecnico.ulisboa.pt""");

            OperatorRouting expRouting = new OperatorRouting() { Type = RoutingType.Hashing, Arg = 1 };

            List<OperatorOutput> expOutput = new List<OperatorOutput>();
            expOutput.Add(new OperatorOutput() { Name = "OP2", Addresses = new List<string> { "tcp://1.2.3.6:11000/op", "tcp://1.2.3.6:11001/op" } });
            expOutput.Add(new OperatorOutput() { Name = "OP5", Addresses = new List<string> { "tcp://1.9.9.2:1410/ninety-ninety-two", "tcp://2.0.0.5:1801/the-documentary" } });

            OperatorSpec expected = new OperatorSpec()
            {
                Id = "OP1",
                Inputs = expInputs,
                ReplicationFactor = 2,
                Addrs = expAddrs,
                Args = expArgs,
                Routing = expRouting,
                OutputOperators = expOutput,
                Type = OperatorType.Filter,
                Semantics = Semantics.AtMostOnce,
                LoggingLevel = LoggingLevel.Light
            };

            Assert.That(conf.Operators, Does.Contain(expected).Using(new OperatorSpec.OperatorSpecComparer()));
        }

        /// <summary>
        /// This test uses the provided config with an additional, more complex DUP input
        /// operator and test that OP2 has it in its OutputOperator property.
        /// </summary>
        [Test]
        public void TestOP2OutputsToDupOperator()
        {
            ConfigParser cp = new ConfigParser(COMPLEMENTED_CONF);
            Config conf = cp.Parse();

            // Build expected operator
            List<OperatorInput> expInputs = new List<OperatorInput>();
            expInputs.Add(new OperatorInput() { Name = "OP1", Type = InputType.Operator, Addresses = new List<string> { "tcp://1.2.3.4:11000/op", "tcp://1.2.3.5:11000/op" } });

            List<string> expAddrs = new List<string>();
            expAddrs.AddRange(new string[] { "tcp://1.2.3.6:11000/op", "tcp://1.2.3.6:11001/op" });

            List<string> expArgs = new List<string>();
            expArgs.Add("mylib.dll", "QueryFollowersFile", "getFollowers");

            OperatorRouting expRouting = new OperatorRouting() { Type = RoutingType.Random };

            List<OperatorOutput> expOutput = new List<OperatorOutput>();
            expOutput.Add(new OperatorOutput() { Name = "OP3", Addresses = new List<string> { "tcp://1.2.3.8:11000/op", "tcp://1.2.3.9:11000/op" } });
            expOutput.Add(new OperatorOutput() { Name = "OP5", Addresses = new List<string> { "tcp://1.9.9.2:1410/ninety-ninety-two", "tcp://2.0.0.5:1801/the-documentary" } });

            OperatorSpec expected = new OperatorSpec()
            {
                Id = "OP2",
                Inputs = expInputs,
                ReplicationFactor = 2,
                Addrs = expAddrs,
                Args = expArgs,
                Routing = expRouting,
                OutputOperators = expOutput,
                Type = OperatorType.Custom,
                Semantics = Semantics.AtMostOnce,
                LoggingLevel = LoggingLevel.Light
            };

            Assert.That(conf.Operators, Does.Contain(expected).Using(new OperatorSpec.OperatorSpecComparer()));
        }

        [Test]
        public void TestSetPuppetMasterUrl()
        {
            const string URL = "The Game - The Documentary(2005)";
            ConfigParser cp = new ConfigParser(PROVIDED_CONF);
            Config conf = cp.Parse();

            conf.SetPuppetMasterUrl(URL);

            foreach(var ops in conf.Operators)
            {
                Assert.That(Is.Equals(ops.PuppetMasterUrl, URL));
            }
        }
        #endregion

        #region DADSTORM Full Config Tests
        [Test]
        public void TestCustomNamespace()
        {
            ConfigParser cp = new ConfigParser(FULL_CONFIG);
            Config conf = cp.Parse();

            // Build expected operator
            List<OperatorInput> expInputs = new List<OperatorInput>();
            expInputs.Add(new OperatorInput() { Name = "OP4", Type = InputType.Operator, Addresses = new List<string> { "tcp://localhost:11006/op" } });

            List<string> expAddrs = new List<string>();
            expAddrs.AddRange(new string[] { "tcp://localhost:11008/op" });

            List<string> expArgs = new List<string>();
            expArgs.Add("mylib.dll", "LibCustomOperator.OutputOperator", "CustomOperation");

            OperatorRouting expRouting = new OperatorRouting() { Type = RoutingType.Primary };

            OperatorSpec expected = new OperatorSpec()
            {
                Id = "OP5",
                Inputs = expInputs,
                ReplicationFactor = 1,
                Addrs = expAddrs,
                Args = expArgs,
                Routing = expRouting,
                // OutputOperators = null,
                Type = OperatorType.Custom,
                Semantics = Semantics.AtMostOnce,
                LoggingLevel = LoggingLevel.Full
            };

            Assert.That(conf.Operators, Does.Contain(expected).Using(new OperatorSpec.OperatorSpecComparer()));
        }
        #endregion
    }

}
