using OperatorProxys;
using System;

namespace Operator
{
    [Serializable]
    internal class OutgoingTuple
    {
        public OperatorTuple Tuple { get; set; }
        public long TimeSent { get; set; }

    }
}