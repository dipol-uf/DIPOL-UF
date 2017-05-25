﻿//    This file is part of Dipol-3 Camera Manager.

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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SDK = ATMCD64CS.AndorSDK;

namespace ANDOR_CS.Enums
{
    [Flags]
    public enum ReadMode : uint
    {
        Unknown = 0,
        FullImage = SDK.AC_READMODE_FULLIMAGE,
        SubImage = SDK.AC_READMODE_SUBIMAGE,
        SingleTrack = SDK.AC_READMODE_SINGLETRACK,
        FullVerticalBinning = SDK.AC_READMODE_FVB,
        MultiTrack = SDK.AC_READMODE_MULTITRACK,
        RandomTrack = SDK.AC_READMODE_RANDOMTRACK,
        MultiTrackScan = SDK.AC_READMODE_MULTITRACKSCAN
    }
}
