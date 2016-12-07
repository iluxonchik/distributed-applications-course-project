using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OperatorProxys;
using System.Diagnostics;
using System.Threading;
using ConfigTypes;
using System.IO;
using System.Text.RegularExpressions;
using PuppetMasterProxy;
using System.Collections;
using System.Net.Sockets;

namespace Operator
{

    public abstract class OperatorImpl : MarshalByRefObject, IOperatorProxy, IProcessingNodesProxy
    {


        //private bool start { get; set; }
        private volatile bool freeze;

        protected int RepId { get; set; } // TODO: init from ctor
        protected string MyAddr { get; set; }

        private volatile int interval;

        public int myPort { get; set; }

        /// <summary>
        /// collection of thread that will preform the operations
        /// </summary>
        private Thread[] workers;

        /// <summary>
        /// number of available threads to perferm the operations
        /// 
        /// </summary>
        private int num_workers;

        /// <summary>
        /// list of tupls waiting to be processed 
        /// new tuples are added to the tail
        /// to remove oldest tuple remove from the head
        /// </summary>
        public List<OperatorTuple> waitingTuples { get; private set; }

        public const int DEFAULT_NUM_WORKERS = 1;
        public OperatorSpec Spec { get; private set; }
        protected readonly string BASE_DIR = Directory.GetCurrentDirectory();
        protected readonly string RESOURCES_DIR = Directory.GetCurrentDirectory() + @"\resources\";
        private static readonly string PM_ADDR_FMT = @"tcp://{0}:10001/PuppetMaster";
        private delegate void RemoteAsyncDelegate();
        private delegate void RemoteAsyncDelegateOperatorTuple(OperatorTuple ot);
        private delegate void RemoteAsyncDelegatePuppetMaster(string s, int i, OperatorTuple ot);
        public const int NUM_FAILURES = 2;
        Dictionary<string, int> countFails = new Dictionary<string, int>();
        static readonly String MULTICAST_ADDRESS = "239.0.0.222";
        static readonly int MULTICAST_END_POINT = 2222;
        static readonly int DELTA_TIME = 1 * 60 * 1000;//min * seg * millisecund
        Dictionary<string, long> allReplicas;
        List<string> outReps;
        bool lastOp = false;
        //teste commit
        public OperatorImpl(OperatorSpec spec, string myAddr, int repId)
        {
            this.Spec = spec;


            outReps = new List<string>();
            if (this.Spec.OutputOperators != null)
                foreach (OperatorOutput outOp in this.Spec.OutputOperators)
                {
                    outReps.AddRange(outOp.Addresses);
                }
            if (outReps.Count == 0)
                lastOp = true;
            InitOp();
            RepId = repId;
            MyAddr = myAddr;
            readTuplesFile();
            InitFaultCounter();
            heartBeat();
            //PrintWaitingTuples();

        }

        public OperatorImpl()
        {
            //empty because of unit tests, we did not had acess to oP spec
            InitOp();
        }

        private void InitOp()
        {
            this.num_workers = DEFAULT_NUM_WORKERS;
            this.freeze = false;
            this.waitingTuples = new List<OperatorTuple>();
            allReplicas = new Dictionary<string, long>();
            initWorkers();

        }

        private void heartBeat()
        {
            //TODO:problemas de concurrencia

            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                MulticastServer server = new MulticastServer(MULTICAST_ADDRESS, MULTICAST_END_POINT);
                Byte[] sendBytes = Encoding.ASCII.GetBytes(MyAddr);
                while (true)
                {
                   
                    if (!freeze)
                        server.sendHeartBeat(sendBytes);
                    Thread.Sleep(DELTA_TIME);

                }
            }).Start();

            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                MulticastClient client = new MulticastClient(MULTICAST_ADDRESS, MULTICAST_END_POINT);
                while (true)
                {
                    String url = client.receiveHeartBeat();

                    //add value and replace old value if exists
                    // de method add trows exception if key exists so better this way
                    lock (this)
                    {
                        allReplicas[url] = getCurrentTime();
                        countFails[url] = 0;
                    }
                }
            }).Start();
        }
        /// <returns>current time in milliecunds</returns>
        private long getCurrentTime()
        {
            return (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond);

        }

        private void InitFaultCounter()
        {
            foreach (string s in outReps)
                countFails.Add(s, 0);
        }

        private void readTuplesFile()
        {
            foreach (OperatorInput in_ in this.Spec.Inputs)
            {
                if (in_.Type.Equals(InputType.File))
                {
                    // Console.WriteLine("directoria: " + RESOURCES_DIR + in_.Name);
                    // this.waitingTuples.AddRange(this.ReadTuplesFromFile(new FileInfo(RESOURCES_DIR + in_.Name)));
                    this.waitingTuples.AddRange(this.ReadTuples(RESOURCES_DIR + in_.Name));
                }
            }
        }
        // Start all the workers at once
        private void initWorkers()
        {
            this.workers = new Thread[num_workers];

            for (int i = 0; i < num_workers; i++)
            {
                ThreadStart st = new ThreadStart(this.consume);
                workers[i] = new Thread(st);

            }
        }

        /// <summary>
        /// Controlled consume
        /// </summary>
        public void consume()
        {
            while (true)
            {
                lock (this)
                {
                    while (this.waitingTuples.Count == 0)
                        Monitor.Wait(this);

                    if (!this.freeze)
                    {
                        if (this.interval > 0)
                            Thread.Sleep(this.interval);
                        TreatTuple();
                        Monitor.PulseAll(this);
                    }
                }
            }
        }

        private void TreatTuple()
        {
            OperatorTuple tuple = this.waitingTuples.First();
            this.waitingTuples.RemoveAt(0);

            List<OperatorTuple> list = Operation(tuple);

            foreach (OperatorTuple tupleX in list)
            {
                //Console.WriteLine("Tuple threated");

                //TODO NULL not needed??
                //if (tupleX != null)
                //{
                //Console.WriteLine("Enviar: ");
                //foreach (string a in tupleX.Tuple)
                //    Console.Write(a + " | ");
                //Console.WriteLine();
                if (!lastOp)
                    SendTuple(tupleX);
                else
                {
                    Console.WriteLine("lastOP");
                }
                //}
            }

            /* no need to save the tuple */
        }
        /// <summary>
        /// Commands accepted
        /// </summary>
        public void Start()
        {
            //Console.WriteLine("Started");
            for (int i = 0; i < this.num_workers; i++)
                this.workers[i].Start();
        }

        public void Interval(int x_ms)
        {

            if (x_ms > 0)
            {

                this.interval = x_ms;
            }

        }

        /// <summary>
        ///   Depends of the specific operator
        /// </summary>
        public abstract void Status();

        protected void generalStatus()
        {
            if (this.Spec.ReplicationFactor > 1)
                Console.WriteLine("TYPE:" + this.Spec.Type.ToString() + " | ID: " + this.Spec.Id + " | REP: " + this.RepId + " | PORT:" + myPort);
            else
                Console.WriteLine("TYPE:" + this.Spec.Type.ToString() + " | ID: " + this.Spec.Id + " | PORT:" + myPort);

            Console.WriteLine("freeze = " + this.freeze);
            List<String> aux = Spec.Addrs.ToList();
            Console.WriteLine("All my OP address: ");

            foreach (string s in aux)
                Console.WriteLine("\t" + s);
            Console.WriteLine("Can send to: ");

            foreach (string s in this.getAliveOutReps())
                Console.WriteLine("\t" + s);

            Console.WriteLine("All reps:");
            lock (this)
            {
                aux = allReplicas.Keys.ToList();
            }
            foreach (string s in aux)
            {
                Console.WriteLine("\t" + s);
            }
        }

        /// <summary>
        /// Debugging commands
        /// </summary>
        public void Crash()
        {
            System.Environment.Exit(1);
        }

        public void Freeze()
        {
            this.freeze = true;
        }

        public void UnFreeze()
        {
            this.freeze = false;
        }

        /// <summary>
        /// Tuple manipulation commands
        /// </summary>
        public void ReceiveTuple(OperatorTuple tuple)
        {
            while (true)
            {
                lock (this)
                {
                    Console.WriteLine("receive tuple");
                    foreach (string s in tuple.Tuple)
                        Console.Write(s + " ");
                    Console.WriteLine();

                    this.waitingTuples.Add(tuple);
                    Monitor.PulseAll(this);
                    break;
                }
            }

        }

        public void ReceiveTuples(List<OperatorTuple> tuples)
        {
            while (true)
            {
                lock (this)
                {
                    this.waitingTuples.AddRange(tuples);
                    Monitor.PulseAll(this);
                    break;
                }
            }

        }

        //TODO: routing and semmantics shoud apear here i think
        //routing for check point is primary and We only have one replica for each operator
        public void SendTuple(OperatorTuple tuple)
        {
            string url = this.GetOutUrl(tuple);
            if (url != null)
                try
                {
                    Console.WriteLine("Enviar: ");
                    foreach (string a in tuple.Tuple)
                        Console.Write(a + " | ");
                    Console.WriteLine();
                    //Console.WriteLine("Send tuples to: " + url);
                    IOperatorProxy opServer = (IOperatorProxy)Activator.GetObject(typeof(IOperatorProxy), url);
                    //opServer.ReceiveTuple(tuple);
                    asyncServiceCall(opServer.ReceiveTuple, tuple, url);

                    // send tuple to PuppetMaster
                    if (this.Spec.LoggingLevel.Equals(LoggingLevel.Full))
                    {
                        //Console.WriteLine("send tuples to ppm");
                        IPuppetMasterProxy obj = (IPuppetMasterProxy)Activator.GetObject(
                            typeof(IPuppetMasterProxy),
                            String.Format(PM_ADDR_FMT, this.Spec.PuppetMasterUrl));

                        //obj.ReportTuple(this.Spec.Id, this.RepId, tuple);
                        asyncServiceCall(obj.ReportTuple, this.Spec.Id, this.RepId, tuple, String.Format(PM_ADDR_FMT, this.Spec.PuppetMasterUrl));


                    }

                }
                catch (SocketException)
                {
                    new Thread(() =>
                    {
                        Thread.CurrentThread.IsBackground = true;
                    //TODO: se der merda a culpa e do paulo
                    SendTuple(tuple);

                    }).Start();
                }
                catch (Exception e)
                {
                    //TODO: we probably dont want to catch all but for now 
                    // what we do may depends on semantics
                    Console.WriteLine("\t\tCan not send tuple");
                    //Console.WriteLine(e.StackTrace);
                }
        }

        private void asyncServiceCall(Action<OperatorTuple> method, OperatorTuple ot, string url)
        {
            try
            {
                RemoteAsyncDelegateOperatorTuple RemoteDel = new RemoteAsyncDelegateOperatorTuple(method);
                // Call delegate to remote method
                IAsyncResult RemAr = RemoteDel.BeginInvoke(ot, null, null);
                // Wait for the end of the call and then explictly call EndInvoke
                RemAr.AsyncWaitHandle.WaitOne();
                RemoteDel.EndInvoke(RemAr);
            }
            catch (SocketException)
            {
                lock (this)
                {
                    this.countFails[url]++;
                }
                throw new SocketException(); // send top to retry
            }
            catch (Exception)
            {
                // TODO some weird error
            }
        }

        private void asyncServiceCall(Action<string, int, OperatorTuple> method, string s, int i, OperatorTuple ot, string url)
        {
            try
            {
                RemoteAsyncDelegatePuppetMaster RemoteDel = new RemoteAsyncDelegatePuppetMaster(method);
                // Call delegate to remote method
                IAsyncResult RemAr = RemoteDel.BeginInvoke(s, i, ot, null, null);
                // Wait for the end of the call and then explictly call EndInvoke
                RemAr.AsyncWaitHandle.WaitOne();
                RemoteDel.EndInvoke(RemAr);
            }
            catch (SocketException)
            {
                lock (this)
                {
                    this.countFails[url]++;
                }
                throw new SocketException(); // send top to retry
            }
            catch (Exception)
            {
                // TODO some weird error
            }
        }

        //private void asyncServiceCall(Action method, string url)
        //{
        //    try
        //    {
        //        RemoteAsyncDelegate RemoteDel = new RemoteAsyncDelegate(method);
        //        // Call delegate to remote method
        //        IAsyncResult RemAr = RemoteDel.BeginInvoke(null, null);
        //        // Wait for the end of the call and then explictly call EndInvoke
        //        RemAr.AsyncWaitHandle.WaitOne();
        //        RemoteDel.EndInvoke(RemAr);
        //    }
        //    catch (SocketException)
        //    {
        //        throw new SocketException(); // send top to retry
        //    }
        //    catch (Exception)
        //    {
        //        // TODO some weird error
        //    }
        //}

        public void isToRemove(string url)
        {
            lock (this)
            {
                /* then is to remove */
                if (this.countFails[url] >= NUM_FAILURES)
                {
                    Console.WriteLine("Replica removida: {0}", url);
                    this.allReplicas.Remove(url);
                }
                else /* just save the new value */
                {
                    this.countFails[url]++;
                }
            }
        }

        //public void removeUrl(string url)
        //{
        //    removeRep(url);
        //}

        //private void removeRep(string url)
        //{

        //    //TODO: este metodo nao deve ser preciso
        //    List<OperatorOutput> aux = new List<OperatorOutput>(this.Spec.OutputOperators);
        //    bool b = false;
        //    foreach (OperatorOutput op in aux)
        //    {
        //        foreach (string u in op.Addresses)
        //        {
        //            if (u.Equals(url))
        //            {
        //                op.Addresses.Remove(url);
        //                b = true;
        //                Console.WriteLine("Just removed: {0}", url);
        //                break;
        //            }

        //        }
        //        if (b)
        //            break;
        //    }
        //    this.Spec.OutputOperators = aux;
        //}

        /// <summary>
        /// for the first delivery return index=0 because that is the only availale
        /// </summary>
        /// <returns></returns>


        private string GetOutUrl(OperatorTuple tuple)
        {
            //routing 
            // FIX check null of url on call method

            List<string> aliveOutReps = getAliveOutReps();
            Console.WriteLine("possivel url:");
            foreach (string s in aliveOutReps)
            {
                Console.WriteLine("\t\t" + s);
            }
           
            if (aliveOutReps.Count <= 0)
                return null;


            string url = null;

            switch (this.Spec.Routing.Type)
            {
                case RoutingType.Primary:
                    url = aliveOutReps[0];
                    break;
                case RoutingType.Random:
                    int idxR = new Random().Next(0, aliveOutReps.Count);
                    url = aliveOutReps[idxR];
                    break;
                case RoutingType.Hashing:
                    int max_arg = this.Spec.Routing.Arg - 1;
                    if (this.Spec.Routing.Arg > tuple.Tuple.Count)
                        max_arg = tuple.Tuple.Count - 1;
                    int idxH = (int)CalculateHash(tuple.Tuple[max_arg], aliveOutReps.Count);
                    url = aliveOutReps[idxH];
                    // some hard cheat is going to happen here....
                    break;
            }

            return url;
        }

        private bool isAlive(string url)
        {
            try
            {
                long lastSeen = 0;
                lock (this)
                {
                    lastSeen = this.allReplicas[url];
                }
                if ((this.getCurrentTime() - lastSeen) <= (NUM_FAILURES * DELTA_TIME) && this.countFails[url] < NUM_FAILURES)
                {
                    return true;
                }
                lock (this)
                {
                    Console.WriteLine("Replica removida: {0}", url);
                    this.allReplicas.Remove(url);
                }
                
            }
            catch (KeyNotFoundException)
            {
                return false;
            }
            return false;
        }
        /* Hashing Functions */
        //remove

        //private int SimpleHash(string key, int length)
        //{
        //    int res = 0;
        //    res = Math.Abs(key.GetHashCode() % length);
        //    return res;
        //}

        private uint CalculateHash(string key, int d)
        {
            uint hashCode = 0;
            int len = key.Length;

            for (int i = 0; i < len; i++)
            {
                hashCode ^= (hashCode << 5) + (hashCode >> 2) + key[i];
            }
            hashCode = (hashCode % (uint)d);

            return hashCode;
        }

        /* for test purposes */
        public uint CalculateHashPublic(string key, int d)
        {
            /* we can specify which hash function to use
             * for now use this
             */
            return CalculateHash(key, d);
        }

        private List<String> getAliveOutReps()
        {
            List<string> aliveOutReps = new List<string>();
            foreach (string s in this.outReps)
            {
                if (this.isAlive(s))
                    aliveOutReps.Add(s);

            }
            return aliveOutReps;
        }

        public List<OperatorTuple> ReadTuplesFromFile(FileInfo filePath)
        {
            List<OperatorTuple> tuples = new List<OperatorTuple>();
            System.IO.StreamReader file = new System.IO.StreamReader(filePath.FullName);

            //http://stackoverflow.com/questions/25471521/split-string-by-commas-ignoring-any-punctuation-marks-including-in-quotati
            // it does not remove the quote marks but if all url's have quote marks then it does not matter

            foreach (var line in File.ReadAllLines(filePath.FullName).Skip(2))
            {
                string[] aux = Regex.Split(line, @", (?=(?:""[^""]*?(?: [^""]*)*))|, (?=[^"",]+(?:,|$))");
                List<string> tuple = new List<string>(aux);

                tuples.Add(new OperatorTuple(tuple));
            }
            return tuples;
        }

        public List<OperatorTuple> ReadTuples(string filePath)
        {
            List<OperatorTuple> tuples = new List<OperatorTuple>();
            
            using (FileStream fileStream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read))
            {
                using (StreamReader streamReader = new StreamReader(fileStream))
                {
                    streamReader.ReadLine(); // skip first two
                    streamReader.ReadLine(); // skip first two
                    var line = streamReader.ReadLine();
                    while (line != null)
                    {
                        string[] aux = Regex.Split(line, @", (?=(?:""[^""]*?(?: [^""]*)*))|, (?=[^"",]+(?:,|$))");
                        List<string> tuple = new List<string>(aux);
                        tuples.Add(new OperatorTuple(tuple));
                        line = streamReader.ReadLine();
                    }
                }
            }
            return tuples;
        }

        /// <summary>
        /// Specific operator type of operation
        /// implents the diferent types of operators
        /// </summary>
        public abstract List<OperatorTuple> Operation(OperatorTuple tuple);

        private void PrintWaitingTuples()
        {
            Console.WriteLine(this.waitingTuples.ToString());
            foreach (OperatorTuple tuple in this.waitingTuples)
            {

                List<string> t = tuple.Tuple;
                foreach (string s in t)
                {
                    Console.Write(s + " ");
                }
                Console.WriteLine();
            }
        }

    }
}
