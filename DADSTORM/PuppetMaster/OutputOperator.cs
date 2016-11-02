using System;
using System.Collections.Generic;

namespace PuppetMaster
{
    [Serializable]
    public class OutputOperator
    {

        public OutputOperator(OperatorSpec op)
        {
            Name = op.Id;
            Addresses = op.Addrs;
        }

        public OutputOperator()
        {
            // empty
        }

        public string Name { get; set; }
        public List<string> Addresses { get; set; } = new List<string>();
    }
}