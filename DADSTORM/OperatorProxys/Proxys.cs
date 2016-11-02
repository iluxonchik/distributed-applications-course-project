using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace OperatorProxys
{
    /// <summary>
    /// Interfece of all nodes that process stuff
    /// Configuration Commnds that the Operator offers to PuppetMaster
    /// </summary>
    public interface IProcessingNodesProxy
    {
        /// <summary>
        /// starts the operator
        /// 
        /// </summary>
        void Start();

        /// <summary>
        /// tells the operator to sleep x milliseconds between two consecutive events.
        /// </summary>
        /// <pre>
        /// x_ms>=0
        /// </pre>
        /// <param name="x_ms"> number of milliseconds</param>
        void Interval(int x_ms);

        /// <summary>
        /// tells the node to print its status on the console
        /// should present brief information about the state of the system 
        /// (who is present, which nodes are presumed failed, etc...).
        /// </summary>
        void Status();

        /*
         * Replicas debug commands 
         */

        /// <summary>
        /// kill's the process
        /// </summary>
        void Crash();

        /// <summary>
        /// freeze the process, it continues receiving messages but stops processing them
        ///until the PuppetMaster \unfreezes" it.
        ///it enqueue the input messages
        /// </summary>
        void Freeze();

        /// <summary>
        /// unfreezes  the process
        /// </summary>
        void UnFreeze();


    }

    public interface IOperatorProxy
    {
        // it may be usefull for the operator to be able to receive a tuple or a collection of tuples


        /// <summary>
        /// The operator that wants to send tuples, calls this service from "OperatorDestination"
        /// and gives him the tuples that he wants to
        /// </summary>
        // TODO: tuple er string ou ser ADT (abstract data type)
        /// Assuming a tuple is represented By a string array, it can be a Abstract type
        /// 
        /// <param name="tuples">list containing the tuples</param>
        void ReceiveTuples(List<OperatorTuple> tuples);

        
        /// <summary>
        /// The operator that wants to send tuples, calls this service from "OperatorDestination"
        /// and gives him the tuples that he wants to
        /// </summary>
        /// <param name="tuple"></param>
        void ReceiveTuple(OperatorTuple tuple);
    }
    
}
