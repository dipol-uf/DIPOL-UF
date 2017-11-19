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
        }

        public bool? CameraTreeViewSelectedAll => model.CameraTreeViewSelectedAll;


        public ICommand CameraTreeViewSelectAllCommand => model.CameraTreeViewSelectAllCommand;
        public ICommand ConnectButtonCommand => model.ConnectButtonCommand as ICommand;
        public ICommand DisconnectButtonCommand => model.DisconnectButtonCommand as ICommand;
        public ICommand CameraTreeViewSelectionChangedCommand => model.CameraTreeViewSelectionChangedCommand as ICommand;

        public ObservableCollection<MenuItemViewModel> MenuBarItems => model.MenuBarItems;
        public ObservableConcurrentDictionary<string, CameraBase> ConnectedCameras => model.ConnectedCameras;
        public ObservableConcurrentDictionary<string, bool> CameraTreeViewSelectedItems => model.CameraTreeViewSelectedItems;

        public ObservableCollection<ConnectedCamerasTreeViewModel> CameraTreeRepresentation => model.CameraTreeRepresentation;
        public ObservableConcurrentDictionary<string, Dictionary<string, object>> CameraRealTimeStats => model.CameraRealTimeStats;
    }
}
