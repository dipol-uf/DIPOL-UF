using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DIPOL_UF.Models;

using ANDOR_CS.Classes;
using ANDOR_CS.Enums;

namespace DIPOL_UF.ViewModels
{
    class ConnectedCameraViewModel : ViewModel<ConnectedCamera>
    {
        public CameraBase Camera => model.Camera;

        public float MinimumAllowedTemperature => model.Camera.Properties.AllowedTemperatures.Minimum;
        public float MaximumAllowedTemperature => model.Camera.Properties.AllowedTemperatures.Maximum;

        public float TargetTemperature
        {
            get => model.TargetTemperature;
            set
            {
                if (value != model.TargetTemperature)
                    model.TargetTemperature = value;
            }
        }


        public ConnectedCameraViewModel(ConnectedCamera model) : base(model)
        {
            
        }
    }
}
