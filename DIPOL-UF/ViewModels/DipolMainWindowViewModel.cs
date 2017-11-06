using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows;

using DIPOL_UF.Models;

using ANDOR_CS.Classes;

namespace DIPOL_UF.ViewModels
{
    class DipolMainWindowViewModel : ViewModel<DipolMainWindow>
    {
        public DipolMainWindowViewModel(DipolMainWindow model) : base(model)
        {
            model.PropertyChanged += (sender, e) => Helper.WriteLog(e.PropertyName);
        }

        public ICommand ConnectButtonCommand => model.ConnectButtonCommand as ICommand;
        public ICommand DisconnectButtonCommand => model.DisconnectButtonCommand as ICommand;

        public ObservableCollection<MenuItemViewModel> MenuBarItems => model.MenuBarItems;
        public ObservableConcurrentDictionary<string, CameraBase> ConnectedCameras => model.ConnectedCameras;

        public ObservableCollection<ConnectedCamerasTreeViewModel> TreeCameraRepresentation => model.TreeCameraRepresentation;

    }
}
