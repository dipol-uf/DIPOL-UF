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

        public static readonly FaultReason CameraCommunicationReason
            = new FaultReason($"Error occured while communicating with {nameof(ANDOR_CS.Classes.Camera)} object.");


        [DataMember(IsRequired = true, Order = 0)]
        public string Message;
        [DataMember(IsRequired = true, Order = 1)]
        public string Details;
        [DataMember(IsRequired = true, Order = 2)]
        public string MethodName;
    }
}
