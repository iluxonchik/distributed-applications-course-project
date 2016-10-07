using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace PuppetMaster.Tests
{
    /// <summary>
    /// Tests the simple_conf.config file. This file contains all of the operator types,
    /// as well as all of the possible individual INPUT_OPS types (pointing to files and pointing to
    /// other operators). It ONLY tests the REP_FACT 1. It does not contains any lists.
    /// LoggingLevel is set to "full". Semantics are set to "at-most-once".
    /// 
    /// IT'S NOT A VALID CONFIG FILE, in a sense that
    /// you might have errros at runtime. Its syntax, is however, valid.
    /// </summary>
    [TestFixture]
    public class PuppetMasterSimpleConfigTest
    {

        [SetUp]
        public void SetUp()
        {
            // TODO
        }

        [TearDown]
        public void TearDown()
        {
            // TODO
        }

        [Test]
        public void TestTest()
        {
            // Should be removed once actual tests are written
            Assert.That(true);
        }

    }
}
