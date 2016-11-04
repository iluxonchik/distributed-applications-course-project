using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Operator.Tests
{
   public class OperatorBaseTestFixture
    {
        protected static readonly string BASE_DIR = TestContext.CurrentContext.TestDirectory;
        protected static readonly string RESOURCES_DIR = TestContext.CurrentContext.TestDirectory + "../../../resources/";

    }
}
