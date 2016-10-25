using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using Operator;

namespace Operator.Tests
{
    [TestFixture]
    public class OperatorSimpleTest
    {

        /// <summary>
        /// simple variables to be used in the test
        /// </summary>
        private static readonly List<string> tuple1 = new List<string> { "test1", "test2", "test3", "Ola", "ola" };
        private static readonly List<string> tuple2 = new List<string> { "test4", "test5", "test6", "test6" };
        private static readonly List<string> tuple3 = new List<string> { "test1", "test2", "test3" };

        private readonly List<List<string>> tuples = new List<List<string>> { tuple1, tuple2 };

        /* build directory of TestDll, change as necessary */

        private static readonly string dllName = TestContext.CurrentContext.TestDirectory + "../../../resources/TestDll.dll";
        private static readonly string className = "TestDll.ChangeTuple";
        private static readonly string methodName = "DuplicateTuple";

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
        public void TestUniqueOperator()
        {
            Operator.UniqOperator uo = new UniqOperator(3);
            Assert.That(Is.Equals(uo.Operation(tuple1), tuple1));

            Assert.That(Is.Equals(uo.Operation(tuple2), null));

            uo = new UniqOperator(1);
            Assert.That(Is.Equals(uo.Operation(tuple2), tuple2));
        }

        [Test]
        public void TestCountOperator()
        {
            Operator.CountOperator co = new CountOperator();
            Assert.That(Is.Equals(co.Operation(tuple1), null));
            Assert.That(Is.Equals(co.countResult, 5));

            Assert.That(Is.Equals(co.Operation(tuple2), null));
            Assert.That(Is.Equals(co.countResult, 9));

            Assert.That(Is.Equals(co.Operation(tuple2), null));
            Assert.That(Is.Equals(co.countResult, 13));

            Assert.That(Is.Equals(co.Operation(tuple2), null));
            Assert.That(!Is.Equals(co.countResult, 13));
        }

        [Test]
        public void TestDupOperator()
        {
            Operator.DupOperator dop = new DupOperator();
            Assert.That(Is.Equals(dop.Operation(tuple1), tuple1));

            Assert.That(Is.Equals(dop.Operation(tuple2), tuple2));
        }

        [Test]
        public void TestFilterOperator()
        {
            Operator.FilterOperator fo = new FilterOperator(0, "<", "text2");
            Assert.That(Is.Equals(fo.Operation(tuple1), tuple1));

            fo = new FilterOperator(3, "=", "Ola");
            Assert.That(Is.Equals(fo.Operation(tuple1), tuple1));

            fo = new FilterOperator(3, ">", "ola");
            Assert.That(Is.Equals(fo.Operation(tuple1), tuple1));

            fo = new FilterOperator(1, "=", "test2");
            Assert.That(Is.Equals(fo.Operation(tuple2), null));
        }

        [Test]
        public void TestCustomOperator()
        {
            
            Operator.CustomOperator co = new CustomOperator(dllName, className, methodName);

            /* the tuple which content should be result */
            List<string> tupleCompare = new List<string> { "test1", "test2", "test3", "test1", "test2", "test3" };

            /* invoke operation */
            List<string> res = co.Operation(tuple3);

            /* the size of both tuples must be equal */
            Assert.That(Is.Equals(res.Count, tupleCompare.Count));

            /* compare each string in the tuple, must be equal */
            for (int i = 0; i < res.Count; i++)
            {
                Assert.That(Is.Equals(res[i], tupleCompare[i]));
            }
        }
    }

}
