using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ServiceModel;

using System.Runtime.Serialization;

namespace DIPOL_Remote.Faults
{
    [DataContract(IsReference = false)]
    public class AndorSDKServiceException : ServiceException
    {
        public static FaultException<AndorSDKServiceException> WrapAndorSDKException(
            ANDOR_CS.Exceptions.AndorSDKException e,
            string method)
            => new FaultException<AndorSDKServiceException>(
                new AndorSDKServiceException()
                {
                    Message = "Andor SDK failed to execute operation.",
                    Details = e.Message,
                    ErrorCode = e.ErrorCode,
                    MethodName = method
                },
                CameraCommunicationReason); 
            


        [DataMember(IsRequired = true)]
        public uint ErrorCode;
        
    }
}
