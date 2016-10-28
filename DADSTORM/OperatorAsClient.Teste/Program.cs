using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Operator;
using OperatorProxys;

namespace OperatorAsClient.Tests
{
    class Program
    {
        private static readonly List<string> tuple1 = new List<string> { "test1", "test2", "test3", "Ola", "ola" };
        static void Main(string[] args)
        {
            Console.WriteLine("waiting");
            Console.ReadLine();
            OperatorTuple tuple = new OperatorTuple(tuple1);
            OperatorImpl op = new DupOperator();
            op.SendTuple(tuple);
            Console.WriteLine("send");

            Console.ReadLine();
        }
    }
}
