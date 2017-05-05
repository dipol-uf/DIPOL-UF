using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SDK = ATMCD64CS.AndorSDK;

namespace ANDOR_CS.Enums
{
    public enum PixelMode : uint
    {
        /// <summary>
        /// 8-bit mode
        /// </summary>
        Bits8 =  SDK.AC_PIXELMODE_8BIT,

        /// <summary>
        /// 14-bit mode
        /// </summary>
        Bits14 = SDK.AC_PIXELMODE_14BIT,

        /// <summary>
        /// 16-bit mode
        /// </summary>
        Bits16 = SDK.AC_PIXELMODE_16BIT,

        /// <summary>
        /// 32-bit mode
        /// </summary>
        Bits32 = SDK.AC_PIXELMODE_32BIT,

        /// <summary>
        /// Grey scale
        /// </summary>
        Mono = SDK.AC_PIXELMODE_MONO,

        /// <summary>
        /// RGB colors
        /// </summary>
        RGB =  SDK.AC_PIXELMODE_RGB,


        /// <summary>
        /// CMY colors
        /// </summary>
        CMY = SDK.AC_PIXELMODE_CMY

    }
}
