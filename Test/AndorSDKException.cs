using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    public class AndorSDKException : Exception
    {
        public uint ErrorCode
        {
            get;
            private set;
        } = 0;

        public AndorSDKException(string message, Exception innerException) 
            : base(message, innerException)
        { }

        public AndorSDKException(string message, uint errorCode)
            : base(message)
        {
            ErrorCode = errorCode;
        }
    }
}
