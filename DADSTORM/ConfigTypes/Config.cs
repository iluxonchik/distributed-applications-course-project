using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConfigTypes
{
    public class Config
    {
        public LoggingLevel LoggingLevel { get; set; }
        public List<OperatorSpec> Operators { get; set; }
        public Semantics Semantics { get; set; }
        public Queue<Command> commands { get; set; }
        public Dictionary<string, OperatorSpec> OPnameToOpSpec { get; set; } // needed by CommandParser

        /// <summary>
        /// Set PuppetMaster's URL for each operator.
        /// 
        /// This method should be called in the PuppetMasterGUI, since only there its url is known.
        /// </summary>
        /// <param name="url">PuppetMaster's URL</param>
        public void SetPuppetMasterUrl(string url)
        {
            foreach(var op in Operators)
            {
                op.PuppetMasterUrl = url;
            }
        }
    }
}
