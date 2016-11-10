using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using Operator;
using OperatorProxys;
using System.Threading;
using System.IO;

namespace Operator.Tests
{
    [TestFixture]
    public class OperatorSimpleTest : OperatorBaseTestFixture
    {
        private string operatorPathExec = BASE_DIR + @"../../../../Operator/bin/Debug/Operator.exe";
        /// <summary>
        /// simple variables to be used in the test
        /// </summary>
        private static readonly OperatorTuple tuple1 = new OperatorTuple(new List<string> { "test1", "test2", "test3", "Ola", "ola" });
        private static readonly OperatorTuple tuple2 = new OperatorTuple(new List<string> { "test4", "test5", "test6", "test6" });

        private static readonly OperatorTuple tuple3 = new OperatorTuple(new List<string> { "test1", "test2", "test3" });

        private readonly List<OperatorTuple> tuples = new List<OperatorTuple> { tuple1, tuple2 };

        /* build directory of TestDll, change as necessary */

        private static readonly string dllName = RESOURCES_DIR+"TestDll.dll";
        private static readonly string className = "TestDll.ChangeTuple";
        private static readonly string methodName = "DuplicateTuple";
        private static readonly string followersFile = RESOURCES_DIR+"followers.dat";
        private static readonly string tweetersFile = RESOURCES_DIR+"tweeters.dat";
        private static readonly string followersFile1 = RESOURCES_DIR + "followers1.dat";
        private static readonly string tweetersFile1 = RESOURCES_DIR + "tweeters1.dat";


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
            Operator.UniqOperator uo = new UniqOperator(2);
            Assert.That(Is.Equals(uo.Operation(tuple1), tuple1));

            Assert.That(Is.Equals(uo.Operation(tuple3), null));

            uo = new UniqOperator(1);
            Assert.That(Is.Equals(uo.Operation(tuple2), tuple2));
        }

        [Test]
        public void TestCountOperator()
        {
            Operator.CountOperator co = new CountOperator();
            Assert.That(Is.Equals(co.Operation(tuple1), tuple1));
            Assert.That(Is.Equals(co.countResult, 1));

            Assert.That(Is.Equals(co.Operation(tuple2), tuple2));
            Assert.That(Is.Equals(co.countResult, 2));

            Assert.That(Is.Equals(co.Operation(tuple2), tuple2));
            Assert.That(Is.Equals(co.countResult, 3));

            Assert.That(Is.Equals(co.Operation(tuple2), tuple2));
            Assert.That(Is.Equals(co.countResult, 4));
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

            Operator.CustomOperator co = new CustomOperator(null, className, methodName);
            // WARNING FOR TEST ONLY
            co.setDll(dllName);

            /* the tuple which content should be result */
            List<string> tupleCompare = new List<string> { "test1", "test2", "test3", "test1", "test2", "test3" };

            /* invoke operation */
            OperatorTuple res = co.Operation(tuple3);

            /* the size of both tuples must be equal */
            Assert.That(Is.Equals(res.Tuple.Count, tupleCompare.Count));

            /* compare each string in the tuple, must be equal */
            for (int i = 0; i < res.Tuple.Count; i++)
            {
                Assert.That(Is.Equals(res.Tuple[i], tupleCompare[i]));
            }
        }

        [Test]
        public void TestFlowOfTuples()
        {
            Operator.DupOperator dop = new DupOperator();

            dop.ReceiveTuple(tuple1);
            dop.ReceiveTuple(tuple2);
            dop.ReceiveTuple(tuple3);
            Assert.That(Is.Equals(dop.waitingTuples.Count, 3));
            //Assert.That(Is.Equals(dop.readyTuples.Count, 0));

            // Thread.Sleep(100);
            dop.Start();

            //precisamos de garantir que as thread processão os tuplos
            Thread.Sleep(2000);

            Assert.That(Is.Equals(dop.waitingTuples.Count, 0));
            //Assert.That(Is.Equals(dop.readyTuples.Count, 3));



            dop = new DupOperator();

            dop.Start();
            dop.ReceiveTuple(tuple1);
            dop.ReceiveTuple(tuple2);
            dop.ReceiveTuple(tuple3);

            //precisamos de garantir que as thread processão os tuplos
            Thread.Sleep(100);
            Assert.That(Is.Equals(dop.waitingTuples.Count, 0));
            //Assert.That(Is.Equals(dop.readyTuples.Count, 3));

        }
        [Test]
        public void TestFreezeUnfreeze()
        {
            Operator.DupOperator dop = new DupOperator();
            dop.Start();
            dop.Freeze();
            dop.ReceiveTuple(tuple1);
            dop.ReceiveTuple(tuple2);
            dop.ReceiveTuple(tuple3);
            //precisamos de garantir que as thread processão os tuplos
            Thread.Sleep(100);
            Assert.That(Is.Equals(dop.waitingTuples.Count, 3));
            //Assert.That(Is.Equals(dop.readyTuples.Count, 0));

            dop.UnFreeze();
            //precisamos de garantir que as thread processão os tuplos
            Thread.Sleep(100);
            Assert.That(Is.Equals(dop.waitingTuples.Count, 0));
            //Assert.That(Is.Equals(dop.readyTuples.Count, 3));
        }

        [Test]
        public void TestReadTuplesFile()
        {
            Operator.DupOperator dop = new DupOperator();
            FileInfo f = new FileInfo(tweetersFile1);
            Assert.That(Is.Equals(f.Exists, true));

            List<OperatorTuple> tuples = dop.ReadTuplesFromFile(f);
            Assert.That(Is.Equals(tuples.Count, 1));
            Assert.That(Is.Equals(tuples[0].Tuple[0], "1"));
            Assert.That(Is.Equals(tuples[0].Tuple[1], "user3"));
            Assert.That(Is.Equals(tuples[0].Tuple[2], "\"www.tecnico.ulisboa.pt\""));

            //---------------------------------------//
             dop = new DupOperator();
             f = new FileInfo(tweetersFile);
            Assert.That(Is.Equals(f.Exists, true));

            tuples = dop.ReadTuplesFromFile(f);
            Assert.That(Is.Equals(tuples.Count, 12));
            Assert.That(Is.Equals(tuples[11].Tuple[0], "12"));
            Assert.That(Is.Equals(tuples[11].Tuple[1], "user5"));
            Assert.That(Is.Equals(tuples[11].Tuple[2], "\"www.tecnico.ulisboa.pt\""));

            //------------------------------------------------------//
            dop = new DupOperator();
            f = new FileInfo(followersFile1);
            Assert.That(Is.Equals(f.Exists, true));

            tuples = dop.ReadTuplesFromFile(f);
            Assert.That(Is.Equals(tuples.Count, 1));
            Assert.That(Is.Equals(tuples[0].Tuple[0], "user2"));
            Assert.That(Is.Equals(tuples[0].Tuple[1], "user10"));

            //---------------------------//
            dop = new DupOperator();
            f = new FileInfo(followersFile);
            Assert.That(Is.Equals(f.Exists, true));

            tuples = dop.ReadTuplesFromFile(f);
            Assert.That(Is.Equals(tuples.Count, 13));
            Assert.That(Is.Equals(tuples[12].Tuple[0], "user11"));
            Assert.That(Is.Equals(tuples[12].Tuple[1], "user6"));
            Assert.That(Is.Equals(tuples[12].Tuple[2], "user5"));
            Assert.That(Is.Equals(tuples[12].Tuple[3], "user10"));
           
        }
    }


}
