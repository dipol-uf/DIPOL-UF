using System.Collections.ObjectModel;
using ANDOR_CS.Classes;

namespace DIPOL_UF.Models
{
    class ConnectedCameraTreeItemModel : ObservableObject
    {

        private CameraBase camera = null;
        private ObservableCollection<ViewModels.MenuItemViewModel> contextMenu = new ObservableCollection<ViewModels.MenuItemViewModel>();

        public CameraBase Camera
        {
            get => camera;
            set
            {
                if (value != camera)
                {
                    camera = value;
                    RaisePropertyChanged();
                }
            }

        }
        public ObservableCollection<ViewModels.MenuItemViewModel> ContextMenu
        {
            get => contextMenu;
            set
            {
                if (value != contextMenu)
                {
                    contextMenu = value;
                    RaisePropertyChanged();
                }
            }
        }

        public ConnectedCameraTreeItemModel(CameraBase cam)
        {
            camera = cam;
        }
    }
}
