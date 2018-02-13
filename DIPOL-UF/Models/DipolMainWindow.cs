using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Linq;

using DIPOL_UF.ViewModels;

using ANDOR_CS.Classes;
using ANDOR_CS.Enums;
using ANDOR_CS.Events;

using DIPOL_Remote.Classes;

using MenuCollection = System.Collections.ObjectModel.ObservableCollection<DIPOL_UF.ViewModels.MenuItemViewModel>;
using DelegateCommand = DIPOL_UF.Commands.DelegateCommand;

using static DIPOL_UF.DIPOL_UF_App;

namespace DIPOL_UF.Models
{
    class DipolMainWindow : ObservableObject, IDisposable
    {
        private readonly DispatcherTimer _UIStatusUpdateTimer;

        private readonly DispatcherTimer _TestTimer;

        private bool? _camPanelAreAllSelected = false;
        private bool _canConnect;
        private bool _isDisposed;
        private readonly string[] _remoteLocations
            = Settings
                  .GetValueOrNullSafe<object[]>("RemoteLocations")
                  ?.Cast<string>()
                  .ToArray()
            ?? new string[0];
        private DipolClient[] _remoteClients;

        /// <summary>
        /// Connect button command
        /// </summary>
        private DelegateCommand _connectButtonCommand;
        /// <summary>
        /// Disconnect button command
        /// </summary>
        private DelegateCommand _disconnectButtonCommand;
        /// <summary>
        /// Handles selection changed event of the tree view
        /// </summary>
        private DelegateCommand _camPanelSelectionChangedCommand;
        /// <summary>
        /// Handles selection/deselection of all cameras
        /// </summary>
        private DelegateCommand _camPanelSelectedAllCommand;

        /// <summary>
        /// Menu bar source
        /// </summary>
        private ObservableCollection<MenuItemViewModel> _menuBarItems
            = new ObservableCollection<MenuItemViewModel>();

        /// <summary>
        /// Connected cams. This one is for work.
        /// </summary>
        private ObservableConcurrentDictionary<string, ConnectedCameraViewModel> _connectedCams
            = new ObservableConcurrentDictionary<string, ConnectedCameraViewModel>();

        /// <summary>
        /// Tree represencation of connected cams (grouped by location)
        /// </summary>
        private ObservableCollection<ConnectedCamerasTreeViewModel> _camPanel
            = new ObservableCollection<ConnectedCamerasTreeViewModel>();

        /// <summary>
        /// Collection of selected cams (checked with checkboxes)
        /// </summary>
        private ObservableConcurrentDictionary<string, bool> _camPanelSelectedItems
            = new ObservableConcurrentDictionary<string, bool>();

        /// <summary>
        /// Updates cam stats
        /// </summary>
        private ObservableConcurrentDictionary<string, Dictionary<string, object>> _camRealTimeStats
            = new ObservableConcurrentDictionary<string, Dictionary<string, object>>();

        /// <summary>
        /// Collection of menu bar items' viewmodels
        /// </summary>
        public ObservableCollection<MenuItemViewModel> MenuBarItems
        {
            get => _menuBarItems;
            set
            {
                if (value != _menuBarItems)
                {
                    _menuBarItems = value;
                    RaisePropertyChanged();
                }
            }
        }

        /// <summary>
        /// Collection of all connected cameras.
        /// </summary>
        public ObservableConcurrentDictionary<string, ConnectedCameraViewModel> ConnectedCameras
        {
            get => _connectedCams;
            set
            {
                if (value != _connectedCams)
                {
                    _connectedCams = value;
                    RaisePropertyChanged();
                }
            }
        }

        /// <summary>
        /// Tree representation of connected cameras.
        /// </summary>
        public ObservableCollection<ConnectedCamerasTreeViewModel> CameraPanel
        {
            get => _camPanel;
            set
            {
                if (value != _camPanel)
                {
                    _camPanel = value;
                    RaisePropertyChanged();
                }
            }
        }
        /// <summary>
        /// List of cam IDs that are currently selected in the TreeView
        /// </summary>
        public ObservableConcurrentDictionary<string, bool> CameraPanelSelectedItems
        {
            get => _camPanelSelectedItems;
            set
            {
                if (value != _camPanelSelectedItems)
                {
                    _camPanelSelectedItems = value;
                    RaisePropertyChanged();
                }
            }
        }
        public ObservableConcurrentDictionary<string, Dictionary<string, object>> CameraRealTimeStats
        {
            get => _camRealTimeStats;
            set
            {
                if (value != _camRealTimeStats)
                {
                    _camRealTimeStats = value;
                    RaisePropertyChanged();
                }
            }
        }

        public DelegateCommand ConnectButtonCommand
        {
            get => _connectButtonCommand;
            set
            {
                if (value != _connectButtonCommand)
                {
                    _connectButtonCommand = value;
                    RaisePropertyChanged();
                }
            }
        }
        public DelegateCommand DisconnectButtonCommand
        {
            get => _disconnectButtonCommand;
            set
            {
                if (value != _disconnectButtonCommand)
                {
                    _disconnectButtonCommand = value;
                    RaisePropertyChanged();
                }
            }
        }
        public DelegateCommand CameraPanelSelectionChangedCommand
        {
            get => _camPanelSelectionChangedCommand;
            set
            {
                if (value != _camPanelSelectionChangedCommand)
                {
                    _camPanelSelectionChangedCommand = value;
                    RaisePropertyChanged();
                }
            }
        }
        public DelegateCommand CameraPanelSelectedAllCommand
        {
            get => _camPanelSelectedAllCommand;
            set
            {
                if (value != _camPanelSelectedAllCommand)
                {
                    _camPanelSelectedAllCommand = value;
                    RaisePropertyChanged();
                }
            }
        }

        public bool? CameraPanelAreAllSelected
        {
            get => _camPanelAreAllSelected;
            set
            {
                if (value != _camPanelAreAllSelected)
                {
                    _camPanelAreAllSelected = value;
                    RaisePropertyChanged();
                }
            }
        }
        public bool IsDisposed
        {
            get => _isDisposed;
            private set
            {
                if (value != _isDisposed)
                {
                    _isDisposed = value;
                    RaisePropertyChanged();
                }
            }
        }
        public bool CanConnect
        {
            get => _canConnect;
            set
            {
                if (value != _canConnect)
                {
                    _canConnect = value;
                    RaisePropertyChanged();
                }
            }
        }

        public DipolMainWindow()
        {
            InitializeMenu();
            InitializeCommands();
            InitializeRemoteSessions();

            _UIStatusUpdateTimer = new DispatcherTimer(
                TimeSpan.FromMilliseconds(Settings.GetValueOrNullSafe("UICamStatusUpdateDelay", 1000)),
                DispatcherPriority.DataBind,
                DispatcherTimerTickHandler,
                Application.Current.Dispatcher
            );

            _TestTimer = new DispatcherTimer()
            {
                Interval = new TimeSpan(0, 3, 30),
                IsEnabled = false
            };

            _TestTimer.Tick += (sender, e) =>
            {
                foreach (var cam in ConnectedCameras)
                {
                    cam.Value.Model.Camera.SetTemperature(20);
                    cam.Value.Model.Camera.CoolerControl(Switch.Disabled);
                }

                _TestTimer.Stop();
            };

            _TestTimer.Start();
        }
                                                        

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool diposing)
        {
            if (diposing)
            {
                _UIStatusUpdateTimer.Stop();
                Task[] pool = new Task[_connectedCams.Count];
                int taskInd = 0;

                foreach (var cam in _connectedCams)
                    pool[taskInd++] = DisposeCamera(cam.Key);

                Task.WaitAll(pool);

                Parallel.ForEach(_remoteClients, (client) =>
                {
                    client?.Disconnect();
                    client?.Dispose();
                });

                IsDisposed = true;
            }
        }

        private void InitializeMenu()
        {

        }
        private void InitializeCommands()
        {
            ConnectButtonCommand = new DelegateCommand(
                ConnectButtonCommandExecute,
                (param) => CanConnect);
            PropertyChanged += (sender, e) =>
                {
                    if (e.PropertyName == nameof(CanConnect))
                    {
                        if (Application.Current.Dispatcher.IsAvailable())
                            Application.Current.Dispatcher.Invoke(ConnectButtonCommand.OnCanExecuteChanged);
                        else
                            ConnectButtonCommand.OnCanExecuteChanged();
                    }   
                };
                    
            DisconnectButtonCommand = new DelegateCommand(
                DisconnectButtonCommandExecute,
                CanDisconnectButtonCommandExecute);
            _connectedCams.CollectionChanged += (sender, e) => DisconnectButtonCommand.OnCanExecuteChanged();
            CameraPanelSelectedItems.CollectionChanged += (sender, e) 
                => DisconnectButtonCommand.OnCanExecuteChanged();
            
            CameraPanelSelectionChangedCommand = new DelegateCommand(
                CameraPanelSelectionChangedCommandExecute,
                DelegateCommand.CanExecuteAlways);


            CameraPanelSelectedAllCommand = new DelegateCommand(
                CameraPanelSelectedAllCommandExecute,
                DelegateCommand.CanExecuteAlways);

        }
        private void InitializeRemoteSessions()
        {
            var pb = new ProgressBar()
            {
                IsIndeterminate = true,
                CanAbort = false,
                BarTitle = "Connecting to remote locations..."
            };

            var pbWindow = new Views.ProgressWindow(new ProgressBarViewModel(pb));

            pbWindow.Show();

            var connectedClients = new List<DipolClient>(_remoteLocations.Length);

            void ConnectToClient() => Parallel.For(0, _remoteLocations.Length, (i) =>
            {
                try
                {
                    var client = new DipolClient(_remoteLocations[i], 
                        TimeSpan.Parse(Settings.GetValueOrNullSafe("RemoteOpenTimeout", "00:00:30")), 
                        TimeSpan.Parse(Settings.GetValueOrNullSafe("RemoteSendTimeout", "00:05:00")), 
                        TimeSpan.Parse(Settings.GetValueOrNullSafe("RemoteOperationTimeout", "00:00:45")), 
                        TimeSpan.Parse(Settings.GetValueOrNullSafe("RemoteCloseTimeout", "00:00:45")));
                    client.Connect();
                    connectedClients.Add(client);
                }
                catch (System.ServiceModel.EndpointNotFoundException enfe)
                {
                    Helper.WriteLog(enfe.Message);
                    if (Application.Current.Dispatcher.IsAvailable())
                        Application.Current.Dispatcher.Invoke(() => 
                            MessageBox.Show(pbWindow, 
                                enfe.Message, 
                                "Host not found or unreachable", 
                                MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK));
                    else
                        MessageBox.Show(enfe.Message, "Host not found or unreachable", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
                }
            });

            var connect = Task.Factory.StartNew(ConnectToClient);

            connect.ContinueWith((task) =>
            {
                _remoteClients = connectedClients.ToArray();
                pb.BarComment = $"Connected to {_remoteClients.Length} out of {_remoteLocations.Length} locations.";

                Task.Delay(TimeSpan.Parse(Settings.GetValueOrNullSafe("PopUpDelay", "00:00:00.750"))).Wait();

                if(Application.Current.Dispatcher.IsAvailable())
                    Application.Current.Dispatcher.Invoke(pbWindow.Close);

                CanConnect = true;

            });


        }
       
        private void CameraPanelSelectionChangedCommandExecute(object parameter)
        {
            if (parameter is string key)
            {
                if (CameraPanelSelectedItems.TryGetValue(key, out var value))
                {
                    CameraPanelSelectedItems.TryUpdate(key, !value, value);
                    var count = CameraPanelSelectedItems.Count;
                    var selectedCount = CameraPanelSelectedItems.Count(item => item.Value);

                    if (selectedCount == 0)
                        CameraPanelAreAllSelected = false;
                    else if (selectedCount < count)
                        CameraPanelAreAllSelected = null;
                    else CameraPanelAreAllSelected = true;

                }
            }
            
        }
        private bool CanDisconnectButtonCommandExecute(object parameter)
            => CameraPanelSelectedItems.Any(item => item.Value);
        private void ConnectButtonCommandExecute(object parameter)
        {
            var camQueryModel = new AvailableCamerasModel(_remoteClients);
            var viewModel = new AvailableCamerasViewModel(camQueryModel);
            var wind = new Views.AvailableCameraView(viewModel);
            if (parameter is Window owner)
                wind.Owner = owner;
            CanConnect = false;
            wind.Show();

            camQueryModel.CameraSelectionsMade += CameraSelectionsMade;           

        }
        /// <summary>
        /// Disconnects cams
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        private async void DisconnectButtonCommandExecute(object parameter)
        {
            var workers = new List<Task>();

            foreach (var key in CameraPanelSelectedItems.Where(item => item.Value).Select(item => item.Key))
            {
                var category = Helper.GetCameraHostName(key);
                if(!string.IsNullOrWhiteSpace(category))
                {
                    var node = CameraPanel.Where(item => item.Name == category).DefaultIfEmpty(null).FirstOrDefault();

                    if (node != null)
                        foreach (var camItem in node.CameraList.Where(item => item.Key == key))
                        {
                            node.CameraList.TryRemove(camItem.Key, out _);
                            workers.Add(DisposeCamera(camItem.Key, false));
                            if (node.CameraList.IsEmpty)
                                CameraPanel.Remove(node);
                        }
                }

                CameraPanelSelectedItems.TryRemove(key, out _);
            }

            if (!CameraPanelSelectedItems.Any(item => item.Value))
                CameraPanelAreAllSelected = false;
            await Task.WhenAll(workers);
        }
        private void CameraPanelSelectedAllCommandExecute(object parameter)
        {
            if (CameraPanelAreAllSelected == null || CameraPanelAreAllSelected == false)
            {
                foreach (var key in CameraPanelSelectedItems.Keys)
                {
                    if (CameraPanelSelectedItems.TryGetValue(key, out var oldVal))
                        CameraPanelSelectedItems.TryUpdate(key, true, oldVal);
                }
                RaisePropertyChanged(nameof(CameraPanelSelectedItems));
                CameraPanelAreAllSelected = true;
            }
            else
            {
                foreach (var key in CameraPanelSelectedItems.Keys)
                {
                    if (CameraPanelSelectedItems.TryGetValue(key, out var oldVal))
                        CameraPanelSelectedItems.TryUpdate(key, false, oldVal);
                }
                RaisePropertyChanged(nameof(CameraPanelSelectedItems));
                CameraPanelAreAllSelected = false;
            }
        }

        private void CameraSelectionsMade(object e)
        {
           
            if (e is IEnumerable<KeyValuePair<string, CameraBase>> providedCameras)
            {
                var inst = providedCameras.ToList();
                foreach (var x in inst)
                    if (ConnectedCameras.TryAdd(x.Key,
                        new ConnectedCameraViewModel(new ConnectedCamera(x.Value, x.Key))))
                        HookCamera(x.Key, x.Value);

                foreach (var x in inst)
                    CameraPanelSelectedItems.TryAdd(x.Key, false);

                var categories = inst
                                      .Select(item => Helper.GetCameraHostName(item.Key))
                                      .Distinct()
                                      .ToArray();

                foreach (var cat in categories)
                {
                    CameraPanel.Add(new ConnectedCamerasTreeViewModel(new ConnectedCamerasTreeModel()
                    {
                        Name = cat,
                        CameraList = new ObservableConcurrentDictionary<string, ConnectedCameraTreeItemViewModel>(
                            inst
                                .Where(item => Helper.GetCameraHostName(item.Key) == cat)
                                .Select(item => new KeyValuePair<string, ConnectedCameraTreeItemViewModel>(
                                    item.Key,
                                    new ConnectedCameraTreeItemViewModel(
                                        new ConnectedCameraTreeItemModel(item.Value)
                                        {
                                            ContextMenu = new MenuCollection()
                                            {
                                                new MenuItemViewModel(
                                                    new MenuItemModel()
                                                    {
                                                        Header = "Properties",
                                                        Command = new DelegateCommand(
                                                            (param) => ContextMenuCommandExecute("Properties", param),
                                                            DelegateCommand.CanExecuteAlways)
                                                    })
                                            }
                                        }))))
                    }));
                }
            }

            _canConnect = true;
            ConnectButtonCommand?.OnCanExecuteChanged();

        }

        private static void ContextMenuCommandExecute(string command, object param)
        {
            if(command == "Properties")
                if (param is DependencyObject obj)
                {
                    var parentObj = Helper.FindParentOfType<System.Windows.Controls.StackPanel>(obj);
                    if (parentObj is System.Windows.Controls.StackPanel parent)
                        if (parent.DataContext is KeyValuePair<string,ConnectedCameraTreeItemViewModel> viewmodel)
                        {
                            var vm = new CameraPropertiesViewModel(viewmodel.Value.Camera);
                            var window = new Views.CameraPropertiesView(vm)
                            {
                                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                                Owner = Helper.FindParentOfType<Window>(parent)
                            };
                            window.Show();
                        }
                }
        }
        /// <summary>
        /// Hooks events of the connected camera instance.
        /// </summary>
        /// <param name="cam">Camera instance.</param>
        private void HookEvents(CameraBase cam)
        {
            if (cam != null)
            {
                cam.PropertyChanged += Camera_PropertyChanged;
                cam.TemperatureStatusChecked += Camera_TemperatureStatusChecked;
                cam.NewImageReceived += Cam_NewImageReceived;
            }
        }

        /// <summary>
        /// Attaches event handlers to each cam to monitor status and progress.
        /// </summary>
        /// <param name="key">CameraID.</param>
        /// <param name="cam">Camera instance.</param>
        private void HookCamera(string key, CameraBase cam)
        {
            if (_camRealTimeStats.ContainsKey(key))
                _camRealTimeStats.TryRemove(key, out _);

            _camRealTimeStats.TryAdd(key, new Dictionary<string, object>());

            // Hooks events of the current camera
            HookEvents(cam);

            if (cam.Capabilities.GetFunctions.HasFlag(GetFunction.Temperature))
            {
                cam.TemperatureMonitor(Switch.Enabled,
                     Settings.GetValueOrNullSafe("UICamStatusUpdateDelay", 500));
                Camera_PropertyChanged(cam, new System.ComponentModel.PropertyChangedEventArgs(nameof(cam.FanMode)));
            }
        }

        /// <summary>
        /// Handles <see cref="CameraBase.PropertyChanged"/> event of the <see cref="CameraBase"/>.
        /// </summary>
        /// <param name="sender"><see cref="CameraBase"/> sender.</param>
        /// <param name="e">Event arguments.</param>
        private void Camera_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (sender is CameraBase cam)
            {
                var key = GetCameraKey(cam);
                Helper.WriteLog($"[{key}]: {e.PropertyName}");
                             
            }
        }
        private void Cam_NewImageReceived(object sender, NewImageReceivedEventArgs e)
        {
            if (sender is CameraBase cam)
            {
                var key = GetCameraKey(cam);
                Helper.WriteLog($"[{key}]: {e.First} unclaimed images acquired.");
            }
        }
        /// <summary>
        /// Handles <see cref="CameraBase.TemperatureStatusChecked"/> event of the <see cref="CameraBase"/>.
        /// </summary>
        /// <param name="sender"><see cref="CameraBase"/> sender.</param>
        /// <param name="e">Event arguments.</param>
        private void Camera_TemperatureStatusChecked(object sender, TemperatureStatusEventArgs e)
        {
            if (sender is CameraBase cam)
            {
                var key = GetCameraKey(cam);
                if (key != null && CameraRealTimeStats.ContainsKey(key))
                {
                    CameraRealTimeStats[key]["Temp"] = e.Temperature;
                    CameraRealTimeStats[key]["TempStatus"] = e.Status;
                }
            }
        }

        private string GetCameraKey(CameraBase instance)
            => ConnectedCameras.FirstOrDefault(item => Equals(item.Value.Camera, instance)).Key;
        private void DispatcherTimerTickHandler(object sener, EventArgs e)
            =>   RaisePropertyChanged(nameof(CameraRealTimeStats));


        private async Task DisposeCamera(string camID, bool removeSelection = true)
        {

            _connectedCams.TryRemove(camID, out var camInstance);
            if (removeSelection)
                _camPanelSelectedItems.TryRemove(camID, out _);
            CameraRealTimeStats.TryRemove(camID, out _);

            await Task.Run(() =>
                {
                    if (camInstance?.Model?.Camera != null)
                    {
                        camInstance.Model.Camera.CoolerControl(Switch.Disabled);
                        camInstance.Model.Camera.TemperatureMonitor(Switch.Disabled);
                        camInstance.Model.Camera.Dispose();
                    }
                });
           
        }
    }
}
