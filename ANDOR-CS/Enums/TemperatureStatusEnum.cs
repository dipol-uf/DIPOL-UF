//    This file is part of Dipol-3 Camera Manager.

//    Dipol-3 Camera Manager is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.

//    Dipol-3 Camera Manager is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//    GNU General Public License for more details.

//    You should have received a copy of the GNU General Public License
//    along with Dipol-3 Camera Manager.  If not, see<http://www.gnu.org/licenses/>.
//
//    Copyright 2017, Ilia Kosenkov, Tuorla Observatory, Finland

using System;
using System.ComponentModel;
using System.Runtime.Serialization;

#if X86
using SDK = ATMCD32CS.AndorSDK;
#endif
#if X64
using SDK = ATMCD64CS.AndorSDK;
#endif

namespace ANDOR_CS.Enums
{
    [Flags]
    [DataContract]
    public enum TemperatureStatus : uint
    {
        [Description("Off")]
        [EnumMember]
        Off = SDK.DRV_TEMPERATURE_OFF,

        [Description("Stabilized")]
        [EnumMember]
        Stabilized = SDK.DRV_TEMPERATURE_STABILIZED,

        [Description("Not Reached")]
        [EnumMember]
        NotReached = SDK.DRV_TEMPERATURE_NOT_REACHED,

        [Description("Drifting")]
        [EnumMember]
        Drift = SDK.DRV_TEMPERATURE_DRIFT,

        [Description("Not Stabilized")]
        [EnumMember]
        NotStabilized = SDK.DRV_TEMPERATURE_NOT_STABILIZED
    }
}
