using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace PuppetMaster.Exceptions
{
    [Serializable]
   public class UnknownOperatorTypeException : Exception
    {
        public UnknownOperatorTypeException()
        {
            // empty
        }

        public UnknownOperatorTypeException(string message) : base(message)
        {
            // empty
        }

        public UnknownOperatorTypeException(string message, Exception innerException) : base(message, innerException)
        {
            // empty
        }

        protected UnknownOperatorTypeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            // empty
        }
    }
}
