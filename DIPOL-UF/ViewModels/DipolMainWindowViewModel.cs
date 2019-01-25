using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using DIPOL_UF.Models;

namespace DIPOL_UF.ViewModels
{
    class DipolMainWindowViewModel : ViewModel<DipolMainWindow>
    {
       public DipolMainWindowViewModel(DipolMainWindow model) : base(model)
        {
            //ConnectedCameras.CollectionChanged += (sender, e) => RaisePropertyChanged(nameof(AnyCameraConnected));
        }

        //public bool? CameraPanelAreAllSelected => model.CameraPanelAreAllSelected;

        //public bool AnyCameraConnected => !model.ConnectedCameras.IsEmpty;

        public ICommand CameraPanelSelectedAllCommand => model.CameraPanelSelectedAllCommand;
        public ICommand ConnectButtonCommand => model.ConnectButtonCommand;
        public ICommand DisconnectButtonCommand => model.DisconnectButtonCommand;
        public ICommand CameraPanelSelectionChangedCommand => model.CameraPanelSelectionChangedCommand;

        public ICommand WindowLoadedCommand => model.WindowLoadedCommand;

        //public ObservableCollection<MenuItemViewModel> MenuBarItems => model.MenuBarItems;
        //public ObservableConcurrentDictionary<string, ConnectedCameraViewModel> ConnectedCameras => model.ConnectedCameras;
        //public ObservableConcurrentDictionary<string, ConnectedCameraViewModel>.ObservableValueCollection
        //    ConnectedCameras => model.ConnectedCameras.ObservableValues();
        //public ObservableConcurrentDictionary<string, bool> CameraPanelSelectedItems =>
        //    model.CameraPanelSelectedItems;

        //public ObservableCollection<ConnectedCamerasTreeViewModel> CameraPanel => 
        //    model.CameraPanel;
        //public ObservableConcurrentDictionary<string, Dictionary<string, object>> CameraRealTimeStats => 
        //    model.CameraRealTimeStats;

    }
}
