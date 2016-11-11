using System;
using System.Runtime.Serialization;

namespace PuppetMaster
{
    [Serializable]
    internal class UnknownCommandTypeException : Exception
    {
        public UnknownCommandTypeException()
        {
        }

        public UnknownCommandTypeException(string message) : base(message)
        {
        }

        public UnknownCommandTypeException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected UnknownCommandTypeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}