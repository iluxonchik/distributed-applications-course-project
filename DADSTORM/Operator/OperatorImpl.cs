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
        static readonly int DELTA_TIME = 1 * 60 * 1000; // min * seg * millisecond
        Dictionary<string, long> allReplicas;
        List<string> outReps;
        bool lastOp = false;

        // Variables used in Exactly-Once-Semantics
        public Semantics Semantics { get; private set; } // for easy access
        protected int counter = 0; // used to assign IDs to newly created operators
        protected string MyId { get; private set; } // for easy access
        /// <summary>
        /// {tuple_id : {tuple, timestamp}}
        /// </summary>
        Dictionary<string, OutgoingTuple> tuplesAwaitingACK = new Dictionary<string, OutgoingTuple>();
        static readonly ulong MAX_WAIT_FOR_TUPLE_DELIVERY = 5 * 1000; // 5 seconds
        static readonly ulong ACK_WATCHDOG_INTERVAL = 2 * 1000; // 2 seconds
        
        protected Dictionary<string, List<OperatorTuple>> resultsCache = new Dictionary<string, List<OperatorTuple>>();
        /// <summary>
        /// List of urls of this replica (excluding the url of this replica itelf).
        /// </summary>
        protected List<string> myReplicasURLs = new List<string>();

        public OperatorImpl(OperatorSpec spec, string myAddr, int repId)
        {
            this.Spec = spec;

            outReps = new List<string>();
            if (this.Spec.OutputOperators != null)
                foreach (OperatorOutput outOp in this.Spec.OutputOperators)
                {
                    outReps.AddRange(outOp.Addresses);
                }

            InitMyReplicaURLs(MyAddr, myReplicasURLs);

            if (outReps.Count == 0)
            {
                lastOp = true;
            }
            InitOp();
            RepId = repId;
            MyAddr = myAddr;
            readTuplesFile();
            InitFaultCounter();
            heartBeat();
            //PrintWaitingTuples();
            Semantics = spec.Semantics;
            MyId = spec.Id;
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

        /// <summary>
        /// Initializes the list of urls of replicas of this operator.
        /// </summary>
        /// <param name="myAddr"></param>
        /// <param name="myReplicasURLs"></param>
        private void InitMyReplicaURLs(string myAddr, List<string> myReplicasURLs)
        {
            foreach(string url in Spec.Addrs)
            {
                if (url != myAddr)
                {
                    myReplicasURLs.Add(url);
                }
            }
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

        /// <summary>
        /// Gets the tuple from the waiting tuples list and treats it.
        /// </summary>
        private void TreatTuple()
        {

            if (Semantics == Semantics.ExactlyOnce)
            {
                TreatTupleExactlyOnce();
            } else
            {
                TreatTupleDefault();
            }

        }

        private void TreatTupleDefault()
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
        }

        private void TreatTupleExactlyOnce()
        {
            OperatorTuple tuple = this.waitingTuples.First();
            this.waitingTuples.RemoveAt(0);

            List<OperatorTuple> list;

            // check if result stored
            // if yes, retrieve, if no compute and store in table
            List<OperatorTuple> result = resultsCache.Get(tuple.Id);
            if (result == null)
            {
                list = Operation(tuple);
                resultsCache.Add(tuple.Id, list);
            }
            else
            {
                list = result;
            }
            
            
            foreach (OperatorTuple tupleX in list)
            {
                //Console.WriteLine("Tuple treated");

                //TODO NULL not needed??
                //if (tupleX != null)
                //{
                //Console.WriteLine("Enviar: ");
                //foreach (string a in tupleX.Tuple)
                //    Console.Write(a + " | ");
                //Console.WriteLine();
                if (!lastOp)
                   
                    // if I'm not the parent, don't send the tuple (replication functionality)
                    if (tupleX.YouAreParent)
                    {
                        Console.WriteLine("TreatTupleExactlyOnce(): I am the parent, so I'm sending the tuple");
                        SendTuple(tupleX);
                        // TODO: send ACK to previous operator
                    }
                    else
                    {
                        // I'm not the paret, so I'm not gonna send the tuple
                        // This "else" block shouldn't do anything, it's here just to print a debug message
                        Console.WriteLine("TreatTupleExactlyOnce(): I'm not the parent, so I'm not forwarding the tuple");
                    }
                else
                {
                    // TODO: NOTE: this will cause issues when using ExactlyOnce semantics with the last operator,
                    // the same check for parent should be done at the last one.
                    Console.WriteLine("lastOP");
                }
                //}
            }
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
                
            if (Semantics == Semantics.ExactlyOnce)
            {
                ReceiveTupleExactlyOnce(tuple);
            }
             else
            {
                ReceiveTupleOriginal(tuple);
            }
        }

        private void ReceiveTupleOriginal(OperatorTuple tuple)
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

        private void ReceiveTupleExactlyOnce(OperatorTuple tuple)
        {
            while (true)
            {
                lock (this)
                {
                    if (tuple.YouAreParent)
                    {
                        // TODO: clone tuple, set YouAreParent to False and forward it to all children
                        OperatorTuple childTuple = tuple.Clone();
                        childTuple.YouAreParent = false;

                        List<string> myAliveReps = GetMyAliveReps();
                        foreach(string url in myAliveReps)
                        {
                            IOperatorProxy opServer = (IOperatorProxy)Activator.GetObject(typeof(IOperatorProxy), url);
                            asyncServiceCall(opServer.ReceiveTuple, childTuple, url);
                          
                        }
                    }

                    Console.WriteLine("ReceiveTupleExactlyOnce(): Received a tuple");
                    foreach (string s in tuple.Tuple)
                    {
                        Console.Write("\t" + s + " ");
                    }
                    Console.WriteLine();
                    
                    // The decision of whether the tuple should be processed or retrieved from cache
                    // is done in TreatTuple() method.
                    this.waitingTuples.Add(tuple);
                    Monitor.PulseAll(this);
                    break;
                }
            }
        }

        private List<string> GetMyAliveReps()
        {
            List<string> aliveOutReps = new List<string>();
            Console.WriteLine(string.Format("GetMyAliveReps(): Getting list of my alive replicas... [total replicas = {}]", aliveOutReps.Count));
            foreach (string url in this.myReplicasURLs)
            { 
                if (this.isAlive(url))
                {
                    Console.WriteLine(string.Format("\t{} is alive", url));
                    aliveOutReps.Add(url);
                }
                else
                {
                    Console.WriteLine(string.Format("\t{} is down"), url);
                }
            }
            return aliveOutReps;
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

            if (Semantics == Semantics.ExactlyOnce)
            {
                SendTupleExactlyOnce(tuple);
            } else
            {
                SendTupleOriginal(tuple);
            }
        }

        private void SendTupleOriginal(OperatorTuple tuple)
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
                catch (Exception)
                {
                    //TODO: we probably dont want to catch all but for now 
                    // what we do may depends on semantics
                    Console.WriteLine("\t\tCan not send tuple");
                    //Console.WriteLine(e.StackTrace);
                }
        }

        private void SendTupleExactlyOnce(OperatorTuple tuple)
        {
            string url = this.GetOutUrl(tuple);
            if (url != null)
                try
                {
                    // TODO: init watchdog therad
                    // TODO: implement ACKs

                    tuple.SenderUrl = MyAddr; // just to be extra sure that the ACK arrives to the right place

                    Console.WriteLine("SendTupleExactlyOnce(): Sending tuple: ");
                    foreach (string a in tuple.Tuple)
                        Console.Write("\t" + a + " | ");
                    Console.WriteLine();

                    IOperatorProxy opServer = (IOperatorProxy)Activator.GetObject(typeof(IOperatorProxy), url);
                    
                    // send tuple to the downstream operator
                    asyncServiceCall(opServer.ReceiveTuple, tuple, url);
                    tuplesAwaitingACK.Add(tuple.Id, new OutgoingTuple { Tuple = tuple, TimeSent = (ulong)getCurrentTime() });

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
                catch (Exception)
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
        /// Get the URL of the downsteram operator to send the tuples to.
        /// 
        /// For the first delivery return index=0 because that is the only one availale.
        /// </summary>
        /// <returns></returns>
        private string GetOutUrl(OperatorTuple tuple)
        {
            // routing 
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

        private string GetNewId()
        {
            string id = string.Format("{0}{1}", MyId, counter);
            counter++; //update counter for next iteration
            return id;
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
                        // this is the "origin" of a tuple in the stream, so create a new Id for it. This ID
                        // will be the same for the whole stream.
                        string newId = GetNewId();
                        tuples.Add(new OperatorTuple(tuple, newId, MyAddr));
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
