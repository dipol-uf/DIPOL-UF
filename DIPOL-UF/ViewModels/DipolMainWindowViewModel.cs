﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using DIPOL_UF.Models;
using DynamicData;
using DynamicData.Alias;
using DynamicData.Kernel;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace DIPOL_UF.ViewModels
{
    internal sealed class DipolMainWindowViewModel : ReactiveViewModel<DipolMainWindow>
    {
        public DipolMainWindowViewModel(DipolMainWindow model) : base(model)
        {
            //ConnectedCameras.CollectionChanged += (sender, e) => RaisePropertyChanged(nameof(AnyCameraConnected));
            HookObservables();
            HookValidators();
        }

        private void HookObservables()
        {
            Model.ConnectedCameras.CountChanged
                 .Select(x => x != 0)
                 .ObserveOnUi()
                 .ToPropertyEx(this, x => x.AnyCameraConnected)
                 .DisposeWith(_subscriptions);

            Model.ConnectedCameras.Connect().Transform(x => x.Id).Bind(out var coll1).Subscribe()
                 .DisposeWith(_subscriptions);

            Model.SelectedDevices.Connect().Bind(out var coll2).Subscribe().DisposeWith(_subscriptions);

            //coll1.Except(coll2).Select()
        }
        //public bool? CameraPanelAreAllSelected => model.CameraPanelAreAllSelected;

        // ReSharper disable UnassignedGetOnlyAutoProperty
        public bool AnyCameraConnected { [ObservableAsProperty] get; }
        public bool AllCamerasSelected { [ObservableAsProperty] get; }
        // ReSharper restore UnassignedGetOnlyAutoProperty

        //public ICommand CameraPanelSelectedAllCommand => model.CameraPanelSelectedAllCommand;
        public ICommand ConnectButtonCommand => Model.ConnectButtonCommand;
        public ICommand DisconnectButtonCommand => Model.DisconnectButtonCommand;
        //public ICommand CameraPanelSelectionChangedCommand => model.CameraPanelSelectionChangedCommand;

        public ICommand WindowLoadedCommand => Model.WindowLoadedCommand;

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
