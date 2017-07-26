using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.Serialization;
using System.ServiceModel;

namespace DIPOL_Remote.Faults
{
    [DataContract(IsReference = false)]
    public class ServiceException
    {
        public static readonly FaultReason CameraCommunicationReason
            = new FaultReason($"Error occured while communicating with {nameof(ANDOR_CS.Classes.Camera)} object.");

        public static readonly FaultReason GeneralServiceErrorReason
            = new FaultReason("General error occured while communicating with service.");

        public static readonly FaultReason IllegalSessionReason
            = new FaultReason("Accessed camera belongs to another remote session.");

        public static FaultException<ServiceException> IllegalSessionFaultException()
            => new FaultException<ServiceException>(
                        new ServiceException()
                        {
                            Message = "Cannot remove camera used in our session.",
                            Details = "Specified camera is used in anoter session and therefore cannot be controlled from current session.",
                            MethodName = ""
                        },
                        IllegalSessionReason);

        [DataMember(IsRequired = true, Order = 0)]
        public string Message;
        [DataMember(IsRequired = true, Order = 1)]
        public string Details;
        [DataMember(IsRequired = true, Order = 2)]
        public string MethodName;
    }
}
