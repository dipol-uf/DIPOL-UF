using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows;

using ANDOR_CS.Classes;

namespace DIPOL_UF.Models
{
    class ConnectedCamerasTreeModel : ObservableObject
    {
        private ObservableConcurrentDictionary<string, CameraBase> cameraList = new ObservableConcurrentDictionary<string, CameraBase>();
        private string name = "";

        public ObservableConcurrentDictionary<string, CameraBase> CameraList
        {
            get => cameraList;
            set
            {
                if (value != cameraList)
                {
                    cameraList = value;
                    RaisePropertyChanged();
                }
            }
        }

        public string Name
        {
            get => name;
            set
            {
                if (value != name)
                {
                    name = value;
                    RaisePropertyChanged();
                }
            }
        }

    }
}
