﻿using System;

namespace ConfigTypes
{
    [Serializable]
    public class OperatorRouting
    {
        public RoutingType Type { get; set; }
        public int Arg { get; set; } // only applies if Type is Routing.Hashing
    }
}