using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using System.IO;
using System.Diagnostics;
using PuppetMaster;

namespace ProcessCreation.Tests
{
    [TestFixture]
    public class ProcessCreationTests
    {
    
        private  string  operatorPathExec = TestContext.CurrentContext.TestDirectory + "../../../resources/Operator.exe";
        [SetUp]
        public void SetUp()
        {
            // Empty
        }

        [TearDown]
        public void TearDown()
        {
            // Empty
        }
        //[Test]
        //public void TestInitOp()
        //{
        //    //should boot a Operator
        //    //Operator boots with error
        //    FileInfo file = new FileInfo(operatorPathExec);
        //    ProcessCreationProxyImpl pcs = new ProcessCreationProxyImpl(file);
        //    OperatorSpec spec = new OperatorSpec();
        //    spec.Type = PuppetMaster.OperatorType.Count;
        //    pcs.CreateOperator(spec);

        //}
    }
}
