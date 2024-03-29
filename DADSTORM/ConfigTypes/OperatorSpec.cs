﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace ConfigTypes
{
    /// <summary>
    /// Represents a specification of an Operator. Contains the id of the operator,
    /// the inputs of the operator,
    /// the type of the operator, replication factor, routing type, address list 
    /// and the of arguments for that operator (if appiles).
    /// </summary>
    [Serializable]
    public class OperatorSpec
    {
        public class OperatorSpecComparer : IComparer<OperatorSpec>
        {
            public int Compare(OperatorSpec x, OperatorSpec y)
            {
                // This is here to simplify testing. Useful when comparing expected and actual OperatorSpecs
                return JsonConvert.SerializeObject(x).CompareTo(JsonConvert.SerializeObject(y));
            }
        }

        public string Id { get; set; }
        public List<OperatorInput> Inputs;
        public OperatorType Type { get; set; }
        public int ReplicationFactor { get; set; }
        public OperatorRouting Routing;
        /// <summary>
        /// Operator's replica address list
        /// </summary>
        public List<string> Addrs = new List<string>();
        public List<string> Args { get; set; } = new List<string>();
        public List<OperatorOutput> OutputOperators { get; set; } = new List<OperatorOutput>(); // lista de addrs de output de cada operador

        public LoggingLevel LoggingLevel { get; set; } // pass inside

        public Semantics Semantics { get; set; } // pass inside

        public string PuppetMasterUrl { get; set; } 

        /*
         * incremented every time a new replica int the same OP is created
         */


        public OperatorSpec()
        {
            // empty on purpose
        }

        public void AddArg(string arg)
        {
            Args.Add(arg);
        }

        public override string ToString()
        {
            string inputs = "";
            foreach (OperatorInput oi in Inputs)
            {
                inputs += oi.ToString() + ";";
            }
            return String.Format("ID: {0}, Type: {1}, Inputs: [{2}]", Id, Type, inputs);
        }

        public override bool Equals(object obj)
        {
            if (obj is OperatorSpec) {
            
                OperatorSpec aux = (OperatorSpec)obj;
                return aux.Id.Equals(this.Id);
            }
            return false;
        }
        /*
         * public Operator GetInstance() {
         *      // This is where polymorphism would've made the code cleaner
         *      if (Type == OperatorType.Uniq) {
         *          return GetUniqOpInstance();
         *      } else if (...)  { ... }
         *      
         * }
         */
    }

}