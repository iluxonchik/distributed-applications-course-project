using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessCreation.Tests
{
    /// <summary>
    /// Base class for all ProcesCreateion.Test test fixtures, which contains some useful properties.
    /// </summary>
    public class ProcessCreationBaseTestFixture
    {
        protected static readonly string BASE_DIR = TestContext.CurrentContext.TestDirectory;
        protected static readonly string RESOURCES_DIR = TestContext.CurrentContext.TestDirectory + "../../../resources/";
    }
}
