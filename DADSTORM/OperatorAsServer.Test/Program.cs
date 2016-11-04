using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Operator;
using OperatorProxys; 
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Collections;

namespace OperatorAsServer.Tests
{
    class Program
    {


        static void Main(string[] args)
        {
            Console.WriteLine("service available");
            int port = 9000;


            TcpChannel channel = new TcpChannel(port);
            ChannelServices.RegisterChannel(channel, false);
            OperatorImpl op = new CountOperator();
            RemotingServices.Marshal(op, "op", typeof(OperatorImpl));

            Console.ReadLine();

        }
    }
}