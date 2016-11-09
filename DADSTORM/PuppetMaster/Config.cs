﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuppetMaster
{
    public class Config
    {
        public LoggingLevel LoggingLevel { get; set; }
        public List<OperatorSpec> Operators { get; set; }
        public Semantics Semantics { get; set; }
        public Queue<Command> commands { get; set; }
    }
}
