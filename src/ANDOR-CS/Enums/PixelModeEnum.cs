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
    public enum PixelMode : uint
    {
        /// <summary>
        /// 8-bit mode
        /// </summary>
        [EnumMember]
        Bits8 =  SDK.AC_PIXELMODE_8BIT,

        /// <summary>
        /// 14-bit mode
        /// </summary>
        [EnumMember]
        Bits14 = SDK.AC_PIXELMODE_14BIT,

        /// <summary>
        /// 16-bit mode
        /// </summary>
        [EnumMember]
        Bits16 = SDK.AC_PIXELMODE_16BIT,

        /// <summary>
        /// 32-bit mode
        /// </summary>
        [EnumMember]
        Bits32 = SDK.AC_PIXELMODE_32BIT,

        /// <summary>
        /// Grey scale
        /// </summary>
        [EnumMember]
        Mono = SDK.AC_PIXELMODE_MONO,

        /// <summary>
        /// RGB colors
        /// </summary>
        [EnumMember]
        Rgb =  SDK.AC_PIXELMODE_RGB,

        /// <summary>
        /// CMY colors
        /// </summary>
        [EnumMember]
        Cmy = SDK.AC_PIXELMODE_CMY

    }
}
