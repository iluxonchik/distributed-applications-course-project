using System;
using System.Collections.Generic;

namespace ConfigTypes
{
    [Serializable]
    public class OperatorOutput
    {

        public OperatorOutput(OperatorSpec op)
        {
            Name = op.Id;
            Addresses = op.Addrs;
        }

        public OperatorOutput()
        {
            // empty
        }

        public string Name { get; set; }
        public List<string> Addresses { get; set; } = new List<string>();
    }
}