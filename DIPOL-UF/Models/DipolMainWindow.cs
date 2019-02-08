using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Resources;
using System.Threading;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using DIPOL_UF.ViewModels;

using ANDOR_CS.Classes;
using ANDOR_CS.Enums;
using ANDOR_CS.Events;

using DIPOL_Remote.Classes;
using DIPOL_UF.Views;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace DIPOL_UF.Models
{
    internal sealed class DipolMainWindow : ReactiveObjectEx
    {
        //private readonly DispatcherTimer _uiStatusUpdateTimer;

        //private bool? _camPanelAreAllSelected = false;


        ///// <summary>
        ///// Disconnect button command
        ///// </summary>
        //private DelegateCommand _disconnectButtonCommand;
        ///// <summary>
        ///// Handles selection changed event of the tree view
        ///// </summary>
        //private DelegateCommand _camPanelSelectionChangedCommand;
        ///// <summary>
        ///// Handles selection/deselection of all cameras
        ///// </summary>
        //private DelegateCommand _camPanelSelectedAllCommand;

        ///// <summary>
        ///// Menu bar source
        ///// </summary>
        //private ObservableCollection<MenuItemViewModel> _menuBarItems
        //    = new ObservableCollection<MenuItemViewModel>();

        ///// <summary>
        ///// Connected cams. This one is for work.
        ///// </summary>
        //private ObservableConcurrentDictionary<string, ConnectedCameraViewModel> _connectedCams
        //    = new ObservableConcurrentDictionary<string, ConnectedCameraViewModel>();

        ///// <summary>
        ///// Tree represencation of connected cams (grouped by location)
        ///// </summary>
        //private ObservableCollection<ConnectedCamerasTreeViewModel> _camPanel
        //    = new ObservableCollection<ConnectedCamerasTreeViewModel>();

        ///// <summary>
        ///// Collection of selected cams (checked with checkboxes)
        ///// </summary>
        //private ObservableConcurrentDictionary<string, bool> _camPanelSelectedItems
        //    = new ObservableConcurrentDictionary<string, bool>();

        ///// <summary>
        ///// Updates cam stats
        ///// </summary>
        //private ObservableConcurrentDictionary<string, Dictionary<string, object>> _camRealTimeStats
        //    = new ObservableConcurrentDictionary<string, Dictionary<string, object>>();

        ///// <summary>
        ///// Collection of menu bar items' viewmodels
        ///// </summary>
        //public ObservableCollection<MenuItemViewModel> MenuBarItems
        //{
        //    get => _menuBarItems;
        //    set
        //    {
        //        if (value != _menuBarItems)
        //        {
        //            _menuBarItems = value;
        //            // RaisePropertyChanged();
        //        }
        //    }
        //}

        ///// <summary>
        ///// Collection of all connected cameras.
        ///// </summary>
        //public ObservableConcurrentDictionary<string, ConnectedCameraViewModel> ConnectedCameras
        //{
        //    get => _connectedCams;
        //    set
        //    {
        //        if (value != _connectedCams)
        //        {
        //            _connectedCams = value;
        //            // RaisePropertyChanged();
        //        }
        //    }
        //}

        //// TODO: This is an attempt to properly implement MVVM
        //public ObservableConcurrentDictionary<string, ConnectedCamera> ConnectedCamerasEx { get; } 
        //    = new ObservableConcurrentDictionary<string, ConnectedCamera>();

        ///// <summary>
        ///// Tree representation of connected cameras.
        ///// </summary>
        //public ObservableCollection<ConnectedCamerasTreeViewModel> CameraPanel
        //{
        //    get => _camPanel;
        //    set
        //    {
        //        if (value != _camPanel)
        //        {
        //            _camPanel = value;
        //            // RaisePropertyChanged();
        //        }
        //    }
        //}
        ///// <summary>
        ///// List of cam IDs that are currently selected in the TreeView
        ///// </summary>
        //public ObservableConcurrentDictionary<string, bool> CameraPanelSelectedItems
        //{
        //    get => _camPanelSelectedItems;
        //    set
        //    {
        //        if (value != _camPanelSelectedItems)
        //        {
        //            _camPanelSelectedItems = value;
        //            // RaisePropertyChanged();
        //        }
        //    }
        //}
        //public ObservableConcurrentDictionary<string, Dictionary<string, object>> CameraRealTimeStats
        //{
        //    get => _camRealTimeStats;
        //    set
        //    {
        //        if (value != _camRealTimeStats)
        //        {
        //            _camRealTimeStats = value;
        //            // RaisePropertyChanged();
        //        }
        //    }
        //}

        //public DelegateCommand DisconnectButtonCommand
        //{
        //    get => _disconnectButtonCommand;
        //    set
        //    {
        //        if (value != _disconnectButtonCommand)
        //        {
        //            _disconnectButtonCommand = value;
        //            // RaisePropertyChanged();
        //        }
        //    }
        //}
        //public DelegateCommand CameraPanelSelectionChangedCommand
        //{
        //    get => _camPanelSelectionChangedCommand;
        //    set
        //    {
        //        if (value != _camPanelSelectionChangedCommand)
        //        {
        //            _camPanelSelectionChangedCommand = value;
        //            // RaisePropertyChanged();
        //        }
        //    }
        //}
        //public DelegateCommand CameraPanelSelectedAllCommand
        //{
        //    get => _camPanelSelectedAllCommand;
        //    set
        //    {
        //        if (value != _camPanelSelectedAllCommand)
        //        {
        //            _camPanelSelectedAllCommand = value;
        //            // RaisePropertyChanged();
        //        }
        //    }
        //}

        //public bool? CameraPanelAreAllSelected
        //{
        //    get => _camPanelAreAllSelected;
        //    set
        //    {
        //        if (value != _camPanelAreAllSelected)
        //        {
        //            _camPanelAreAllSelected = value;
        //            // RaisePropertyChanged();
        //        }
        //    }
        //}

        //private void CameraPanelSelectionChangedCommandExecute(object parameter)
        //{
        //    if (parameter is string key)
        //    {
        //        if (CameraPanelSelectedItems.TryGetValue(key, out var value))
        //        {
        //            CameraPanelSelectedItems.TryUpdate(key, !value, value);
        //            var count = CameraPanelSelectedItems.Count;
        //            var selectedCount = CameraPanelSelectedItems.Count(item => item.Value);

        //            if (selectedCount == 0)
        //                CameraPanelAreAllSelected = false;
        //            else if (selectedCount < count)
        //                CameraPanelAreAllSelected = null;
        //            else CameraPanelAreAllSelected = true;

        //        }
        //    }

        //}
        //private bool CanDisconnectButtonCommandExecute(object parameter)
        //    => CameraPanelSelectedItems.Any(item => item.Value);
        ///// <summary>
        ///// Disconnects cams
        ///// </summary>
        ///// <param name="parameter">Command parameter</param>
        //private async void DisconnectButtonCommandExecute(object parameter)
        //{
        //    var workers = new List<Task>();

        //    foreach (var key in CameraPanelSelectedItems.Where(item => item.Value).Select(item => item.Key))
        //    {
        //        var category = Helper.GetCameraHostName(key);
        //        if(!string.IsNullOrWhiteSpace(category))
        //        {
        //            var node = CameraPanel.Where(item => item.Name == category).DefaultIfEmpty(null).FirstOrDefault();

        //            if (node != null)
        //                foreach (var camItem in node.CameraList.Where(item => item.Key == key))
        //                {
        //                    node.CameraList.TryRemove(camItem.Key, out _);
        //                    workers.Add(DisposeCameraAsync(camItem.Key, false));
        //                    if (node.CameraList.IsEmpty)
        //                        CameraPanel.Remove(node);
        //                }
        //        }

        //        CameraPanelSelectedItems.TryRemove(key, out _);
        //    }

        //    if (!CameraPanelSelectedItems.Any(item => item.Value))
        //        CameraPanelAreAllSelected = false;
        //    await Task.WhenAll(workers);
        //}
        //private void CameraPanelSelectedAllCommandExecute(object parameter)
        //{
        //    if (CameraPanelAreAllSelected == null || CameraPanelAreAllSelected == false)
        //    {
        //        foreach (var key in CameraPanelSelectedItems.Keys)
        //        {
        //            if (CameraPanelSelectedItems.TryGetValue(key, out var oldVal))
        //                CameraPanelSelectedItems.TryUpdate(key, true, oldVal);
        //        }
        //        this.RaisePropertyChanged(nameof(CameraPanelSelectedItems));
        //        CameraPanelAreAllSelected = true;
        //    }
        //    else
        //    {
        //        foreach (var key in CameraPanelSelectedItems.Keys)
        //        {
        //            if (CameraPanelSelectedItems.TryGetValue(key, out var oldVal))
        //                CameraPanelSelectedItems.TryUpdate(key, false, oldVal);
        //        }
        //        this.RaisePropertyChanged(nameof(CameraPanelSelectedItems));
        //        CameraPanelAreAllSelected = false;
        //    }
        //}

        //private void CameraSelectionsMade(object e)
        //{

        //    var menus = new[] { "Properties" };
        //    if (e is IEnumerable<KeyValuePair<string, CameraBase>> providedCameras)
        //    {
        //        var inst = providedCameras.ToList();
        //        foreach (var x in inst)
        //        {
        //            var camModel = new ConnectedCamera(x.Value, x.Key);
        //            var ctxMenu = menus.Select(menu =>
        //                new MenuItemViewModel(
        //                    new MenuItemViewModel()
        //                    {
        //                        Header = menu,
        //                        Command = new DelegateCommand(
        //                            camModel.ContextMenuCommandExecute,
        //                            DelegateCommand.CanExecuteAlways)
        //                    }));
        //            camModel.ContextMenu = new MenuCollection(ctxMenu);

        //            // TODO: Dispose & Message if failure
        //            if (ConnectedCamerasEx.TryAdd(x.Key, camModel))
        //            {
        //                 HookCamera(x.Key, x.Value);
        //                _connectedCams.TryAdd(x.Key, new ConnectedCameraViewModel(camModel));
        //            }
        //            else
        //            {
        //                x.Value.Dispose();
        //                MessageBox.Show(
        //                    Properties.Localization.MainWindow_MB_FailedToAddCamera_Message,
        //                    Properties.Localization.MainWindow_MB_FailedToAddCamera_Caption,
        //                    MessageBoxButton.OK,
        //                    MessageBoxImage.Error);
        //            }
        //            // For compatibility

        //            //lock(_connectedCams)
        //            //    ConnectedCameras = new ObservableConcurrentDictionary<string, ConnectedCameraViewModel>(_connectedCams.OrderByDescending(item => item.Value.Camera.ToString()));

        //        }

        //        foreach (var x in inst)
        //            CameraPanelSelectedItems.TryAdd(x.Key, false);

        //        var categories = inst.Select(item => Helper.GetCameraHostName(item.Key))
        //                             .Distinct()
        //                             //.Take(1) //Debug
        //                             .ToArray(); 



        //        foreach (var cat in categories)
        //        {

        //            List<KeyValuePair<string, ConnectedCameraTreeItemViewModel>> vms =
        //                new List<KeyValuePair<string, ConnectedCameraTreeItemViewModel>>(ConnectedCameras.Count);

        //            foreach (var camKey in inst
        //                .Where(item => Helper.GetCameraHostName(item.Key) == cat)
        //                .Select(item => item.Key))
        //            {

        //                var camModel = ConnectedCameras[camKey].Model;

        //                vms.Add(
        //                    new KeyValuePair<string, ConnectedCameraTreeItemViewModel>(
        //                        camKey,
        //                        new ConnectedCameraTreeItemViewModel(camModel)));
        //            }

        //            ConnectedCamerasTreeViewModel conCamTvm;
        //            if ((conCamTvm = CameraPanel.FirstOrDefault(pnl => pnl.Name == cat)) != null)
        //            {
        //                foreach (var item in vms)
        //                    conCamTvm.Model.CameraList.TryAdd(item.Key, item.Value);
        //            }
        //            else
        //            {
        //                CameraPanel.Add(new ConnectedCamerasTreeViewModel(new ConnectedCamerasTreeModel()
        //                {
        //                    Name = cat,
        //                    CameraList =
        //                        new ObservableConcurrentDictionary<string, ConnectedCameraTreeItemViewModel>(vms)
        //                }));
        //            }
        //        }
        //    }

        //    // TODO : Check here
        //    //_canConnect = true;
        //    //ConnectButtonCommand?.OnCanExecuteChanged();

        //}

        /// <summary>
        /// Hooks events of the connected camera instance.
        /// </summary>
        /// <param name="cam">Camera instance.</param>
        private void HookEvents(CameraBase cam)
        {
            //            if (cam != null)
            //            {
            //                cam.PropertyChanged += Camera_PropertyChanged;
            //                cam.TemperatureStatusChecked += Camera_TemperatureStatusChecked;
            //                cam.NewImageReceived += Cam_NewImageReceived;
            //#if DEBUG
            //                DebugTracer.AddTarget(cam, cam.ToString());
            //#endif
            //            }
        }



        /// <summary>
        /// Handles <see cref="CameraBase.PropertyChanged"/> event of the <see cref="CameraBase"/>.
        /// </summary>
        /// <param name="sender"><see cref="CameraBase"/> sender.</param>
        /// <param name="e">Event arguments.</param>
        private void Camera_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            //if (sender is CameraBase cam)
            //{
            //    var key = GetCameraKey(cam);
            //    //Helper.WriteLog($"[{key}]: {e.PropertyName}");

            //}
        }

        private void Cam_NewImageReceived(object sender, NewImageReceivedEventArgs e)
        {
            //if (sender is CameraBase cam)
            //{
            //    var key = GetCameraKey(cam);
            //    Helper.WriteLog($"[{key}]: {e.First} unclaimed images acquired.");
            //}
        }


        //private string GetCameraKey(CameraBase instance)
        //    => ConnectedCameras.FirstOrDefault(item => Equals(item.Value.Camera, instance)).Key;
        //private void DispatcherTimerTickHandler(object sender, EventArgs e)
        //    =>   this.RaisePropertyChanged(nameof(CameraRealTimeStats));


        private readonly string[] _remoteLocations
            = UiSettingsProvider.Settings.GetArray<string>("RemoteLocations")
              ?? new string[0];

        private DipolClient[] _remoteClients;

        private readonly SourceCache<(string Id, CameraBase Camera), string> _connectedCameras;


        public DescendantProvider ProgressBarProvider { get; private set; }
        public DescendantProvider AvailableCamerasProvider { get; private set; }

        
        public bool CanConnect { [ObservableAsProperty] get; }

        public SourceList<string> SelectedDevices { get; }
        public IObservableCache<(string Id, CameraBase Camera), string> ConnectedCameras { get; private set; }
        public IObservableCache<(string Id, CameraTab Tab), string> CameraTabs { get; private set; }

        public ReactiveCommand<Unit, Unit> WindowLoadedCommand { get; private set; }

        public ReactiveCommand<object, AvailableCamerasModel> ConnectButtonCommand { get; private set; }
        public ReactiveCommand<Unit, Unit> DisconnectButtonCommand { get; private set; }
        public ReactiveCommand<Unit, Unit> SelectAllCamerasCommand { get; private set; }
        public ReactiveCommand<string, Unit> SelectCameraCommand { get; private set; }
        public ReactiveCommand<string, Unit> ContextMenuCommand { get; private set; }

        public DipolMainWindow()
        {
            _connectedCameras = new SourceCache<(string Id, CameraBase Camera), string>(x => x.Id)
                .DisposeWith(_subscriptions);

            SelectedDevices = new SourceList<string>()
                .DisposeWith(_subscriptions);

            InitializeCommands();
            HookObservables();
            HookValidators();
        }

        private void InitializeCommands()
        {

            ContextMenuCommand =
                ReactiveCommand.CreateFromObservable<string, Unit>(
                                   x => Observable.FromAsync(_ => ContextMenuCommandExecuteAsync(x)),
                                   _connectedCameras.CountChanged.Select(x => x != 0)
                                                   .DistinctUntilChanged()
                                                   .ObserveOnUi())
                               .DisposeWith(_subscriptions);

            SelectCameraCommand =
                ReactiveCommand.Create<string>(
                                   SelectCameraCommandExecute,
                                   _connectedCameras.CountChanged.Select(x => x != 0)
                                                   .DistinctUntilChanged()
                                                   .ObserveOnUi())
                               .DisposeWith(_subscriptions);

            WindowLoadedCommand =
                ReactiveCommand.Create<Unit, Unit>(_ => Unit.Default)
                               .DisposeWith(_subscriptions);

            ConnectButtonCommand =
                ReactiveCommand.Create<object, AvailableCamerasModel>(
                                   _ =>
                                   {
                                       var camQueryModel = new AvailableCamerasModel(
                                           _remoteClients);

                                       return camQueryModel;
                                   },
                                   this.WhenAnyPropertyChanged(nameof(CanConnect))
                                       .Select(x => x.CanConnect)
                                       .DistinctUntilChanged()
                                       .ObserveOnUi())
                               .DisposeWith(_subscriptions);

            DisconnectButtonCommand =
                ReactiveCommand.Create(
                                   DisconnectButtonCommandExecute,
                                   SelectedDevices.CountChanged.Select(x => x != 0)
                                                  .DistinctUntilChanged()
                                                  .ObserveOnUi())
                               .DisposeWith(_subscriptions);

            SelectAllCamerasCommand =
                ReactiveCommand.Create(
                                   SelectAllCamerasCommandExecute,
                                   _connectedCameras.CountChanged.Select(x => x != 0)
                                                   .DistinctUntilChanged()
                                                   .ObserveOnUi())
                               .DisposeWith(_subscriptions);

            ProgressBarProvider = new DescendantProvider(
                    ReactiveCommand.CreateFromTask<object, ReactiveObjectEx>(
                        _ => Task.Run<ReactiveObjectEx>(() =>
                            new ProgressBar()
                            {
                                Minimum = 0,
                                Maximum = 1,
                                Value = 0,
                                IsIndeterminate = true,
                                CanAbort = false,
                                BarTitle = "Connecting to remote locations..."
                            })),
                    ReactiveCommand.Create<Unit>(_ => { }),
                    ReactiveCommand.Create<Unit>(_ => { }),
                    ReactiveCommand.Create<ReactiveObjectEx>(x => x.Dispose()))
                .DisposeWith(_subscriptions);

            AvailableCamerasProvider = new DescendantProvider(
                ReactiveCommand.Create<object, ReactiveObjectEx>(x => (ReactiveObjectEx)x), 
                ReactiveCommand.Create<Unit>(_ => { }),
                null, 
                ReactiveCommand.CreateFromTask<ReactiveObjectEx>(async x =>
                    {
                        await ReceiveConnectedCameras((AvailableCamerasModel) x).ExpectCancellation();
                        x.Dispose();
                    }))
                .DisposeWith(_subscriptions);
        }

        private void HookObservables()
        {
            Observable.Merge(
                          ProgressBarProvider.ViewRequested.Select(_ => false),
                          ProgressBarProvider.ViewFinished.Select(_ => true),
                          AvailableCamerasProvider.ViewRequested.Select(_ => false),
                          AvailableCamerasProvider.ViewFinished.Select(_ => true))
                      .ToPropertyEx(this, x => x.CanConnect)
                      .DisposeWith(_subscriptions);


           _connectedCameras.Connect()
                            .DisposeManyEx(async x => await DisposeCamera(x.Camera))
                            .Subscribe()
                            .DisposeWith(_subscriptions);

           ConnectedCameras = _connectedCameras
                              .AsObservableCache()
                              .DisposeWith(_subscriptions);



           CameraTabs = _connectedCameras.Connect()
                                         .Transform(x => (x.Id, Tab: new CameraTab(x.Camera)))
                                         .DisposeManyEx(x => x.Tab?.Dispose())
                                         .AsObservableCache()
                                         .DisposeWith(_subscriptions);

           WindowLoadedCommand
               .InvokeCommand(ProgressBarProvider.ViewRequested)
               .DisposeWith(_subscriptions);

           ProgressBarProvider.ViewRequested.Select(async x =>
                              {
                                  await InitializeRemoteSessionsAsync((ProgressBar) x);
                                  return Unit.Default;
                              })
                              .CombineLatest(ProgressBarProvider.WindowShown,
                                  (x, y) => Unit.Default)
                              .Delay(TimeSpan.Parse(UiSettingsProvider.Settings.Get("PopUpDelay", "00:00:00.750")))
                              .InvokeCommand(ProgressBarProvider.ClosingRequested)
                              .DisposeWith(_subscriptions);

           ConnectButtonCommand.InvokeCommand(AvailableCamerasProvider.ViewRequested as ICommand)
                               .DisposeWith(_subscriptions);

           AvailableCamerasProvider.WindowShown.WithLatestFrom(
                                       AvailableCamerasProvider.ViewRequested,
                                       (x, y) => y)
                                   .Subscribe(async x =>
                                       await QueryCamerasAsync((AvailableCamerasModel) x)
                                           .ExpectCancellation())
                                   .DisposeWith(_subscriptions);

        }

        private void SelectAllCamerasCommandExecute()
        {
            if(SelectedDevices.Count < ConnectedCameras.Count)
                SelectedDevices.Edit(context =>
                {
                    context.Clear();
                    context.AddRange(ConnectedCameras.Keys);
                });
            else
                SelectedDevices.Clear();
        }

        private void SelectCameraCommandExecute(string param)
        {
            SelectedDevices.Edit(context =>
            {
                if (context.Contains(param))
                    context.Remove(param);
                else
                    context.Add(param);
            });
        }

        private async Task ContextMenuCommandExecuteAsync(string param)
        {
            var result = await Helper.RunNoMarshall(() => ConnectedCameras.Lookup(param));
            if (result.HasValue)
            {
                var vm = await Helper.RunNoMarshall(() => new CameraPropertiesViewModel(result.Value.Camera));
                var view = Helper.ExecuteOnUi(() => new CameraPropertiesView()
                        {WindowStartupLocation = WindowStartupLocation.CenterScreen}
                    .WithDataContext(vm));

                Helper.ExecuteOnUi(view.ShowDialog);

                await Helper.RunNoMarshall(vm.Dispose);
            }
        }

        private async Task InitializeRemoteSessionsAsync(ProgressBar pb)
        {
            var connectedClients = new List<DipolClient>(_remoteLocations.Length);

            void ConnectToClient() => Parallel.For(0, _remoteLocations.Length, (i) =>
            {
                try
                {
                    var client = new DipolClient(_remoteLocations[i],
                        TimeSpan.Parse(UiSettingsProvider.Settings.Get("RemoteOpenTimeout", "00:00:30")),
                        TimeSpan.Parse(UiSettingsProvider.Settings.Get("RemoteSendTimeout", "00:00:30")),
                        TimeSpan.Parse(UiSettingsProvider.Settings.Get("RemoteOperationTimeout", "00:00:30")),
                        TimeSpan.Parse(UiSettingsProvider.Settings.Get("RemoteCloseTimeout", "00:00:30")));
                    client.Connect();
                    lock(connectedClients)
                        connectedClients.Add(client);
                }
                catch (System.ServiceModel.EndpointNotFoundException endpointException)
                {
                    Helper.WriteLog(endpointException.Message);
                    Helper.ExecuteOnUi(() => MessageBox.Show(
                        endpointException.Message,
                        Properties.Localization.RemoteConnection_UnreachableHostTitle,
                        MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK));
                }
            });

            await Task.Factory.StartNew(ConnectToClient).ConfigureAwait(false);

            _remoteClients = connectedClients.ToArray();
            pb.BarComment = $"Connected to {_remoteClients.Length} out of {_remoteLocations.Length} locations.";
        }
        
        private async Task ReceiveConnectedCameras(AvailableCamerasModel model)
        {
            var cams = model.RetrieveSelectedDevices();

            if (cams.Count > 0)
                _connectedCameras.Edit(context =>
                {
                    context.AddOrUpdate(cams);
                });

            await PrepareCamerasAsync(cams.Select(x => x.Camera));
        }

        private void DisconnectButtonCommandExecute()
        {
            foreach (var id in SelectedDevices.Items.ToList())
            {
                SelectedDevices.Remove(id);
                _connectedCameras.RemoveKey(id);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
                if (disposing)
                {
                    _connectedCameras.Clear();

                    if (!(_remoteClients is null))
                        Parallel.ForEach(_remoteClients, (client) =>
                        {
                            client?.Disconnect();
                            client?.Dispose();
                        });
                }
            base.Dispose(disposing);
        }

        private static async Task<AvailableCamerasModel> QueryCamerasAsync(AvailableCamerasModel model)
        {
            (await Helper.RunNoMarshall(() => model
                                             .QueryCamerasCommand.Execute()))
                    .Subscribe(_ => { }, () => { });

            return model;
        }
        private static async Task PrepareCamerasAsync(IEnumerable<CameraBase> cams)
        {
            await Helper.RunNoMarshall(() =>
            {
                foreach (var cam in cams)
                {
                    if (cam.Capabilities.GetFunctions.HasFlag(GetFunction.Temperature))
                        cam.TemperatureMonitor(Switch.Enabled, 500);
                }
            });
        }
        private static async Task DisposeCamera(CameraBase cam)
            => await Helper.RunNoMarshall(() =>
            {
                cam?.CoolerControl(Switch.Disabled);
                cam?.TemperatureMonitor(Switch.Disabled);
                cam?.Dispose();
            });
    }
}
