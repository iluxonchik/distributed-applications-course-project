using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using System.IO;
using System.Diagnostics;
using ConfigTypes;

namespace ProcessCreation.Tests
{
    [TestFixture]
    public class ProcessCreationTests : ProcessCreationBaseTestFixture
    {


        private string operatorPathExec = BASE_DIR + @"../../../../Operator/bin/Debug/Operator.exe";

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
            spec.Type = ConfigTypes.OperatorType.Count;
            spec.Id = "OP4";
            List<OperatorInput> inputs = new List<OperatorInput>();
            OperatorInput in_ = new OperatorInput();
            in_.Type = InputType.Operator;
         
            spec.Inputs = inputs;
            Process proc = pcs.CreateOperator(spec, "tcp://localhost:9090",0);
            Assert.That(proc, Is.Not.Null);
            proc.Kill();

            }
        }
    }
