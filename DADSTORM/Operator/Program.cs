using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using PuppetMaster;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Collections;

namespace Operator
{
    class Program
    {
        public const string INVD_ARGS = "Invalid arguments";
        public const string ERR_CONF_FILE = "Problem's reading config file, check again";

        private static IDictionary props = new Hashtable();

        public static void Main(string[] args)
        {
            Console.WriteLine("Operator Program started");
            Console.WriteLine(args.Length);
            //Console.ReadLine();
            //System.IO.StreamWriter f = new System.IO.StreamWriter(@"C:\Users\paulo\Desktop\teste.txt");
            //f.WriteLine(args[0]);

            //f.Close();
            if (args.Length == 1)
            {
                string fileName = args[0];

                Console.WriteLine("path for config file " + fileName);
                //Console.ReadLine();
                FileInfo file = new FileInfo(fileName);
                if (file.Exists)
                {

                    try
                    {
                        OperatorSpec opSpec = ReadFromBinaryFile<OperatorSpec>(file.FullName);
                        OperatorImpl op = null;
                        switch (opSpec.Type)
                        {
                            case OperatorType.Count:
                                op = new CountOperator(opSpec);
                                Console.WriteLine("new Count Operator");
                                break;
                            case OperatorType.Custom:
                                string dll = opSpec.Args[0];
                                string class_ = opSpec.Args[1];
                                string method = opSpec.Args[2];
                                op = new CustomOperator(opSpec, dll, class_, method);
                                Console.WriteLine("new Custom Operator");
                                break;
                            case OperatorType.Dup:
                                op = new DupOperator(opSpec);
                                Console.WriteLine("new Dup Operator");
                                break;
                            case OperatorType.Filter:
                                int id = Int32.Parse(opSpec.Args[0]);
                                string cond = opSpec.Args[1];
                                string value = opSpec.Args[2];
                                op = new FilterOperator(opSpec, id, cond, value);
                                Console.WriteLine("new Filter Operator");
                                break;
                            case OperatorType.Uniq:
                                id = Int32.Parse(opSpec.Args[0]);
                                op = new UniqOperator(opSpec, id);
                                Console.WriteLine("new Uniq Operator");
                                break;

                        }

                        Uri u = new Uri(opSpec.Addrs[0]);
                        op.myPort = u.Port;

                        props["port"] = u.Port;
                        props["timeout"] = 1000; // in milliseconds
                        TcpChannel channel = new TcpChannel(props, null, null);
                        ChannelServices.RegisterChannel(channel, false);
                        RemotingServices.Marshal(op, "OperatorService", typeof(OperatorImpl));

                    }
                    catch (Exception)
                    {
                        Console.WriteLine(ERR_CONF_FILE);

                    }
                }

            }
            else
            {
                Console.WriteLine(INVD_ARGS);
            }

            Console.ReadLine();
        }
        private static T ReadFromBinaryFile<T>(string filePath)
        {
            using (Stream stream = File.Open(filePath, FileMode.Open))
            {
                var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                return (T)binaryFormatter.Deserialize(stream);
            }
        }
    }
}
