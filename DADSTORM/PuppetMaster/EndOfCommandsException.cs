using System;
using System.Runtime.Serialization;

namespace PuppetMaster
{
    [Serializable]
    public class EndOfCommandsException : Exception
    {


        public EndOfCommandsException()
        {
        }

        public EndOfCommandsException(string message) : base(message)
        {
        }

        public EndOfCommandsException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected EndOfCommandsException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}

