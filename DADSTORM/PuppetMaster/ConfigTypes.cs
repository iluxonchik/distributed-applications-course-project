using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
/// <summary>
/// Contains the type definitions used in the Config class.
/// </summary>
/// 
/* 
 * NOTE:
 * I am aware that using enums to typify a class is not the best practice, a better choice
 * would be to use subclassing, but config parsing isn't an essential part of the project
 * and this won't be mantained in the future.  
 */
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

    public enum OperatorType
    {
        Uniq, Count, Dup, Filter, Custom
    }

    public enum InputType
    {
        File, Operator
    }

    public enum RoutingType
    {
        Primary, Random, Hashing
    }
}
