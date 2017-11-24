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

        public bool? CameraPanelAreAllSelected => model.CameraPanelAreAllSelected;


        public ICommand CameraPanelSelectedAllCommand => model.CameraPanelSelectedAllCommand;
        public ICommand ConnectButtonCommand => model.ConnectButtonCommand as ICommand;
        public ICommand DisconnectButtonCommand => model.DisconnectButtonCommand as ICommand;
        public ICommand CameraPanelSelectionChangedCommand => model.CameraPanelSelectionChangedCommand as ICommand;

        public ObservableCollection<MenuItemViewModel> MenuBarItems => model.MenuBarItems;
        public ObservableConcurrentDictionary<string, CameraBase> ConnectedCameras => model.ConnectedCameras;
        public ObservableConcurrentDictionary<string, bool> CameraPanelSelectedItems =>
            model.CameraPanelSelectedItems;

        public ObservableCollection<ConnectedCamerasTreeViewModel> CameraPanel => 
            model.CameraPanel;
        public ObservableConcurrentDictionary<string, Dictionary<string, object>> CameraRealTimeStats => 
            model.CameraRealTimeStats;
    }
}
