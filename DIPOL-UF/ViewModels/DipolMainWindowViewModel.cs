using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using DIPOL_UF.Models;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
// ReSharper disable UnassignedGetOnlyAutoProperty

namespace DIPOL_UF.ViewModels
{
    internal sealed class DipolMainWindowViewModel : ReactiveViewModel<DipolMainWindow>
    {
        
        public IObservableCollection<MainWindowTreeViewModel> CameraPanel { get; }
            = new ObservableCollectionExtended<MainWindowTreeViewModel>();

        public IObservableCollection<CameraTabViewModel> CameraTabs { get; }
            = new ObservableCollectionExtended<CameraTabViewModel>();

        public bool AnyCameraConnected { [ObservableAsProperty] get; }
        public bool? AllCamerasSelected { [ObservableAsProperty] get; }
        public ICommand SelectAllCamerasCommand => Model.SelectAllCamerasCommand;
        public ICommand ConnectButtonCommand => Model.ConnectButtonCommand;
        public ICommand DisconnectButtonCommand => Model.DisconnectButtonCommand;
        public ICommand WindowLoadedCommand => Model.WindowLoadedCommand;

        public DescendantProxy ProgressBarProxy { get; }
        public DescendantProxy AvailableCamerasProxy { get; }
        //public ObservableCollection<MenuItemViewModel> MenuBarItems => model.MenuBarItems;


        public DipolMainWindowViewModel(DipolMainWindow model) : base(model)
        {
            ProgressBarProxy = new DescendantProxy(
                Model.ProgressBarProvider,
                x => new ProgressBarViewModel((ProgressBar)x))
                .DisposeWith(_subscriptions);
                
            AvailableCamerasProxy = new DescendantProxy(
                Model.AvailableCamerasProvider,
                x => new AvailableCamerasViewModel((AvailableCamerasModel)x))
                .DisposeWith(_subscriptions);

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
                         x == 0
                             ? false
                             : x < y
                                 ? null
                                 : new bool?(true))
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

            Model.CameraTabs.Connect()
                 .ObserveOnUi()
                 .Transform(x => new CameraTabViewModel(x.Tab))
                 .Bind(CameraTabs)
                 .DisposeMany()
                 .Subscribe()
                 .DisposeWith(_subscriptions);

        }
    }
}
