using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;


using System.Xml;
using System.Collections;

namespace ProcessCreation
{
    public class Program
    {
        private const int PORT= 10000;
        private static string operatorPathExec;
        private static string DEFAULT_OP_PATH = @"../../../../Operator/bin/Debug/Operator.exe";
        private static FileInfo operatorExecFile;
        private static IDictionary props = new Hashtable();
        public static void Main(string[] args)
        {

            if (args.Length == 1)
            {
                operatorPathExec = args[0];
            }
            else
            {
                operatorPathExec = Directory.GetCurrentDirectory() + DEFAULT_OP_PATH;
            }

             operatorExecFile = new FileInfo(operatorPathExec);

            while (!File.Exists(operatorExecFile.FullName))
            {
                Console.WriteLine("Invalid path ");
                Console.WriteLine("Please insert the full name of Operation executable file");
                operatorPathExec= Console.ReadLine();
                operatorExecFile = new FileInfo(operatorPathExec);
            }


            props["port"] = PORT;
            //props["timeout"] = 1000; // in milliseconds
            TcpChannel channel = new TcpChannel(props, null, null);
            ChannelServices.RegisterChannel(channel, false);
            ProcessCreationProxyImpl servicos = new ProcessCreationProxyImpl(operatorExecFile);
            Console.WriteLine("PCS Started");
            RemotingServices.Marshal(servicos, "ProcessCreation", typeof(ProcessCreationProxyImpl));
            Console.ReadLine();
        }


    }
}
