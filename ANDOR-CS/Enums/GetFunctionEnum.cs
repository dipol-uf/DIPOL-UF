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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SDK = ATMCD64CS.AndorSDK;

namespace ANDOR_CS.Enums
{
    [Flags]
    public enum GetFunction : uint
    {
        Unknown = 0,
        Temperature =  SDK.AC_GETFUNCTION_TEMPERATURE,
        TemperatureRange = SDK.AC_GETFUNCTION_TEMPERATURERANGE,
        DetectorSize = SDK.AC_GETFUNCTION_DETECTORSIZE,
        MCPGain = SDK.AC_GETFUNCTION_MCPGAIN,
        EMCCDGain = SDK.AC_GETFUNCTION_EMCCDGAIN,
        GateMode = SDK.AC_GETFUNCTION_GATEMODE,
        DDGTimes = SDK.AC_GETFUNCTION_DDGTIMES,
        DDGIntegrateOnChip = SDK.AC_GETFUNCTION_IOC,
        Intelligate = SDK.AC_GETFUNCTION_INTELLIGATE,
        InsertionDelay = SDK.AC_GETFUNCTION_INSERTION_DELAY,
        PhosphorStatus = SDK.AC_GETFUNCTION_PHOSPHORSTATUS,
        BaselineClamp = SDK.AC_GETFUNCTION_BASELINECLAMP
    }
}
