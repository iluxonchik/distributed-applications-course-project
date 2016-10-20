using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OperatorProxys;
using System.Diagnostics;
using System.Threading;

namespace Operator
{
    public abstract class OperatorImpl : MarshalByRefObject, OperatorProxy, ProcessingNodesProxy
    {
        //TODO:where does the routing and the and process semantics cames in??? probably in

        public string Id { get; }

        //private bool start { get; set; }
        private bool freeze { get; set; }

        private int interval { get; set; }

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
        private List<string[]> waitingTuples;
        /// <summary>
        /// list of tuples already processed and ready to be outputed
        /// </summary>
        private List<string[]> readyTuples;

        public const int DEFAULT_NUM_WORKERS= 10;

     
        public OperatorImpl()
        {
            this.num_workers = DEFAULT_NUM_WORKERS;
            this.freeze = false;
            this.waitingTuples = new List<string[]>();
            this.readyTuples = new List<string[]>();
           
        }

        private void initWorkers()
        {
            for(int i = 0; i < num_workers; i++)
            {
                ThreadStart st = new ThreadStart(this.consume);
                workers[i] = new Thread(st);
               
            }
        }

        public void consume()
        {
            while (true)
            {
                lock (this)
                {
                    while (this.waitingTuples.Count==0)
                        Monitor.Wait(this);
                    if (!this.freeze)
                    {
                        if (this.interval > 0)
                            Monitor.Wait(this.interval);
                        TreatTuples();
                        Monitor.PulseAll(this);
                    }

                }
            }
        }
        private void TreatTuples()
        {
            string[] tuple = this.waitingTuples.First();
            this.waitingTuples.RemoveAt(0);
           tuple= Operation(tuple);
            this.readyTuples.Add(tuple);
        }

        //TODO: implents the diferent types of operators
        protected abstract string[] Operation(string[] tuple);
        
        public void Start()
        {
            //this.start = true;

            for (int i = 0; i < this.num_workers; i++)
                this.workers[i].Start();
        }

        public void status()
        {
            throw new NotImplementedException();
        }

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

        public void Interval(int x_ms)
        {
            if (x_ms > 0)
                this.interval = x_ms;
        }
        public void ReceiveTuples(List<string [] >tuples)
        {
            while (true)
            {
                lock(this){
                    this.waitingTuples.AddRange(tuples);
                    Monitor.PulseAll(this);
                    break;
                }
            }
            
        }

       public void ReceiveTuple(string[] tuple)
        {
            while (true)
            {
                lock (this)
                {
                    this.waitingTuples.Add(tuple);
                    Monitor.PulseAll(this);
                    break;
                }
            }
           
        }

        //TODO: routing and semmantics shoud apear here i think
        public void SendTuples()
        {
            throw new NotImplementedException();
        }

    }
}
