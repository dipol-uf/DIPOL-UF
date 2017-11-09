using DIPOL_UF.ViewModels;

using ANDOR_CS.Classes;

namespace DIPOL_UF.Models
{
    class ConnectedCamerasTreeModel : ObservableObject
    {
        private ObservableConcurrentDictionary<string, ConnectedCameraTreeItemViewModel> cameraList 
            = new ObservableConcurrentDictionary<string, ConnectedCameraTreeItemViewModel>();
        private string name = "";

        public ObservableConcurrentDictionary<string, ConnectedCameraTreeItemViewModel> CameraList
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
