using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace PuppetMaster.Exceptions
{
    [Serializable]
    class NullReferrencePropertyException : Exception
    {
        public NullReferrencePropertyException()
        {
            // empty
        }

        public NullReferrencePropertyException(string message) : base(message)
        {
            // empty
        }

        public NullReferrencePropertyException(string message, Exception innerException) : base(message, innerException)
        {
            // empty
        }

        protected NullReferrencePropertyException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            // empty
        }
    }
}
