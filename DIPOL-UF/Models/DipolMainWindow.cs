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
        private DispatcherTimer _UIStatusUpdateTimer = null;

        private DispatcherTimer _TestTimer = null;

        private bool? cameraTreeViewSelectedAll = false;
        private bool canEnterSessionManager = true;
        private bool canConnect = false;
        private bool isDisposed = false;
        private string[] remoteLocations
            = Settings.GetValueOrNullSafe<object[]>("RemoteLocations")?.Cast<String>()?.ToArray()
            ?? new string[0];
        private DipolClient[] remoteClients = null;

        /// <summary>
        /// Connect button command
        /// </summary>
        private DelegateCommand connectButtonCommand;
        /// <summary>
        /// Disconnect button command
        /// </summary>
        private DelegateCommand disconnectButtonCommand;
        /// <summary>
        /// Handles selection changed event of the tree view
        /// </summary>
        private DelegateCommand cameraTreeViewSelectionChangedCommand;
        private DelegateCommand cameraTreeViewSelectAllCommand;

        /// <summary>
        /// Menu bar source
        /// </summary>
        private ObservableCollection<MenuItemViewModel> menuBarItems
            = new ObservableCollection<MenuItemViewModel>();

        /// <summary>
        /// Connected cameras. This one is for work.
        /// </summary>
        private ObservableConcurrentDictionary<string, CameraBase> connectedCameras
            = new ObservableConcurrentDictionary<string, CameraBase>();

        /// <summary>
        /// Tree represencation of connected cameras (grouped by location)
        /// </summary>
        private ObservableCollection<ConnectedCamerasTreeViewModel> cameraTreeRepresentation
            = new ObservableCollection<ConnectedCamerasTreeViewModel>();

        /// <summary>
        /// Collection of selected cameras (checked with checkboxes)
        /// </summary>
        private ObservableConcurrentDictionary<string, bool> cameraTreeViewSelectedItems
            = new ObservableConcurrentDictionary<string, bool>();

        /// <summary>
        /// Updates camera stats
        /// </summary>
        private ObservableConcurrentDictionary<string, Dictionary<string, object>> cameraRealTimeStats
            = new ObservableConcurrentDictionary<string, Dictionary<string, object>>();

        public ObservableCollection<MenuItemViewModel> MenuBarItems
        {
            get => menuBarItems;
            set
            {
                if (value != menuBarItems)
                {
                    menuBarItems = value;
                    RaisePropertyChanged();
                }
            }
        }
        public ObservableConcurrentDictionary<string, CameraBase> ConnectedCameras
        {
            get => connectedCameras;
            set
            {
                if (value != connectedCameras)
                {
                    connectedCameras = value;
                    RaisePropertyChanged();
                }
            }
        }
        public ObservableCollection<ConnectedCamerasTreeViewModel> CameraTreeRepresentation
        {
            get => cameraTreeRepresentation;
            set
            {
                if (value != cameraTreeRepresentation)
                {
                    cameraTreeRepresentation = value;
                    RaisePropertyChanged();
                }
            }
        }
        /// <summary>
        /// List of camera IDs that are currently selected in the TreeView
        /// </summary>
        public ObservableConcurrentDictionary<string, bool> CameraTreeViewSelectedItems
        {
            get => cameraTreeViewSelectedItems;
            set
            {
                if (value != cameraTreeViewSelectedItems)
                {
                    cameraTreeViewSelectedItems = value;
                    RaisePropertyChanged();
                }
            }
        }
        public ObservableConcurrentDictionary<string, Dictionary<string, object>> CameraRealTimeStats
        {
            get => cameraRealTimeStats;
            set
            {
                if (value != cameraRealTimeStats)
                {
                    cameraRealTimeStats = value;
                    RaisePropertyChanged();
                }
            }
        }

        public DelegateCommand ConnectButtonCommand
        {
            get => connectButtonCommand;
            set
            {
                if (value != connectButtonCommand)
                {
                    connectButtonCommand = value;
                    RaisePropertyChanged();
                }
            }
        }
        public DelegateCommand DisconnectButtonCommand
        {
            get => disconnectButtonCommand;
            set
            {
                if (value != disconnectButtonCommand)
                {
                    disconnectButtonCommand = value;
                    RaisePropertyChanged();
                }
            }
        }
        public DelegateCommand CameraTreeViewSelectionChangedCommand
        {
            get => cameraTreeViewSelectionChangedCommand;
            set
            {
                if (value != cameraTreeViewSelectionChangedCommand)
                {
                    cameraTreeViewSelectionChangedCommand = value;
                    RaisePropertyChanged();
                }
            }
        }
        public DelegateCommand CameraTreeViewSelectAllCommand
        {
            get => cameraTreeViewSelectAllCommand;
            set
            {
                if (value != cameraTreeViewSelectAllCommand)
                {
                    cameraTreeViewSelectAllCommand = value;
                    RaisePropertyChanged();
                }
            }
        }

        public bool? CameraTreeViewSelectedAll
        {
            get => cameraTreeViewSelectedAll;
            set
            {
                if (value != cameraTreeViewSelectedAll)
                {
                    cameraTreeViewSelectedAll = value;
                    RaisePropertyChanged();
                }
            }
        }
        public bool IsDisposed
        {
            get => isDisposed;
            private set
            {
                if (value != isDisposed)
                {
                    isDisposed = value;
                    RaisePropertyChanged();
                }
            }
        }
        public bool CanConnect
        {
            get => canConnect;
            set
            {
                if (value != canConnect)
                {
                    canConnect = value;
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
                    cam.Value.SetTemperature(20);
                    cam.Value.CoolerControl(Switch.Disabled);
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
                Task[] pool = new Task[connectedCameras.Count];
                int taskInd = 0;

                foreach (var cam in connectedCameras)
                    pool[taskInd++] = DisposeCamera(cam.Key);

                Task.WaitAll(pool);

                Parallel.ForEach(remoteClients, (client) =>
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
                ListAndSelectAvailableCameras,
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
                DisconnectCameras,
                CanDisconnectCameras);
            connectedCameras.CollectionChanged += (sender, e) => DisconnectButtonCommand.OnCanExecuteChanged();
            CameraTreeViewSelectedItems.CollectionChanged += (sender, e) 
                => DisconnectButtonCommand.OnCanExecuteChanged();
            
            CameraTreeViewSelectionChangedCommand = new DelegateCommand(
                CameraTreeViewSelectionChangedCommandHandler,
                DelegateCommand.CanExecuteAlways);


            CameraTreeViewSelectAllCommand = new DelegateCommand(
                CameraTreeViewSelectAllCommandHandler,
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

            var timeOut = TimeSpan.Parse(Settings.GetValueOrNullSafe<string>("RemoteEstablishPBTimeout", "00:00:03"));

            pbWindow.Show();

            var connectedClients = new List<DipolClient>(remoteLocations.Length);
            Task connect = Task.Run(() => 
            Parallel.For(0, remoteLocations.Length, (i) =>
            {
                try
                {
                    var client = new DipolClient(remoteLocations[i],
                        TimeSpan.Parse(Settings.GetValueOrNullSafe<string>("RemoteOpenTimeout", "00:00:30")),
                        TimeSpan.Parse(Settings.GetValueOrNullSafe<string>("RemoteSendTimeout", "00:05:00")),
                        TimeSpan.Parse(Settings.GetValueOrNullSafe<string>("RemoteOperationTimeout", "00:00:45")),
                        TimeSpan.Parse(Settings.GetValueOrNullSafe<string>("RemoteCloseTimeout", "00:00:45")));
                    client.Connect();
                    connectedClients.Add(client);
                    
                }
                catch (System.ServiceModel.EndpointNotFoundException enfe)
                {
                    Helper.WriteLog(enfe.Message);
                    if (Application.Current.Dispatcher.IsAvailable())
                        Application.Current.Dispatcher.Invoke(() => MessageBox.Show(pbWindow, enfe.Message, "Host not found or unreachable",
                            MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK));
                    else
                        MessageBox.Show(enfe.Message, "Host not found or unreachable",
                            MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);

                }
            }));

            connect.ContinueWith((task) =>
            {
                remoteClients = connectedClients.ToArray();
                pb.BarComment = $"Connected to {remoteClients.Length} out of {remoteLocations.Length} locations.";

                Task.Delay(TimeSpan.Parse(Settings.GetValueOrNullSafe<string>("PopUpDelay", "00:00:00.750"))).Wait();

                if(Application.Current.Dispatcher.IsAvailable())
                    Application.Current.Dispatcher.Invoke(pbWindow.Close);

                CanConnect = true;

            });


        }
       
        private void CameraTreeViewSelectionChangedCommandHandler(object parameter)
        {
            if (parameter is string key)
            {
                if (CameraTreeViewSelectedItems.TryGetValue(key, out bool value))
                {
                    CameraTreeViewSelectedItems.TryUpdate(key, !value, value);
                    int count = CameraTreeViewSelectedItems.Count;
                    int selectedCount = CameraTreeViewSelectedItems.Where(item => item.Value).Count();

                    if (selectedCount == 0)
                        CameraTreeViewSelectedAll = false;
                    else if (selectedCount < count)
                        CameraTreeViewSelectedAll = null;
                    else CameraTreeViewSelectedAll = true;

                }
            }
            
        }
        private bool CanDisconnectCameras(object parameter)
            => CameraTreeViewSelectedItems.Any(item => item.Value);
        private void ListAndSelectAvailableCameras(object parameter)
        {
            var cameraQueryModel = new AvailableCamerasModel(remoteClients);
            var viewModel = new AvailableCamerasViewModel(cameraQueryModel);
            var wind = new Views.AvailableCameraView(viewModel);
            if (parameter is Window owner)
                wind.Owner = owner;
            CanConnect = false;
            wind.Show();

            cameraQueryModel.CameraSelectionsMade += CameraSelectionMade;           

        }
        /// <summary>
        /// Disconnects cameras
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        private async void DisconnectCameras(object parameter)
        {
            List<Task> workers = new List<Task>();

            foreach (var key in CameraTreeViewSelectedItems.Where(item => item.Value).Select(item => item.Key))
            {
                string category = Helper.GetCameraHostName(key);
                if(!String.IsNullOrWhiteSpace(category))
                {
                    var node = CameraTreeRepresentation.Where(item => item.Name == category).DefaultIfEmpty(null).FirstOrDefault();

                    foreach (var camItem in node.CameraList.Where(item => item.Key == key))
                    {
                        node.CameraList.TryRemove(camItem.Key, out _);
                        workers.Add(DisposeCamera(camItem.Key, false));
                        if (node.CameraList.IsEmpty)
                            CameraTreeRepresentation.Remove(node);
                    }

                }

                CameraTreeViewSelectedItems.TryRemove(key, out _);
            }

            if (CameraTreeViewSelectedItems.Where(item => item.Value).Count() == 0)
                CameraTreeViewSelectedAll = false;
            await Task.WhenAll(workers);
        }
        private void CameraTreeViewSelectAllCommandHandler(object parameter)
        {
            if (CameraTreeViewSelectedAll == null || CameraTreeViewSelectedAll == false)
            {
                foreach (var key in CameraTreeViewSelectedItems.Keys)
                {
                    if (CameraTreeViewSelectedItems.TryGetValue(key, out bool oldVal))
                        CameraTreeViewSelectedItems.TryUpdate(key, true, oldVal);
                }
                RaisePropertyChanged(nameof(CameraTreeViewSelectedItems));
                CameraTreeViewSelectedAll = true;
            }
            else
            {
                foreach (var key in CameraTreeViewSelectedItems.Keys)
                {
                    if (CameraTreeViewSelectedItems.TryGetValue(key, out bool oldVal))
                        CameraTreeViewSelectedItems.TryUpdate(key, false, oldVal);
                }
                RaisePropertyChanged(nameof(CameraTreeViewSelectedItems));
                CameraTreeViewSelectedAll = false;
            }
        }

        private void CameraSelectionMade(object e)
        {
           
            var providedCameras = e as IEnumerable<KeyValuePair<string, CameraBase>>;

            foreach (var x in providedCameras)
                if (ConnectedCameras.TryAdd(x.Key, x.Value))
                    HookCamera(x.Key, x.Value);

            foreach (var x in providedCameras)
                CameraTreeViewSelectedItems.TryAdd(x.Key, false);

            string[] categories = providedCameras
                .Select(item => Helper.GetCameraHostName(item.Key))
                .Distinct()
                .ToArray();

            foreach (var cat in categories)
            {
                CameraTreeRepresentation.Add(new ConnectedCamerasTreeViewModel(new ConnectedCamerasTreeModel()
                {
                    Name = cat,
                    CameraList = new ObservableConcurrentDictionary<string, ConnectedCameraTreeItemViewModel>(
                        providedCameras
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
                                                    (param) => ContextMenuCommandHandler("Properties", param),
                                                    DelegateCommand.CanExecuteAlways)
                                            })
                                    }
                                }))))
                }));
            }

            canConnect = true;
            ConnectButtonCommand?.OnCanExecuteChanged();

        }

        private void ContextMenuCommandHandler(string command, object param)
        {
            if(command == "Properties")
                if (param is DependencyObject obj)
                {
                    var parentObj = Helper.FindParentOfType<System.Windows.Controls.StackPanel>(obj);
                    if (parentObj is System.Windows.Controls.StackPanel parent)
                        if (parent.DataContext is KeyValuePair<string,ConnectedCameraTreeItemViewModel> viewmodel)
                        {
                            var vm = new CameraPropertiesViewModel(viewmodel.Value.Camera);
                            var window = new Views.CameraPropertiesView(vm);
                            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                            window.Owner = Helper.FindParentOfType<Window>(parent) as Window;
                            window.Show();
                        }
                }
        }
        /// <summary>
        /// Attaches event handlers to each camera to monitor status & progress.
        /// </summary>
        /// <param name="key">CameraID.</param>
        /// <param name="camera">Camera instance.</param>
        private void HookCamera(string key, CameraBase camera)
        {
            if (cameraRealTimeStats.ContainsKey(key))
                cameraRealTimeStats.TryRemove(key, out _);

            cameraRealTimeStats.TryAdd(key, new Dictionary<string, object>());

            if (camera.Capabilities.Features.HasFlag(SDKFeatures.FanControl)) 
                camera.FanControl(FanMode.FullSpeed);

            if (camera.Capabilities.GetFunctions.HasFlag(GetFunction.Temperature))
            { camera.TemperatureMonitor(Switch.Enabled, Settings.GetValueOrNullSafe("UICamStatusUpdateDelay", 500));
                camera.TemperatureStatusChecked += (sender, e) =>
                {
                    cameraRealTimeStats[key]["Temp"] = e.Temperature;
                    cameraRealTimeStats[key]["TempStatus"] = e.Status;

                };
            }
            //camera.SetTemperature(-20);
            //camera.CoolerControl(Switch.Enabled);
        }

        private void DispatcherTimerTickHandler(object sener, EventArgs e)
        {
            RaisePropertyChanged(nameof(CameraRealTimeStats));
        }


        private async Task DisposeCamera(string camID, bool removeSelection = true)
        {

            connectedCameras.TryRemove(camID, out CameraBase camInstance);
            if (removeSelection)
                cameraTreeViewSelectedItems.TryRemove(camID, out _);


            await Task.Run(() =>
                {
                    camInstance?.CoolerControl(Switch.Disabled);
                    camInstance.TemperatureMonitor(Switch.Disabled);
                    camInstance?.Dispose();
                });
           
        }
    }
}
