using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ANDOR_CS
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

        public override string ToString()
        {
            return string.Format("{0} [{1}]", base.Message, ErrorCode);
        }

        public static void ThrowIfError(uint returnCode, string name)
        {
            if (returnCode != ATMCD64CS.AndorSDK.DRV_SUCCESS)
                throw new AndorSDKException($"{name} returned error code.",
                    returnCode);
        }
    }
}
