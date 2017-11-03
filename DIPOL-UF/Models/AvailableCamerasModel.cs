using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ANDOR_CS.Classes;

namespace DIPOL_UF.Models
{
    class AvailableCamerasModel : ObservableObject
    {
        private ObservableCollection<CameraBase> foundCameras = new ObservableCollection<CameraBase>();

        public ObservableCollection<CameraBase> FoundCameras => foundCameras;

        public AvailableCamerasModel()
        {
        }
    }
}
