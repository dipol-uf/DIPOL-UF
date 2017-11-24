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
