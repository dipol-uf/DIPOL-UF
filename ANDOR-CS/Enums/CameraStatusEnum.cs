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
    public enum CameraStatus : uint
    {
        Idle = SDK.DRV_IDLE,
        TemperatureCycle = SDK.DRV_TEMPCYCLE,
        Acquiring = SDK.DRV_ACQUIRING,
        AccumulateCycleTimeNotMet = SDK.DRV_ACCUM_TIME_NOT_MET,
        KineticCycleTimeNotMet = SDK.DRV_KINETIC_TIME_NOT_MET,
        CommunicationError = SDK.DRV_ERROR_ACK,
        AcquisitionBufferRate = SDK.DRV_ACQ_BUFFER,
        CameraMemoryFull = SDK.DRV_ACQ_DOWNFIFO_FULL,
        SpoolBufferOverflow = SDK.DRV_SPOOLERROR
    }
}
