using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows;

using ANDOR_CS.Classes;

namespace DIPOL_UF.ViewModels
{
    class ConnectedCamerasTreeViewModel : ViewModel<Models.ConnectedCamerasTreeModel>
    {

        public ConnectedCamerasTreeViewModel(Models.ConnectedCamerasTreeModel model)
            :base(model)
        {
        }

        public string Name => model.Name;
        public ObservableConcurrentDictionary<string, ConnectedCameraTreeItemViewModel> CameraList => model.CameraList;
    }
}
