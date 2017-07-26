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
using System.Runtime.Serialization;

using SDK = ATMCD64CS.AndorSDK;

namespace ANDOR_CS.Enums
{

    [Flags]
    [DataContract]
    public enum TriggerMode : uint
    {
        [EnumMember]
        Unknown = 0,
        [EnumMember]
        Internal = SDK.AC_TRIGGERMODE_INTERNAL,
        [EnumMember]
        External = SDK.AC_TRIGGERMODE_EXTERNAL,
        [EnumMember]
        ExternalFVBEM = SDK.AC_TRIGGERMODE_EXTERNAL_FVB_EM,
        [EnumMember]
        Continuous = SDK.AC_TRIGGERMODE_CONTINUOUS,
        [EnumMember]
        ExternalStart = SDK.AC_TRIGGERMODE_EXTERNALSTART,

        /// <summary>
        /// WARNING! Deprecated by <see cref="TriggerMode.ExternalExposure"/>
        /// </summary>
        [EnumMember]
        Bulb = SDK.AC_TRIGGERMODE_BULB,
        [EnumMember]
        ExternalExposure = SDK.AC_TRIGGERMODE_EXTERNALEXPOSURE,
        [EnumMember]
        Inverted = SDK.AC_TRIGGERMODE_INVERTED,
        [EnumMember]
        ExternalChargeshifting = SDK.AC_TRIGGERMODE_EXTERNAL_CHARGESHIFTING
    }

    
}
