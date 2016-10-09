using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using System.IO;
using System.Reflection;
using PuppetMaster;

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



        private StreamReader simple_conf_file;

        public PuppetMasterSimpleConfigTest()
        {
            SIMPLE_CONF = RESOURCES_DIR + "simple_conf.config";
            SIMPLE_CONF_LOG_LIGHT = RESOURCES_DIR + "simple_conf_logging_light.config";
            SIMPLE_CONF_LOG_DEFAULT = RESOURCES_DIR + "simple_conf_logging_default.config";

            SIMPLE_CONF_SEM_AMO = SIMPLE_CONF;
            SIMPLE_CONF_SEM_ALO = SIMPLE_CONF_LOG_LIGHT;
            SIMPLE_CONF_SEM_EO = SIMPLE_CONF_LOG_DEFAULT;
        }

        [SetUp]
        public void SetUp()
        {
            /* 
             * Quick Hack: uncomment line below to make break points 
             * (select your VisualStudio instance when a window pops up). 
             */

            //System.Diagnostics.Debugger.Launch();
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

            Assert.That(Is.Equals(conf.LoggingLevel, LoggingLevel.FULL));

            cp = new ConfigParser(SIMPLE_CONF_LOG_LIGHT);
            conf = cp.Parse();

            Assert.That(Is.Equals(conf.LoggingLevel, LoggingLevel.LIGHT));

            cp = new ConfigParser(SIMPLE_CONF_LOG_DEFAULT);
            conf = cp.Parse();

            Assert.That(Is.Equals(conf.LoggingLevel, LoggingLevel.LIGHT));
        }

    }
}
