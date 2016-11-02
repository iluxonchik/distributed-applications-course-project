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

        //private  string  operatorPathExec = TestContext.CurrentContext.TestDirectory + "../../../resources/Operator.exe";
        private string operatorPathExec = @"C:\Users\paulo\Documents\GitHub\distributed-applications-course-project\DADSTORM\Operator\bin\Debug\Operator.exe";
        // private string operatorPathExec = @"C:\Users\paulo\Documents\GitHub\distributed-applications-course-project\DADSTORM\ProcessCreation.Tests\resources\Operator.exe";
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
        [Test]
        public void TestInitOp()
        {
            //should boot a Operator
            //Operator boots with error
            FileInfo file = new FileInfo(operatorPathExec);

            Assert.That(Is.Equals(file.Exists, true));
            ProcessCreationProxyImpl pcs = new ProcessCreationProxyImpl(file);

            OperatorSpec spec = new OperatorSpec();
            spec.Type = PuppetMaster.OperatorType.Count;
            spec.Id = "OP1";
            pcs.CreateOperator(spec);

        }
    }
}
