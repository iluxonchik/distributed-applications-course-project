using System;
using System.Runtime.Serialization;

namespace ConfigTypes.Exceptions
{
    [Serializable]
   public  class UnknownOperatorRoutingException : Exception
    {
        public UnknownOperatorRoutingException()
        {
            // empty
        }

        public UnknownOperatorRoutingException(string message) : base(message)
        {
            // empty
        }

        public UnknownOperatorRoutingException(string message, Exception innerException) : base(message, innerException)
        {
            // empty
        }

        protected UnknownOperatorRoutingException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            // empty
        }
    }
}