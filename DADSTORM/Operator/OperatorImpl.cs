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


        // MOST IMPORTANT VARIABLE IN WHOLE PROJECT
        private const bool DEBUG = true; // important print in the code, like ACKs status and stuff
        private const bool DEBUG_GERAL = true; // general print made just to see other stuff like the tuples we send, waht tuple received

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
        static readonly int DELTA_TIME = 30 * 1000; // min * seg * millisecond
        Dictionary<string, long> allReplicas;
        List<string> outReps;
        protected bool lastOp = false;

        // Variables used in Exactly-Once-Semantics
        public Semantics Semantics { get; private set; } // for easy access
        protected int counter = 0; // used to assign IDs to newly created operators
        protected string MyId { get; private set; } // for easy access
        /// <summary>
        /// {tuple_id : {tuple, timestamp}}
        /// </summary>
        Dictionary<string, OutgoingTuple> tuplesAwaitingACK = new Dictionary<string, OutgoingTuple>();
        static readonly int MAX_WAIT_FOR_TUPLE_DELIVERY = 5 * 1000; // 5 seconds
        static readonly int ACK_WATCHDOG_INTERVAL = 2 * 1000; // 2 seconds
        /// <summary>
        /// {tuple_id : {tuple, timestamp}}
        /// </summary>
        protected Dictionary<string, List<OperatorTuple>> resultsCache = new Dictionary<string, List<OperatorTuple>>();

        /// <summary>
        /// List of urls of this replica (excluding the url of this replica itelf).
        /// </summary>
        protected List<string> myReplicasURLs = new List<string>();

        private static object tuplesAwaitingACKLock = new object(); // TODO: is this needed? (unused for now)

        public OperatorImpl(OperatorSpec spec, string myAddr, int repId)
        {
            this.Spec = spec;
            MyId = spec.Id;
            Semantics = spec.Semantics;

            outReps = new List<string>();
            if (this.Spec.OutputOperators != null)
                foreach (OperatorOutput outOp in this.Spec.OutputOperators)
                {
                    outReps.AddRange(outOp.Addresses);
                }

            InitMyReplicaURLs(MyAddr, myReplicasURLs);
            StartWatchdogThread();

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
        }

        private void StartWatchdogThread()
        {
            if (Semantics != Semantics.ExactlyOnce)
            {
                // only start the new thread for ExactlyOnceSemantics
                return;
            }
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                while (true)
                {

                    lock (tuplesAwaitingACKLock)
                    {
                        var keyList = tuplesAwaitingACK.Keys.Clone();
                        // check the ACK table, if necesary schedule a re-send
                        foreach (var key in keyList)
                        {
                            if (DEBUG_GERAL)
                            {
                                Console.WriteLine(string.Format("====== Watchdog thread starting iteration over {0} items =======", keyList.Count));
                            }
                            OutgoingTuple ot = tuplesAwaitingACK[key];
                            if (CheckTupleNeedsScheduling(ot))
                            {
                                OperatorTuple tuple = ot.Tuple;

                                if (DEBUG_GERAL)
                                { 
                                    Console.WriteLine(string.Format("Watchdog thread: tuple {0} re-scheduled for sending", tuple));
                                }

                                tuplesAwaitingACK.Remove(key); // NOTE: order important here: first remove, then add to waitingTuples queue
                                tuple.YouAreParent = true; // just to be extra sure
                                PutTupleInWaitingList(tuple);
                            }
                            else
                            {
                                // Shouldn't do anything (no-op)
                                if (DEBUG_GERAL)
                                    Console.WriteLine(string.Format("Watchdog thread: tuple {0} DOES NOT need scheduling for re-sending yet", ot.Tuple));
                            }
                            if (DEBUG_GERAL)
                            {
                                Console.WriteLine(string.Format("====== Watchdog thread ended iteration over {0} items =======", keyList.Count));
                            }
                        }
                    }

                    Thread.Sleep(ACK_WATCHDOG_INTERVAL);
                }
            }
            ).Start();
        }

        private void PutTupleInWaitingList(OperatorTuple tuple)
        {
            lock (this)
            {
                Console.WriteLine(string.Format("PutTupleInWaitingList(): Putting tuple {0} in waiting list", tuple));
                this.waitingTuples.Add(tuple);
                Monitor.PulseAll(this);
            }
        }

        /// <summary>
        /// Check whether the tuple needs to be scheduled for re-sending.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private bool CheckTupleNeedsScheduling(OutgoingTuple value)
        {
            return (this.getCurrentTime() - value.TimeSent) > MAX_WAIT_FOR_TUPLE_DELIVERY;
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
            foreach (string url in Spec.Addrs)
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

                    // add value and replace old value if exists
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
                    if (DEBUG_GERAL)
                        Console.WriteLine("directoria: " + RESOURCES_DIR + in_.Name);
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

                // if we sent all tuples, then recheck
                if ((Semantics == Semantics.AtLeastOnce) && (this.waitingTuples.Count == 0))
                {
                    if (DEBUG)
                        Console.WriteLine("ALL TUPLES DONE; WAITING");
                    if (tuplesAwaitingACK.Count > 0)
                    {
                        List<OperatorTuple> temp = new List<OperatorTuple>();
                        lock (this)
                        {
                            foreach (string url in tuplesAwaitingACK.Keys)
                                temp.Add(tuplesAwaitingACK[url].Tuple);
                        }
                        if (DEBUG)
                            Console.WriteLine("-----------------------------------------------------> MISSING TUPLES SET!!!");
                        SendTuples(temp);
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
            }
            else
            {
                TreatTupleDefault();
            }

        }


        private void TreatTupleDefault()
        {
            OperatorTuple tuple = this.waitingTuples.First();
            this.waitingTuples.RemoveAt(0);

            List<OperatorTuple> list = Operation(tuple);


            /*
            if ((Spec.Type == OperatorType.Custom) && RepId == 0 && !lastOp)
                Crash();
            */

            foreach (OperatorTuple tupleX in list)
            {
                if (tupleX != null)
                {
                    if (DEBUG_GERAL)
                    {
                        Console.WriteLine("Tuple threated");
                        Console.WriteLine("Enviar: ");
                        foreach (string a in tupleX.Tuple)
                            Console.Write(a + " | ");
                        Console.WriteLine();
                    }
                    if (!lastOp)
                    {
                        SendTuple(tupleX);
                        if (Semantics == Semantics.AtLeastOnce)
                            SendACK(tupleX);
                    }
                    else
                    {
                        if (DEBUG)
                            Console.WriteLine("lastOP");
                    }
                }
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
                SendACK(tuple);
            }


            for (int i = 0; i < list.Count; i++)
            {
                OperatorTuple tupleX = list[i]; ;
                OperatorTuple newTuple = tupleX.Clone();
                newTuple.SenderUrl = MyAddr;
                if (tupleX != null)
                {
                    if (DEBUG_GERAL)
                    {
                        /*
                        Console.WriteLine("Tuple Treated");
                        Console.WriteLine("Enviar: ");
                        foreach (string a in tupleX.Tuple)
                            Console.Write(a + " | ");
                        Console.WriteLine();
                        */
                    }
                    if (!lastOp)
                    {

                        // if I'm not the parent, don't send the tuple (replication functionality)
                        if (tupleX.YouAreParent)
                        {
                            if (DEBUG)
                            { 
                                Console.WriteLine("TreatTupleExactlyOnce(): I am the parent, so I'm sending the tuple");
                            }

                            SendTuple(newTuple);

                            if (tupleX.SenderUrl != MyAddr)
                            {
                                Console.WriteLine("TreatTupleExactlyOnce(): sendig ACK to " + tupleX.SenderUrl);
                                SendACK(tupleX);
                            } else
                            {
                                Console.WriteLine("TreatTupleExactlyOnce(): skipping ACK sending. Reason: not sending ACK to myself.");
                            }
                        }
                        else
                        {
                            // I'm not the paret, so I'm not gonna send the tuple
                            // This "else" block shouldn't do anything, it's here just to print a debug message
                            if (DEBUG)
                                Console.WriteLine("TreatTupleExactlyOnce(): I'm not the parent, so I'm not forwarding the tuple");
                        }
                    }
                    else
                    {
                        // TODO: NOTE: this will cause issues when using ExactlyOnce semantics with the last operator,
                        // the same check for parent should be done at the last one.
                        if (DEBUG)
                            Console.WriteLine("lastOP");
                    }
                }
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
            Console.WriteLine("ReceiveTuple(): called with tuple {0}", tuple);
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

            lock (this)
            {
                if (DEBUG_GERAL)
                {
                    Console.WriteLine("received tuple");
                    foreach (string s in tuple.Tuple)
                        Console.Write(s + " ");
                    Console.WriteLine();
                }

                this.waitingTuples.Add(tuple);
                Monitor.PulseAll(this);
            }

        }

        private void ReceiveTupleExactlyOnce(OperatorTuple tuple)
        {

            lock (this)
            {
                if (tuple.YouAreParent)
                {
                    Console.WriteLine("ReceiveTupleExactlyOnce(): receieved tuple: {0}", tuple);
                    // TODO: clone tuple, set YouAreParent to False and forward it to all children
                    OperatorTuple childTuple = tuple.Clone();
                    childTuple.YouAreParent = false;
                    Console.WriteLine("\tReceiveTupleExactlyOnce(): Cloned tuple...");
                    List<string> myAliveReps = GetMyAliveReps();
                    Console.WriteLine("ReceiveTupleExactlyOnce(): myAliveReps.Count = {0}", myAliveReps.Count);
                    foreach (string url in myAliveReps)
                    {
                        try
                        {
                            Console.WriteLine("ReceiveTupleExactlyOnce(): Sending tuple {0} to child at url {1}", childTuple, url);
                            IOperatorProxy opServer = (IOperatorProxy)Activator.GetObject(typeof(IOperatorProxy), url);
                            asyncServiceCall(opServer.ReceiveTuple, childTuple, url);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.StackTrace);
                        }
                    }
                } else
                {
                    if (DEBUG_GERAL)
                    {
                        Console.WriteLine("ReceiveTupleExactlyOnce(): Received tuple {0}, but I'm not parent, so I'm not sending it to children.");
                    }
                }

                if (DEBUG)
                {
                    Console.WriteLine("ReceiveTupleExactlyOnce(): Received a tuple");
                    foreach (string s in tuple.Tuple)
                    {
                        Console.Write("\t" + s + " ");
                    }
                    Console.WriteLine();
                }

                // The decision of whether the tuple should be processed or retrieved from cache
                // is done in TreatTuple() method.
                this.waitingTuples.Add(tuple);
                Monitor.PulseAll(this);

            }
        }

        private List<string> GetMyAliveReps()
        {
            Console.WriteLine("GetMyAliveReps(): Entering...");
            List<string> aliveOutReps = new List<string>();
            Console.WriteLine("GetMyAliveReps(): Getting list of my alive replicas... [total replicas = {0}]", myReplicasURLs.Count);
            foreach (string url in this.myReplicasURLs)
            {
                if (url == MyAddr)
                {
                    continue;
                }
                if (this.isAlive(url))
                {
                    Console.WriteLine("\t{0} is alive", url);
                    aliveOutReps.Add(url);
                }
                else
                {
                    
                    Console.WriteLine("\t{0} is down", url);
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
            }
            else if (Semantics == Semantics.AtLeastOnce)
            {
                SendTupleExactlyOnce(tuple);
            }
            else
            {
                SendTupleOriginal(tuple);
            }
        }

        public void SendTuples(List<OperatorTuple> tuples)
        {
            foreach (OperatorTuple tuple in tuples)
            {
                string url = this.GetOutUrl(tuple);
                if (url != null)
                    try
                    {
                        if (DEBUG_GERAL)
                            Console.WriteLine("-----------------------------------------------------> Sending: {0}", tuple);

                        IOperatorProxy opServer = (IOperatorProxy)Activator.GetObject(typeof(IOperatorProxy), url);
                        asyncServiceCall(opServer.ReceiveTuple, tuple, url);

                        if (this.Spec.LoggingLevel.Equals(LoggingLevel.Full))
                        {
                            IPuppetMasterProxy obj = (IPuppetMasterProxy)Activator.GetObject(typeof(IPuppetMasterProxy), String.Format(PM_ADDR_FMT, this.Spec.PuppetMasterUrl));

                            asyncServiceCall(obj.ReportTuple, this.Spec.Id, this.RepId, tuple, String.Format(PM_ADDR_FMT, this.Spec.PuppetMasterUrl));
                        }
                    }
                    catch (SocketException)
                    {
                        new Thread(() =>
                        {
                            Thread.CurrentThread.IsBackground = true;
                            SendTuple(tuple);
                        }).Start();
                    }
                    catch (Exception)
                    {
                        //TODO: we probably dont want to catch all but for now 
                        if (DEBUG)
                            Console.WriteLine("\t\tCan not send tuple from ALLL");
                    }
            }
        }

        private void SendTupleOriginal(OperatorTuple tuple)
        {
            string url = this.GetOutUrl(tuple);
            if (url != null)
                try
                {
                    if (DEBUG_GERAL)
                    {
                        Console.Write("Enviar: ");
                        foreach (string a in tuple.Tuple)
                            Console.Write(a + " | ");
                        Console.WriteLine();
                        Console.WriteLine("Send tuples to: " + url);
                    }

                    IOperatorProxy opServer = (IOperatorProxy)Activator.GetObject(typeof(IOperatorProxy), url);
                    asyncServiceCall(opServer.ReceiveTuple, tuple, url);

                    // send tuple to PuppetMaster
                    if (this.Spec.LoggingLevel.Equals(LoggingLevel.Full))
                    {
                        if (DEBUG_GERAL)
                            Console.WriteLine("send tuples to ppm");
                        IPuppetMasterProxy obj = (IPuppetMasterProxy)Activator.GetObject(
                                typeof(IPuppetMasterProxy),
                                String.Format(PM_ADDR_FMT, this.Spec.PuppetMasterUrl));

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
                    if (DEBUG)
                        Console.WriteLine("\t\tCan not send tuple");
                    //Console.WriteLine(e.StackTrace);
                }
        }

        private void SendTupleExactlyOnce(OperatorTuple tuple)
        {
            OperatorTuple newTuple = tuple.Clone();

            string url = this.GetOutUrl(tuple);
            if (url != null)
                try
                {
                    newTuple.SenderUrl = MyAddr; // just to be extra sure that the ACK arrives to the right place

                    if (DEBUG_GERAL)
                        Console.WriteLine("SendTupleExactlyOnce(): ===> Sending tuple with ID={0} to URL={1}", tuple.Id, url);

                    if (DEBUG)
                    {
                        Console.WriteLine("SendTupleExactlyOnce(): Sending tuple: ");
                        foreach (string a in newTuple.Tuple)
                            Console.Write("\t" + a + " | ");
                        Console.WriteLine();
                    }
                    IOperatorProxy opServer = (IOperatorProxy)Activator.GetObject(typeof(IOperatorProxy), url);

                    // send tuple to the downstream operator
                    asyncServiceCall(opServer.ReceiveTuple, newTuple, url);

                    tuplesAwaitingACK[tuple.Id] = new OutgoingTuple { Tuple = tuple, TimeSent = getCurrentTime() };
                    // send tuple to PuppetMaster
                    if (this.Spec.LoggingLevel.Equals(LoggingLevel.Full))
                    {
                        if (DEBUG_GERAL)
                            Console.WriteLine("SendTupleExactlyOnce(): Sendig tuple to PPM");
                        IPuppetMasterProxy obj = (IPuppetMasterProxy)Activator.GetObject(
                                typeof(IPuppetMasterProxy),
                                String.Format(PM_ADDR_FMT, this.Spec.PuppetMasterUrl));

                        //obj.ReportTuple(this.Spec.Id, this.RepId, tuple);
                        asyncServiceCall(obj.ReportTuple, this.Spec.Id, this.RepId, newTuple, String.Format(PM_ADDR_FMT, this.Spec.PuppetMasterUrl));
                    }

                }
                catch (SocketException)
                {
                    new Thread(() =>
                    {
                        Console.WriteLine("=========================================> BLAME IT ON PAULO <=========================================");
                        Thread.CurrentThread.IsBackground = true;
                        //TODO: se der merda a culpa e do paulo
                        SendTuple(newTuple);

                    }).Start();
                }
                catch (Exception)
                {
                    //TODO: we probably dont want to catch all but for now 
                    // what we do may depends on semantics
                    if (DEBUG_GERAL)
                        Console.WriteLine("\t\tCan not send tuple");
                    //Console.WriteLine(e.StackTrace);
                }
        }


        private void asyncServiceCall(Action<OperatorTuple> method, OperatorTuple ot, string url)
        {
            try
            {
                Console.WriteLine("asyncServiceCall(): Sending tuple with ID={0} to URL={1}", ot.Id, url);
                RemoteAsyncDelegateOperatorTuple RemoteDel = new RemoteAsyncDelegateOperatorTuple(method);
                // Call delegate to remote method
                IAsyncResult RemAr = RemoteDel.BeginInvoke(ot, null, null);
                // Wait for the end of the call and then explictly call EndInvoke
                RemAr.AsyncWaitHandle.WaitOne();
                Console.WriteLine("##asyncServiceCall() PASSED THROUGH WaitOne():## Sending tuple with ID={0} to URL={1}", ot.Id, url);
                // RemoteDel.EndInvoke(RemAr);
                Console.WriteLine("##asyncServiceCall() PASSED THROUGH EndInvoke():## Sending tuple with ID={0} to URL={1}", ot.Id, url);


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

        /*
        private void asyncServiceCall(Action method, string url)
        {
            try
            {
                RemoteAsyncDelegate RemoteDel = new RemoteAsyncDelegate(method);
                // Call delegate to remote method
                IAsyncResult RemAr = RemoteDel.BeginInvoke(null, null);
                // Wait for the end of the call and then explictly call EndInvoke
                RemAr.AsyncWaitHandle.WaitOne();
                RemoteDel.EndInvoke(RemAr);
            }
            catch (SocketException)
            {
                throw new SocketException(); // send top to retry
            }
            catch (Exception)
            {
                // TODO some weird error
            }
        }
        */


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
            if (DEBUG_GERAL)
            {
                Console.WriteLine("possivel url:");
                foreach (string s in aliveOutReps)
                {
                    Console.WriteLine("\t\t" + s);
                }
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
            if (DEBUG_GERAL)
            {
                Console.WriteLine("\tisAlive(): Checking if {0} is alive. MyAddr = {1}", url, MyAddr);
                if (url == MyAddr)
                {
                    Console.WriteLine("[!!!]isAlive(): ERROR! Trying to check if MyAddr isAlive!-------------------#<");
                }
            }
            try
            {
                long lastSeen = 0;
                lock (this)
                {
                    Console.WriteLine("isAlive(): Trying to get allReplicas[url]");
                    lastSeen = this.allReplicas[url];
                    Console.WriteLine("isAlive(): Got allReplicas[url]");
                }
                if ((this.getCurrentTime() - lastSeen) <= (NUM_FAILURES * DELTA_TIME) && this.countFails[url] < NUM_FAILURES)
                {
                    Console.WriteLine("isAlive(): All good, returning True");
                    return true;
                }
                lock (this)
                {
                    if (DEBUG_GERAL)
                        Console.WriteLine("Replica removida: {0}", url);
                    this.allReplicas.Remove(url);
                }

            }
            catch (KeyNotFoundException)
            {
                Console.WriteLine("isAlive(): KeyNotFoundExeption. Returning false...");
                return false;
            }

            Console.WriteLine("isAlive(): Returning false...");
            return false;
        }


        /* Hashing Functions */

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
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException(string.Format("Operator {0} tried to read file {1}, but it was not found", MyId, filePath));
            }

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
                        tuples.Add(new OperatorTuple(tuple, newId, MyAddr) { SenderUrl = MyAddr });
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

        public void SendACK(OperatorTuple tuple)
        {
            OperatorTuple newTuple = tuple.Clone();
            newTuple.SenderUrl = MyAddr;
            if (DEBUG)
                Console.WriteLine(string.Format("SendACK(): ACKing tuple with ID = {0}. Sending ACK to tuple at URL = {1}", tuple.Id, tuple.SenderUrl));
            IOperatorProxy opServer = (IOperatorProxy)Activator.GetObject(typeof(IOperatorProxy), tuple.SenderUrl);
            asyncServiceCall(opServer.ReceiveACK, newTuple, tuple.SenderUrl);
        }

        public void ReceiveACK(OperatorTuple tuple)
        {
            if (DEBUG)
                Console.WriteLine(string.Format("ReceiveACK(): Received ACK for tuple with ID={0} from Operator at Addr={1}", tuple.Id, tuple.SenderUrl));
            lock (tuplesAwaitingACKLock)
            {
                if (tuplesAwaitingACK.ContainsKey(tuple.Id))
                {
                    if (DEBUG)
                        Console.WriteLine(string.Format("\tFound tuple with ID={0} in tuplesAwaitingACK, removing...", tuple.Id));
                    tuplesAwaitingACK.Remove(tuple.Id);
                }
                else
                {
                    // Do nothing
                    if (DEBUG)
                        Console.WriteLine(string.Format("\tWARN: Tuple with ID={0} not found in tuplesAwaitingACK.", tuple.Id));
                }
            }
        }
    }
}
