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

using SDK = ATMCD64CS.AndorSDK;

namespace ANDOR_CS.Enums
{
    [Flags]
    [DataContract]
    public enum ReadMode : uint
    {
        [Description("Unknown")]
        [EnumMember]
        Unknown = 0,

        [Description("Full image")]
        [EnumMember]
        FullImage = SDK.AC_READMODE_FULLIMAGE,

        [Description("Subimage")]
        [EnumMember]
        SubImage = SDK.AC_READMODE_SUBIMAGE,

        [Description("Single track")]
        [EnumMember]
        SingleTrack = SDK.AC_READMODE_SINGLETRACK,

        [Description("Full v-bin")]
        [EnumMember]
        FullVerticalBinning = SDK.AC_READMODE_FVB,

        [Description("Multitrack")]
        [EnumMember]
        MultiTrack = SDK.AC_READMODE_MULTITRACK,

        [Description("Random track")]
        [EnumMember]
        RandomTrack = SDK.AC_READMODE_RANDOMTRACK,

        [Description("Multitrack scan")]
        [EnumMember]
        MultiTrackScan = SDK.AC_READMODE_MULTITRACKSCAN
    }
}
