using Newtonsoft.Json;
using System;
using System.Collections.Generic;

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
        public List<string> Addrs = new List<string>();
        public List<string> Args { get; set; } = new List<string>();
        public List<OutputOperator> OutputOperators { get; set; } = new List<OutputOperator>();

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