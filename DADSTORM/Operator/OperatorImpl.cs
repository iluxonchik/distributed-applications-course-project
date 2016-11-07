using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OperatorProxys;
using System.Diagnostics;
using System.Threading;
using PuppetMaster;
using System.IO;
using System.Text.RegularExpressions;

namespace Operator
{

    public abstract class OperatorImpl : MarshalByRefObject, IOperatorProxy, IProcessingNodesProxy
    {
        //TODO:where does the routing and the and process semantics cames in??? probably in

        public string Id { get; }

        //private bool start { get; set; }
        private bool freeze { get; set; }

        private int interval { get; set; }

        public int myPort { get; set; }

        /// <summary>
        /// collection of thread that will preform the operations
        /// </summary>
        private Thread[] workers;

        /// <summary>
        /// number of available threads to perferm the operations
        /// </summary>
        private int num_workers;

        /// <summary>
        /// list of tupls waiting to be processed 
        /// new tuples are added to the tail
        /// to remove oldest tuple remove from the head
        /// </summary>
        public List<OperatorTuple> waitingTuples { get; private set; }

        /// <summary>
        /// list of tuples already processed and ready to be outputed
        /// </summary<>
       

        public const int DEFAULT_NUM_WORKERS = 1;
        public OperatorSpec Spec { get; private set; }


        protected readonly string BASE_DIR = Directory.GetCurrentDirectory();
        protected readonly string RESOURCES_DIR = Directory.GetCurrentDirectory() + "../../../resources/";

        public OperatorImpl(OperatorSpec spec)
        {
            this.Spec = spec;
            InitOp();
            foreach (OperatorInput in_ in this.Spec.Inputs)
            {

                if (in_.Type.Equals(InputType.File))
                {

                    this.waitingTuples.AddRange(this.ReadTuplesFromFile(new FileInfo(in_.Name)));


                }
            }



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
           
            initWorkers();

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
                            Monitor.Wait(this.interval);
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
            tuple = Operation(tuple);
            Console.WriteLine("Tratar tuple " + tuple.Tuple[0]);
            if (tuple != null)
            {
                Console.WriteLine("Enviar " + tuple.Tuple[0]);
                SendTuple(tuple);
               
            }


            /* no need to save the tuple */
        }


        /// <summary>
        /// Commands accepted
        /// </summary>
        public void Start()
        {
            //this.start = true;

            for (int i = 0; i < this.num_workers; i++)
                this.workers[i].Start();
        }

        public void Interval(int x_ms)
        {
            if (x_ms > 0)
                this.interval = x_ms;
        }

        /// <summary>
        ///   Depends of the specific operator
        /// </summary>

        public abstract void Status();
        protected void generalStatus()
        {
            Console.WriteLine(this.Spec.Type.ToString() + " | " + this.Spec.Id + " | " + myPort);
            Console.WriteLine("freeze = " + this.freeze);
            Console.WriteLine("Sending to:");
            foreach (string s in Spec.Addrs)
                Console.WriteLine(s);

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
                    foreach (string s in tuple.Tuple)
                    {
                        Console.Write(s + " ");
                    }
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

            try
            {
                //Console.WriteLine("Send tuples to " + this.GetOutUrl());
                IOperatorProxy opServer = (IOperatorProxy)Activator.GetObject(typeof(IOperatorProxy), this.GetOutUrl());
                opServer.ReceiveTuple(tuple);

                if (this.Spec.loginLevel.Equals(LoggingLevel.Full))
                {
                    //TODO: send tuple to puppetMaster
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
                //TODO: we probably dont want to catch all but for now 
                // what we do may depends on semantics
            }
        }

        /// <summary>
        /// for the frist delivery return index=0 because that is the only availale
        /// </summary>
        /// <returns></returns>
        private string GetOutUrl()
        {
            //routing 
            //FIX for final submition implement routing algoritm
            // for check point it should work 
            return this.Spec.OutputOperators[0].Addresses[0];

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
        /// <summary>
        /// Specific operator type of operation
        /// implents the diferent types of operators
        /// </summary>
        public abstract OperatorTuple Operation(OperatorTuple tuple);

        private void PrintWaitingThreads()
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
