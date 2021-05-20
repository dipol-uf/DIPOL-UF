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
using System.Runtime.Serialization;
using Serializers;
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
    public enum GetFunction : uint
    {
        [IgnoreDefault]
        [EnumMember]
        Unknown = 0,
        [EnumMember]
        Temperature =  SDK.AC_GETFUNCTION_TEMPERATURE,
        [EnumMember]
        TemperatureRange = SDK.AC_GETFUNCTION_TEMPERATURERANGE,
        [EnumMember]
        DetectorSize = SDK.AC_GETFUNCTION_DETECTORSIZE,
        [EnumMember]
        McpGain = SDK.AC_GETFUNCTION_MCPGAIN,
        [EnumMember]
        EmccdGain = SDK.AC_GETFUNCTION_EMCCDGAIN,
        [EnumMember]
        GateMode = SDK.AC_GETFUNCTION_GATEMODE,
        [EnumMember]
        DdgTimes = SDK.AC_GETFUNCTION_DDGTIMES,
        [EnumMember]
        DdgIntegrateOnChip = SDK.AC_GETFUNCTION_IOC,
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
