using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OperatorProxys;
using System.Threading;
using System.IO;
using PuppetMasterProxy;
using ConfigTypes;
using ProcessCreationProxy;
using System.Net;
using System.Net.Sockets;

using System.Collections;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting;

namespace PuppetMaster
{

    public class PuppetMasterControler
    {
        protected readonly string logFile = "./log.txt";
        private delegate void RemoteAsyncDelegate();
        private delegate void RemoteAsyncDelegateInt(int ms);
        private ConfigParser parser;
        private CommandParser cmmParser;
        private Config sysConfig;
        private int wait;
        private string puppetMasterUrl;
        private static readonly string PCS_ADDR_FMT = @"tcp://{0}:10000/ProcessCreation";

        private static readonly string PPM_SERVICE = "PuppetMaster";
        private static readonly int PORT = 10001;
        private static IDictionary props = new Hashtable();
        private static Mutex mux = new Mutex();
        private System.Object lockThis = new System.Object();


        public PuppetMasterControler()
        {
            this.sysConfig = null;
            this.cmmParser = null;
            this.parser = null;   
          
            this.wait = 0;
            this.puppetMasterUrl = this.GetLocalIPAddress();
            props["port"] = PORT;
            TcpChannel channel = new TcpChannel(props, null, null);
            ChannelServices.RegisterChannel(channel, false);
            PuppetMasterService.controler = this;
            PuppetMasterService service = new PuppetMasterService();
            RemotingServices.Marshal(service, PPM_SERVICE, typeof(PuppetMasterService));
            //RemotingConfiguration.RegisterWellKnownServiceType(typeof(PuppetMasterService), PPM_SERVICE, WellKnownObjectMode.Singleton);


        }

        //TODO remove this constructer
        public PuppetMasterControler(Config sysconf)
        {
            this.sysConfig = sysconf;
            this.parser = null;
            
            this.wait = 0;
            this.puppetMasterUrl = this.GetLocalIPAddress();

        }
       


        private string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("Local IP Address Not Found!");
        }
        public void ParseConfig(String fileName)
        {
            this.parser = new ConfigParser(fileName);
            this.sysConfig = this.parser.Parse();
            this.cmmParser = new CommandParser(fileName, this.sysConfig);
            this.sysConfig.commands=this.cmmParser.Parse();
            this.sysConfig.SetPuppetMasterUrl(this.puppetMasterUrl);
            CreateOperators();
        }

        public void CreateOperators()
        {
            foreach (OperatorSpec os in this.sysConfig.Operators)
            {
                CreateOperator(os);
            }
        }

        public void CreateOperator(OperatorSpec os)
        {
            for (int i = 0; i < os.Addrs.Count; i++)
            {
                Console.WriteLine("Create OP");
                string addr = os.Addrs[i];
                string host = new Uri(addr).Host;
                IProcessCreationProxy pcs = (IProcessCreationProxy)Activator.GetObject(typeof(IProcessCreationProxy), String.Format(PCS_ADDR_FMT, host));
                pcs.CreateOperator(os, addr, i);
            }
        }
        public void RunAll()
        {
            if (this.sysConfig.commands != null)
            {
                while (this.sysConfig.commands.Count > 0)
                {
                    this.Run(this.sysConfig.commands.Dequeue());
                }
            }
        }
        public Command Step()
        {
            if (this.sysConfig.commands.Count == 0)
                throw new EndOfCommandsException("There are no more Commands");

            Command next = this.sysConfig.commands.Dequeue();
            //Console.WriteLine("Executing command" + next.Type.ToString());
            this.Run(next);
            return next;
        }

        /* to be used with Step button to show the next command before is executed */
        public Command getTopCommand()
        {
            if (this.sysConfig != null)
            {
                if (this.sysConfig.commands.Count != 0)
                {
                    Queue<Command> temp = new Queue<Command>(this.sysConfig.commands);
                    return temp.Dequeue();
                }
            }
            return null;
        }


        private void Run(Command command)
        {
            if (this.wait > 0)
            {
                Thread.Sleep(wait);
            }
            switch (command.Type)
            {
                case CommandType.Start:
                    this.Start(command);
                    break;
                case CommandType.Interval:
                    this.Interval(command);
                    break;
                case CommandType.Status:
                    this.Status(command);
                    break;
                case CommandType.Crash:
                    this.Crash(command);
                    break;
                case CommandType.Freeze:
                    this.Freeze(command);
                    break;
                case CommandType.Unfreeze:
                    this.UnFree(command);
                    break;
                case CommandType.Wait:
                    this.wait = command.MS;
                    break;

            }
        }

        private void UnFree(Command command)
        {
            foreach (string url in command.Operator.Addrs)
            {
                IProcessingNodesProxy op = (IProcessingNodesProxy)Activator.GetObject(typeof(IProcessingNodesProxy), url);
                this.Writelog(command.Operator.Id + " | " + command.RepId + " | " + command.Type.ToString());
                asyncServiceCall(op.UnFreeze, url);
            }
        }

        private void Freeze(Command command)
        {
            foreach (string url in command.Operator.Addrs)
            {
                IProcessingNodesProxy op = (IProcessingNodesProxy)Activator.GetObject(typeof(IProcessingNodesProxy), url);
                this.Writelog(command.Operator.Id + " | " + command.RepId + " | " + command.Type.ToString());
                asyncServiceCall(op.Freeze, url);
            }
        }

        private void Crash(Command command)
        {

            string url = command.Operator.Addrs[command.RepId];
            IProcessingNodesProxy op = (IProcessingNodesProxy)Activator.GetObject(typeof(IProcessingNodesProxy), url);

            this.Writelog(command.Operator.Id + " | " + command.RepId + " | " + command.Type.ToString());

            asyncServiceCall(op.Crash, url);
            removeRep(url);
        }

        private void Interval(Command command)
        {
            foreach (string url in command.Operator.Addrs)
            {
                IProcessingNodesProxy op = (IProcessingNodesProxy)Activator.GetObject(typeof(IProcessingNodesProxy), url);
                this.Writelog(command.Operator.Id + " | " +url+ " | " + command.Type.ToString() + " interval: " + command.MS);
                asyncServiceCall(op.Interval, command.MS, url);
            }
        }

        private void Start(Command command)
        {
            int counter = 0;
            foreach (string url in command.Operator.Addrs)
            {
                IProcessingNodesProxy op = (IProcessingNodesProxy)Activator.GetObject(typeof(IProcessingNodesProxy), url);
                this.Writelog(command.Operator.Id + " | " + counter++ + " | " + command.Type.ToString());
                asyncServiceCall(op.Start, url);
            }
        }

        private void Status(Command command)
        {
            foreach (OperatorSpec opSpec in this.sysConfig.Operators)
            {
                int counter = 0;
                foreach (string url in opSpec.Addrs)
                {
                    IProcessingNodesProxy op = (IProcessingNodesProxy)Activator.GetObject(typeof(IProcessingNodesProxy), url);
                    this.Writelog(opSpec.Id + " | " + counter++ + " | " + url + " | " + command.Type.ToString());
                    asyncServiceCall(op.Status, url);
                }
            }
        }

        private void asyncServiceCall(Action<int> method, int ms, string url)
        {
            try
            {
                RemoteAsyncDelegateInt RemoteDel = new RemoteAsyncDelegateInt(method);
                // Call delegate to remote method
                IAsyncResult RemAr = RemoteDel.BeginInvoke(ms, null, null);
                // Wait for the end of the call and then explictly call EndInvoke
                RemAr.AsyncWaitHandle.WaitOne();
                //RemoteDel.EndInvoke(RemAr); // this causes false negatives, maybe OP too slow?
            }
            catch (SocketException)
            {
                // Console.WriteLine("Could not locate server");
                this.Writelog("Unable to contact OP " + url);
                removeRep(url);
            }
            catch (Exception)
            {
                // TODO
                // What to do in this case...
            }
        }
        private void asyncServiceCall(Action method, string url)
        {
            try
            {
                RemoteAsyncDelegate RemoteDel = new RemoteAsyncDelegate(method);
                // Call delegate to remote method
                IAsyncResult RemAr = RemoteDel.BeginInvoke(null, null);
                // Wait for the end of the call and then explictly call EndInvoke
                RemAr.AsyncWaitHandle.WaitOne();
                //RemoteDel.EndInvoke(RemAr); // this causes false negatives, maybe OP too slow?
            }
            catch (SocketException)
            {
                // Console.WriteLine("Could not locate server");
                this.Writelog("Unable to contact OP " + url);
                removeRep(url);
            }
            catch (Exception)
            {
                // TODO
                // What to do in this case...
            }
        
        }

        public void Writelog(string msg)
        {
            mux.WaitOne();
                using (StreamWriter outputFile = new StreamWriter(this.logFile, true))
                {
                    outputFile.WriteLine(msg);
                }
            mux.ReleaseMutex();
            
            
        }

        public void AddCommand(Command cmm)
        {
            //TODO: do we need to check if the queue exists?
            if (this.sysConfig.commands == null)
            {
                this.sysConfig.commands = new Queue<Command>();
            }
            this.sysConfig.commands.Enqueue(cmm);
        }

        /*
         * To crash all operator when closing form
         */
        public void CrashAll()
        {
            if (this.sysConfig != null)
            {
                foreach (OperatorSpec opList in this.sysConfig.Operators)
                {
                    foreach (string url in opList.Addrs)
                    {
                        try
                        {
                            IProcessingNodesProxy op = (IProcessingNodesProxy)Activator.GetObject(typeof(IProcessingNodesProxy), url);
                            asyncServiceCall(op.Crash, url);

                        }
                        catch (Exception)
                        {
                            // FOR NOW NOTHING
                        }

                    }
                }
            }

            /* shutdown/crash PCS */
            
            
        }


        //TODO test method
        public void removeUrl(string url)
        {
            removeRep(url);
        }

        private void removeRep(string url)
        {
            lock (lockThis)
            {
                List<OperatorSpec> aux = new List<OperatorSpec>(this.sysConfig.Operators);
                bool b = false;
                foreach (OperatorSpec op in aux)
                {
                    foreach (string u in op.Addrs.ToList())
                    {
                        if (u.Equals(url))
                        {
                            //op.Addrs.Remove(url);
                            op.Addrs.RemoveAll(item => item.Equals(url));
                            List<string> temp = new List<string>(op.Addrs);
                            op.Addrs = temp;
                            b = true;
                            break;
                        }

                    }
                    if (b)
                        break;
                }
                this.sysConfig.Operators = new List<OperatorSpec>(aux);
            }

        }

    }

    public delegate void WriteLog(string msg);

    public class PuppetMasterService : MarshalByRefObject, IPuppetMasterProxy
    {
        public static PuppetMasterControler controler;
        private WriteLog del;


        void IPuppetMasterProxy.ReportTuple(string OpId, int RepId, OperatorTuple tuple)
        {
            
            string aux = OpId + " | " + RepId + " | " + tuple.ToString();
            del = new WriteLog(controler.Writelog);
            del(aux);

        }

    }
}
