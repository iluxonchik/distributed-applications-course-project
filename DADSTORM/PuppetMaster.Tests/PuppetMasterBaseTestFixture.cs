using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuppetMaster.Tests
{
    /// <summary>
    /// Base class for all PuppetMasterTests, which contains some useful properties.
    /// </summary>
    class PuppetMasterBaseTestFixture
    {
        protected readonly string BASE_DIR = TestContext.CurrentContext.TestDirectory;
        protected readonly string RESOURCES_DIR = TestContext.CurrentContext.TestDirectory + "../../../resources/";
    }
}
