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



        public PuppetMasterControler()
        {
            this.parser = null;
            this.sysConfig = null;
            this.cmmParser = null;
            this.wait = 0;
            this.puppetMasterUrl = this.GetLocalIPAddress();

        }

        //TODO remove this constructer
        public PuppetMasterControler(Config sysconf)
        {
            this.parser = null;
            this.sysConfig = sysconf;
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
            // TODO: check if works
            for (int i = 0; i < os.Addrs.Count; i++)
            {
                Console.WriteLine("CReate OP");
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
            Console.WriteLine("Executing command" + next.Type.ToString());
            this.Run(next);
            return next;
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
                asyncServiceCall(op.UnFreeze, url);
                this.Writelog(command.Operator.Id + " | " + command.RepId + " | " + command.Type.ToString());
            }
        }

        private void Freeze(Command command)
        {
            foreach (string url in command.Operator.Addrs)
            {
                IProcessingNodesProxy op = (IProcessingNodesProxy)Activator.GetObject(typeof(IProcessingNodesProxy), url);
                asyncServiceCall(op.Freeze, url);
                this.Writelog(command.Operator.Id + " | " + command.RepId + " | " + command.Type.ToString());
            }
        }

        private void Crash(Command command)
        {

            string url = command.Operator.Addrs[command.RepId];
            IProcessingNodesProxy op = (IProcessingNodesProxy)Activator.GetObject(typeof(IProcessingNodesProxy), url);
            asyncServiceCall(op.Crash, url);
            removeRep(url);

            this.Writelog(command.Operator.Id + " | " + command.RepId + " | " + command.Type.ToString());
        }

        private void Interval(Command command)
        {
            foreach (string url in command.Operator.Addrs)
            {
                IProcessingNodesProxy op = (IProcessingNodesProxy)Activator.GetObject(typeof(IProcessingNodesProxy), url);
                asyncServiceCall(op.Interval, command.MS, url);
                this.Writelog(command.Operator.Id + " | " + command.RepId + " | " + command.Type.ToString() + " interval: " + command.MS);
            }
        }

        private void Start(Command command)
        {
            int counter =0;
            foreach (string url in command.Operator.Addrs)
            {
                IProcessingNodesProxy op = (IProcessingNodesProxy)Activator.GetObject(typeof(IProcessingNodesProxy), url);
                asyncServiceCall(op.Start, url);
                this.Writelog(command.Operator.Id + " | " + counter++ + " | " + command.Type.ToString());
            }
        }

        private void Status(Command command)
        {
            foreach (OperatorSpec opSpec in this.sysConfig.Operators)
            {
                foreach (string url in opSpec.Addrs)
                {
                    IProcessingNodesProxy op = (IProcessingNodesProxy)Activator.GetObject(typeof(IProcessingNodesProxy), url);
                    asyncServiceCall(op.Status, url);
                    //TODO:
                    //this.Writelog(opSpec.Id + " | " + opSpec.RepId + " | " + command.Type.ToString());
                    this.Writelog(opSpec.Id + " | " + url + " | " + command.Type.ToString());
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
            }
            catch (Exception)
            {
                //TODO put this in a constante
                this.Writelog("Unable to contact Process (Read Next Line to know which one)");
                removeRep(url);
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
            }
            catch (Exception)
            {
                //TODO
                this.Writelog("Unable to contact Process (Read Next Line to know which one)");
                removeRep(url);

            }
        }

        public void Writelog(string msg)
        {
            using (StreamWriter outputFile = new StreamWriter(this.logFile, true))
            {
                outputFile.WriteLine(msg);
            }
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
        }
        //TODO test method
        public void removeUrl(string url)
        {
            removeRep(url);
        }

        private void removeRep(string url)
        {
            List<OperatorSpec> aux = new List<OperatorSpec>(this.sysConfig.Operators);
            bool b = false;
            foreach (OperatorSpec op in aux)
            {
                foreach (string u in op.Addrs)
                {
                    if (u.Equals(url))
                    {
                        op.Addrs.Remove(url);
                        b = true;
                        break;
                    }

                }
                if (b)
                    break;
            }
            this.sysConfig.Operators = aux;


        }

    }

    public delegate void WriteLog(string msg);

    public class PuppetMasterService : MarshalByRefObject, IPuppetMasterProxy
    {
        public static PuppetMasterControler controler;
        protected readonly string logFile = "./log.txt";
        private WriteLog del;


        void IPuppetMasterProxy.ReportTuple(string OpId, int RepId, OperatorTuple tuple)
        {
            string aux = OpId + " | " + RepId + " | " + tuple.ToString();
            del = new WriteLog(controler.Writelog);
            del(aux);

        }

    }
}
