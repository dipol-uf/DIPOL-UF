using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.Serialization;

namespace DIPOL_Remote.Faults
{
    [DataContract(IsReference = false)]
    public class ServiceException
    {
        [DataMember(IsRequired = true, Order = 0)]
        public string Message;
        [DataMember(IsRequired = true, Order = 1)]
        public string Details;
        [DataMember(IsRequired = true, Order = 2)]
        public string MethodName;
    }
}
