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

namespace PuppetMaster
{

    public class PuppetMasterControler
    {
        protected readonly string logFile = "./log.txt";
        private delegate void RemoteAsyncDelegate();
        private delegate void RemoteAsyncDelegateInt(int ms);
        private ConfigParser parser;
        private Config sysConfig;
        private int wait;
        private static readonly string PCS_ADDR_FMT = @"tcp://{0}:10000/ProcessCreation";



        public PuppetMasterControler()
        {
            this.parser = null;
            this.sysConfig = null;
            this.wait = 0;

        }

        public void ParseConfig(String fileName)
        {
            this.parser = new ConfigParser(fileName);
            this.sysConfig = this.parser.Parse();
        }

        public void CreateOperators()
        {
            foreach (OperatorSpec os in this.sysConfig.Operators)
            {
                CreateOperators(os);


            }
        }

        public void CreateOperators(OperatorSpec os)
        {
            // TODO: check if works
            for (int i = 0; i < os.Addrs.Count; i++)
            {
                var addr = os.Addrs[i];
                string host = new Uri(addr).Host;
                IProcessCreationProxy pcs = (IProcessCreationProxy)Activator.GetObject(typeof(IProcessCreationProxy), String.Format(PCS_ADDR_FMT, host));
                pcs.CreateOperator(os, addr, i);

            }
        }



        public void RunAll()
        {
            while (this.sysConfig.commands.Count > 0)
            {
                this.Run(this.sysConfig.commands.Dequeue());
            }
        }
        public Command Step()
        {
            Command next = this.sysConfig.commands.Dequeue();
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
                    this.wait = command.wait;
                    break;

            }
        }

        private void UnFree(Command command)
        {
            foreach (string url in command.Operator.Addrs)
            {
                IProcessingNodesProxy op = (IProcessingNodesProxy)Activator.GetObject(typeof(IProcessingNodesProxy), url);
                asyncServiceCall(op.UnFreeze);
                this.Writelog(command.Operator.Id + " | " + command.RepId + " | " + command.Type.ToString());
            }
        }

        private void Freeze(Command command)
        {
            IProcessingNodesProxy op = this.CallOpService(command);
            asyncServiceCall(op.Freeze);
            this.Writelog(command.Operator.Id + " | " + command.RepId + " | " + command.Type.ToString());

        }

        private void Crash(Command command)
        {
            foreach (string url in command.Operator.Addrs)
            {
                IProcessingNodesProxy op = (IProcessingNodesProxy)Activator.GetObject(typeof(IProcessingNodesProxy), url);
                asyncServiceCall(op.Crash);
                this.Writelog(command.Operator.Id + " | " + command.RepId + " | " + command.Type.ToString());
            }
        }

        private void Interval(Command command)
        {
            foreach (string url in command.Operator.Addrs)
            {
                IProcessingNodesProxy op = (IProcessingNodesProxy)Activator.GetObject(typeof(IProcessingNodesProxy), url);
                op.Interval(command.Op_ms);
                this.Writelog(command.Operator.Id + " | " + command.RepId + " | " + command.Type.ToString() + " interval: " + command.Op_ms);
            }
        }

        private void Start(Command command)
        {
            foreach (string url in command.Operator.Addrs)
            {
                IProcessingNodesProxy op = (IProcessingNodesProxy)Activator.GetObject(typeof(IProcessingNodesProxy), url);
                asyncServiceCall(op.Start);
                this.Writelog(command.Operator.Id + " | " + command.RepId + " | " + command.Type.ToString());
            }
        }

        private void Status(Command command)
        {
            foreach (string url in command.Operator.Addrs)
            {
                IProcessingNodesProxy op = (IProcessingNodesProxy)Activator.GetObject(typeof(IProcessingNodesProxy), url);
                op.Status();
                this.Writelog(command.Operator.Id + " | " + command.RepId + " | " + command.Type.ToString());
            }
        }

        private void asyncServiceCall(Action<int> method, int ms)
        {

            RemoteAsyncDelegateInt RemoteDel = new RemoteAsyncDelegateInt(method);
            // Call delegate to remote method
            IAsyncResult RemAr = RemoteDel.BeginInvoke(ms, null, null);
            // Wait for the end of the call and then explictly call EndInvoke
            RemAr.AsyncWaitHandle.WaitOne();
        }
        private void asyncServiceCall(Action method)
        {
            RemoteAsyncDelegate RemoteDel = new RemoteAsyncDelegate(method);
            // Call delegate to remote method
            IAsyncResult RemAr = RemoteDel.BeginInvoke(null, null);
            // Wait for the end of the call and then explictly call EndInvoke
            RemAr.AsyncWaitHandle.WaitOne();
        }

        private IProcessingNodesProxy CallOpService(Command command)
        {
            //TODO: what happens if Operator is dead?
            string url = command.Operator.Addrs[command.RepId];
            return (IProcessingNodesProxy)Activator.GetObject(typeof(IProcessingNodesProxy), url);

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
