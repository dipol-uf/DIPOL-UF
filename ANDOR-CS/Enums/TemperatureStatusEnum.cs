//    This file is part of Dipol-3 Camera Manager.

//     MIT License
//     
//     Copyright(c) 2018-2019 Ilia Kosenkov
//     
//     Permission is hereby granted, free of charge, to any person obtaining a copy
//     of this software and associated documentation files (the "Software"), to deal
//     in the Software without restriction, including without limitation the rights
//     to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//     copies of the Software, and to permit persons to whom the Software is
//     furnished to do so, subject to the following conditions:
//     
//     The above copyright notice and this permission notice shall be included in all
//     copies or substantial portions of the Software.
//     
//     THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//     IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//     FITNESS FOR A PARTICULAR PURPOSE AND NONINFINGEMENT. IN NO EVENT SHALL THE
//     AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//     LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//     OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//     SOFTWARE.

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
