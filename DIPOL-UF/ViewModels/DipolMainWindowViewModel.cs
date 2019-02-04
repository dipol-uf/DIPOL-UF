using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using DIPOL_UF.Models;
using DynamicData;
using DynamicData.Binding;
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

            Model.SelectedDevices.CountChanged
                 .CombineLatest(
                     Model.ConnectedCameras.CountChanged,
                          (x, y) =>
                          {
                              if (x == 0)
                                  return false;

                              return x < y ? null : new bool?(true);
                          })
                      .ObserveOnUi()
                      .ToPropertyEx(this, x => x.AllCamerasSelected)
                      .DisposeWith(_subscriptions);


            Model.ConnectedCameras.Connect()
                 .Group(x => Helper.GetCameraHostName(x.Id))
                 .ObserveOnUi()
                 .Transform(x => 
                     new MainWindowTreeViewModel(x.Key, x.Cache, 
                         Model.SelectedDevices, 
                         Model.SelectCameraCommand,
                         Model.ContextMenuCommand))
                 .Bind(CameraPanel)
                 .DisposeMany()
                 .Subscribe()
                 .DisposeWith(_subscriptions);

            
        }


        
        public IObservableCollection<MainWindowTreeViewModel> CameraPanel { get; }
            = new ObservableCollectionExtended<MainWindowTreeViewModel>();

        // ReSharper disable UnassignedGetOnlyAutoProperty
        public bool AnyCameraConnected { [ObservableAsProperty] get; }
        public bool? AllCamerasSelected { [ObservableAsProperty] get; }
        // ReSharper restore UnassignedGetOnlyAutoProperty

        public ICommand SelectAllCamerasCommand => Model.SelectAllCamerasCommand;
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
