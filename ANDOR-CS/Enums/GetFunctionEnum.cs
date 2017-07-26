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
    public enum GetFunction : uint
    {
        [EnumMember]
        Unknown = 0,
        [EnumMember]
        Temperature =  SDK.AC_GETFUNCTION_TEMPERATURE,
        [EnumMember]
        TemperatureRange = SDK.AC_GETFUNCTION_TEMPERATURERANGE,
        [EnumMember]
        DetectorSize = SDK.AC_GETFUNCTION_DETECTORSIZE,
        [EnumMember]
        MCPGain = SDK.AC_GETFUNCTION_MCPGAIN,
        [EnumMember]
        EMCCDGain = SDK.AC_GETFUNCTION_EMCCDGAIN,
        [EnumMember]
        GateMode = SDK.AC_GETFUNCTION_GATEMODE,
        [EnumMember]
        DDGTimes = SDK.AC_GETFUNCTION_DDGTIMES,
        [EnumMember]
        DDGIntegrateOnChip = SDK.AC_GETFUNCTION_IOC,
        [EnumMember]
        Intelligate = SDK.AC_GETFUNCTION_INTELLIGATE,
        [EnumMember]
        InsertionDelay = SDK.AC_GETFUNCTION_INSERTION_DELAY,
        [EnumMember]
        PhosphorStatus = SDK.AC_GETFUNCTION_PHOSPHORSTATUS,
        [EnumMember]
        BaselineClamp = SDK.AC_GETFUNCTION_BASELINECLAMP
    }
}
