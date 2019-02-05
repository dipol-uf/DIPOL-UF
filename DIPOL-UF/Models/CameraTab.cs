using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ANDOR_CS.Classes;
using ANDOR_CS.Enums;

namespace DIPOL_UF.Models
{
    internal sealed class CameraTab: ReactiveObjectEx
    {
        private readonly CameraBase Camera;
        public (float Minimum, float Maximum) TemperatureRange { get; }

        public CameraTab(CameraBase camera)
        {
            Camera = camera;

            TemperatureRange = camera.Capabilities.GetFunctions.HasFlag(GetFunction.TemperatureRange)
                ? camera.Properties.AllowedTemperatures
                : default;
        }

        public override void Dispose(bool disposing)
        {
            Helper.WriteLog("Disposing Tab model");
            base.Dispose(disposing);
        }
    }
}
