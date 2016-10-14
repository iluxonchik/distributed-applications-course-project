﻿using System.Collections.Generic;

namespace PuppetMaster
{
    /// <summary>
    /// Represents a specification of an Operator. Contains the id of the operator,
    /// the inputs of the operator,
    /// the type of the operator, replication factor, routing type, address list 
    /// and the of arguments for that operator (if appiles).
    /// </summary>
    public class OperatorSpec
    {
        public string Id { get; set; }
        public List<OperatorInput> Inputs;
        public OperatorType Type { get; set; }
        public int ReplicationFactor { get; set; }
        public OperatorRouting Routing;
        public List<string> Addrs;
        public List<string> Args { get; set; }

        public OperatorSpec()
        {
            // empty on purpose
        }

        public void AddArg(string arg)
        {
            if (Args == null)
            {
                Args = new List<string>();
            }
            Args.Add(arg);
        }

        public void AddAddr(string addr)
        {
            if (Addrs == null)
            {
                Addrs = new List<string>();
            }
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