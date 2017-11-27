using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ANDOR_CS.Classes;

namespace DIPOL_UF.Models
{
    class ConnectedCamera : ObservableObject
    {
        private CameraBase camera = null;
        private float targetTemperature = 0.0f;

        public float TargetTemperature
        {
            get => targetTemperature;
            set
            {
                if (value != targetTemperature)
                {
                    targetTemperature = value;
                    RaisePropertyChanged();
                }
            }
        }
        public CameraBase Camera
        {
            get => camera;
            private set
            {
                if (value != camera)
                {
                    camera = value;
                    RaisePropertyChanged();
                }
            }
        }


        public ConnectedCamera(CameraBase camera)
        {
            Camera = camera;
        }
    }
}
