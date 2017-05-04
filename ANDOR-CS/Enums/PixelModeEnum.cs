using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SDK = ATMCD64CS.AndorSDK;

namespace ANDOR_CS.Enums
{
    public enum PixelMode : int
    {
        /// <summary>
        /// 8-bit mode
        /// </summary>
        Bits8 = (int) SDK.AC_PIXELMODE_8BIT,

        /// <summary>
        /// 14-bit mode
        /// </summary>
        Bits14 = (int)SDK.AC_PIXELMODE_14BIT,

        /// <summary>
        /// 16-bit mode
        /// </summary>
        Bits16 = (int)SDK.AC_PIXELMODE_16BIT,

        /// <summary>
        /// Grey scale
        /// </summary>
        Mono = (int)SDK.AC_PIXELMODE_MONO,

        /// <summary>
        /// RGB colors
        /// </summary>
        RGB = (int) SDK.AC_PIXELMODE_RGB,


        /// <summary>
        /// CMY colors
        /// </summary>
        CMY = (int)SDK.AC_PIXELMODE_CMY

    }
}
