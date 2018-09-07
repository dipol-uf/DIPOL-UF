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
