using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.Serialization;

namespace DIPOL_Remote.Faults
{
    [DataContract(IsReference = false)]
    public class AndorSDKServiceException : ServiceException
    {
        [DataMember(IsRequired = true)]
        public uint ErrorCode;
        
    }
}
