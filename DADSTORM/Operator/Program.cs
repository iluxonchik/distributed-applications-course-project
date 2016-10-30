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

namespace Operator
{
    class Program
    {
        public const string INVD_ARGS= "Invalid arguments";
        public const string ERR_CONF_FILE = "Problem's reading config file, check again";

        
        public static void Main(string[] args)
        {
            Console.WriteLine("Operator Program started");
            Console.ReadLine();
            if (args.Length == 1)
            {
                string fileName = args[0];
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
                                op = new CustomOperator(opSpec,dll, class_,method);
                                Console.WriteLine("new Custom Operator");
                                break;
                            case OperatorType.Dup:
                                op = new DupOperator(opSpec);
                                Console.WriteLine("new Dup Operator");
                                break;
                            case OperatorType.Filer:
                                int id = Int32.Parse(opSpec.Args[0]);
                                string cond = opSpec.Args[1];
                                string value = opSpec.Args[2];
                                op = new FilterOperator(opSpec,id, cond,value);
                                Console.WriteLine("new Filter Operator");
                                break;
                            case OperatorType.Uniq:
                                 id = Int32.Parse(opSpec.Args[0]);
                                op = new UniqOperator(opSpec,id);
                                Console.WriteLine("new Uniq Operator");
                                break;

                        }
                        //int port= opSepc.Port;
                        //TcpChannel channel = new TcpChannel(port);
                        //ChannelServices.RegisterChannel(channel, false);
                        //RemotingServices.Marshal(op, "OperatorService", typeof(OperatorImpl));
                    }
                    catch (Exception)
                    {
                        Console.WriteLine(ERR_CONF_FILE);
                       
                    }  
                }

            }else
            {
                Console.WriteLine(INVD_ARGS);
            }
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
