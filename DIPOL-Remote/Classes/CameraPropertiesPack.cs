using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ANDOR_CS.Enums;

using System.Runtime.Serialization;

namespace DIPOL_Remote.Classes
{
    [DataContract]
    public struct CameraPropertiesPack
    {
        [DataMember(IsRequired = true)]
        bool IsActive;
        [DataMember(IsRequired = true)]
        bool IsInitialized;
        [DataMember(IsRequired = true)]
        string SerialNumber;
        [DataMember(IsRequired = true)]
        string CameraModel;
        [DataMember(IsRequired = true)]
        FanMode FanMode;
        [DataMember(IsRequired = true)]
        Switch CoolerMode;
        [DataMember(IsRequired = true)]
        int CameraIndex;
    }
}
