using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Collections;
using PuppetMasterProxy;
using ConfigTypes;
using System.Runtime.Serialization.Formatters.Binary;

namespace Operator
{
    class Program
    {
        public const string INVD_ARGS = "Invalid arguments";
        public const string ERR_CONF_FILE = "Problem's reading config file, check again";
        public const string OP_SERVICE = "op";

        private static IDictionary props = new Hashtable();

        public static void Main(string[] args)
        {
            Console.WriteLine("Operator Program started");
            if (args.Length == 3)
            {
                //string fileName = args[0];
                string myAddr = args[1];
                int repId = Int32.Parse(args[2]);
                string spec = args[0];

                //Console.WriteLine("path for config file " + fileName);
                //FileInfo file = new FileInfo(fileName);
                //if (file.Exists)
                //{

                    try
                    {
                        //Console.WriteLine("config file name "+file.FullName);
                        //OperatorSpec opSpec = ReadFromBinaryFile<OperatorSpec>(file.FullName);
                    
                        OperatorSpec opSpec = ReadFromByteArray(spec);
                        OperatorImpl op = null;
                        
                        // Console.WriteLine("Parametros do config");
                        // Console.WriteLine(opSpec.ToString());

                        switch (opSpec.Type)
                        {
                            case OperatorType.Count:
                                op = new CountOperator(opSpec, myAddr, repId);
                                Console.WriteLine("new Count Operator");
                                break;
                            case OperatorType.Custom:
                                string dll = opSpec.Args[0];
                                string class_ = opSpec.Args[1];
                                string method = opSpec.Args[2];
                                op = new CustomOperator(opSpec, dll, class_, method, myAddr, repId);
                                Console.WriteLine("new Custom Operator");
                                break;
                            case OperatorType.Dup:
                                op = new DupOperator(opSpec, myAddr, repId);
                                Console.WriteLine("new Dup Operator");
                                break;
                            case OperatorType.Filter:
                                int id = Int32.Parse(opSpec.Args[0]);
                                string cond = opSpec.Args[1];
                                string value = opSpec.Args[2];
                                op = new FilterOperator(opSpec, id, cond, value, myAddr, repId);
                                Console.WriteLine("new Filter Operator");
                                break;
                            case OperatorType.Uniq:
                                id = Int32.Parse(opSpec.Args[0]);
                                op = new UniqOperator(opSpec, id, myAddr, repId);
                                Console.WriteLine("new Uniq Operator");
                                break;

                        }
                        Uri u = new Uri(myAddr);
                        op.myPort = u.Port;
                        //FIX o illian ia meter o porto no operator sepc???
                        props["port"] = u.Port;
                        //props["timeout"] = 1000; // in milliseconds
                        TcpChannel channel = new TcpChannel(props, null, null);
                        ChannelServices.RegisterChannel(channel, false);
                        RemotingServices.Marshal(op, OP_SERVICE, typeof(OperatorImpl));

                        //Console.WriteLine("press entrer to start OP");
                        //Console.Read();
                        //op.Start();
                        //Console.WriteLine("Já fiz start");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(ERR_CONF_FILE);
                        Console.WriteLine(e.StackTrace);

                    }
                }

            //}
            /*else
            {
                Console.WriteLine(INVD_ARGS);
            }*/

            Console.ReadLine();
        }

        private static T ReadFromBinaryFile<T>(string filePath)
        {
            using (Stream stream = File.Open(filePath, FileMode.Open, FileAccess.Read))
            {
                var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                return (T)binaryFormatter.Deserialize(stream);
            }
        }

        private static OperatorSpec ReadFromByteArray(string spec)
        {
            byte[] a = Convert.FromBase64String(spec);
            MemoryStream memStream = new MemoryStream();
            BinaryFormatter binForm = new BinaryFormatter();
            memStream.Write(a, 0, a.Length);
            memStream.Seek(0, SeekOrigin.Begin);
            return (OperatorSpec)binForm.Deserialize(memStream);
        }

    }
}
