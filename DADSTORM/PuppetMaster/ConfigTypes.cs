using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
/// <summary>
/// Contains the type definitions used in the Config class.
/// </summary>
namespace PuppetMaster
{
    public enum LoggingLevel
    {
        Full, Light
    }

    public enum Semantics
    {
        AtLeastOnce, AtMostOnce, ExactlyOnce
    }
}
