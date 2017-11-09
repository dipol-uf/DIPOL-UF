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

using static DIPOL_UF.DIPOL_UF_App;

namespace DIPOL_UF.Models
{
    class DipolMainWindow : ObservableObject, IDisposable
    {
        private DispatcherTimer _UIStatusUpdateTimer = null;

        private DispatcherTimer _TestTimer = null;

        private bool canEnterSessionManager = true;
        private bool canConnect = true;
        private bool isDisposed = false;
        private string[] remoteLocations
            = Settings.GetValueOrNullSafe<object[]>("RemoteLocations")?.Cast<String>()?.ToArray() 
            ?? new string[0]; 
        private DipolClient[] remoteClients;

        /// <summary>
        /// Connect button command
        /// </summary>
        private Commands.DelegateCommand connectButtonCommand;
        /// <summary>
        /// Disconnect button command
        /// </summary>
        private Commands.DelegateCommand disconnectButtonCommand;
        /// <summary>
        /// Handles selection changed event of the tree view
        /// </summary>
        private Commands.DelegateCommand cameraTreeViewSelectionChangedCommand;

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
        private ObservableCollection<ConnectedCamerasTreeViewModel> treeCameraRepresentation
            = new ObservableCollection<ConnectedCamerasTreeViewModel>();

        /// <summary>
        /// Collection of selected cameras (checked with checkboxes)
        /// </summary>
        private ObservableCollection<string> cameraTreeViewSelectedItems
            = new ObservableCollection<string>();

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
        public ObservableCollection<ConnectedCamerasTreeViewModel> TreeCameraRepresentation
        {
            get => treeCameraRepresentation;
            set
            {
                if (value != treeCameraRepresentation)
                {
                    treeCameraRepresentation = value;
                    RaisePropertyChanged();
                }
            }
        }
        /// <summary>
        /// List of camera IDs that are currently selected in the TreeView
        /// </summary>
        public ObservableCollection<string> CameraTreeViewSelectedItems
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

        public Commands.DelegateCommand ConnectButtonCommand
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
        public Commands.DelegateCommand DisconnectButtonCommand
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
        public Commands.DelegateCommand CameraTreeViewSelectionChangedCommand
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
            ConnectButtonCommand = new Commands.DelegateCommand(
                ListAndSelectAvailableCameras,
                (param) => canConnect);

            DisconnectButtonCommand = new Commands.DelegateCommand(
                DisconnectCameras,
                CanDisconnectCameras);
            connectedCameras.CollectionChanged += (sender, e) => DisconnectButtonCommand.OnCanExecuteChanged();
            cameraTreeViewSelectedItems.CollectionChanged += (sender, e) => DisconnectButtonCommand.OnCanExecuteChanged();
            

            CameraTreeViewSelectionChangedCommand = new Commands.DelegateCommand(
                CameraTreeViewSelectionChangedCommandHandler,
                Commands.DelegateCommand.CanExecuteAlways);

        }
        private void InitializeRemoteSessions()
        {
            remoteClients = new DipolClient[remoteLocations.Length];
            Parallel.For(0, remoteClients.Length, (i) =>
            {
                remoteClients[i] = new DipolClient(remoteLocations[i]);
                remoteClients[i].Connect();
            });
        }
       
        private void CameraTreeViewSelectionChangedCommandHandler(object parameter)
        {
            if (parameter is Commands.CommandEventArgs<RoutedEventArgs> args)
            {
                args.EventArgs.Handled = true;

                if (args.Sender is FrameworkElement sender)
                {
                    var context = sender.DataContext;

                    var camID = (context is KeyValuePair<string, ConnectedCameraTreeItemViewModel>)
                        ? ((KeyValuePair<string, ConnectedCameraTreeItemViewModel>)context).Key 
                        : null;

                    var state = (sender as System.Windows.Controls.CheckBox)?.IsChecked ?? false;

                    if (state)
                        CameraTreeViewSelectedItems.Add(camID);
                    else
                        CameraTreeViewSelectedItems.Remove(camID);
                }              
            }
        }

        private bool CanDisconnectCameras(object parameter)
            => !connectedCameras.IsEmpty && cameraTreeViewSelectedItems.Count > 0;

        private void ListAndSelectAvailableCameras(object parameter)
        {
            var cameraQueryModel = new AvailableCamerasModel(remoteClients);
            var viewModel = new ViewModels.AvailableCamerasViewModel(cameraQueryModel);
            var wind = new Views.AvailableCameraView(viewModel);
            if (parameter is Window owner)
                wind.Owner = owner;
            canConnect = false;
            ConnectButtonCommand?.OnCanExecuteChanged();
            wind.Show();

            cameraQueryModel.CameraSelectionsMade += CameraSelectionMade;           

        }

        private void CameraSelectionMade(object e)
        {
            var providedCameras = e as IEnumerable<KeyValuePair<string, CameraBase>>;

            foreach (var x in providedCameras)
                if (ConnectedCameras.TryAdd(x.Key, x.Value))
                    HookCamera(x.Key, x.Value);

            string[] categories = providedCameras.Select(item => Helper.GetCameraHostName(item.Key)).ToArray();

            foreach (var cat in categories)
            {
                treeCameraRepresentation.Add(new ConnectedCamerasTreeViewModel(new ConnectedCamerasTreeModel()
                {
                    Name = cat,
                    CameraList = new ObservableConcurrentDictionary<string, ConnectedCameraTreeItemViewModel>(
                        providedCameras
                        .Where(item => Helper.GetCameraHostName(item.Key) == cat)
                        .Select(item => new KeyValuePair<string, ConnectedCameraTreeItemViewModel>(
                            item.Key,
                            new ConnectedCameraTreeItemViewModel(
                                new ConnectedCameraTreeItemModel(item.Value)))))
                }));
            }

            canConnect = true;
            ConnectButtonCommand?.OnCanExecuteChanged();

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
            
            camera.TemperatureMonitor(Switch.Enabled, Settings.GetValueOrNullSafe("UICamStatusUpdateDelay", 500));
            //camera.SetTemperature(-20);
            //camera.CoolerControl(Switch.Enabled);
            camera.FanControl(FanMode.FullSpeed);
            camera.TemperatureStatusChecked += (sender, e) =>
            {
                cameraRealTimeStats[key]["Temp"] = e.Temperature;
                cameraRealTimeStats[key]["TempStatus"] = e.Status;

            };

        }

        private void DispatcherTimerTickHandler(object sener, EventArgs e)
        {
            RaisePropertyChanged(nameof(CameraRealTimeStats));
        }

        /// <summary>
        /// Disconnects cameras
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        private async void DisconnectCameras(object parameter)
        {
            List<Task> workers = new List<Task>();

            for (int camIndex = 0; camIndex < CameraTreeViewSelectedItems.Count; camIndex++)
            {
                string category = Helper.GetCameraHostName(CameraTreeViewSelectedItems[camIndex]);
                if(!String.IsNullOrWhiteSpace(category))
                {
                    var node = TreeCameraRepresentation.Where(item => item.Name == category).DefaultIfEmpty(null).FirstOrDefault();

                    foreach (var camItem in node.CameraList.Where(item => item.Key == CameraTreeViewSelectedItems[camIndex]))
                    {
                        node.CameraList.TryRemove(camItem.Key, out _);
                        workers.Add(DisposeCamera(camItem.Key, false));
                        if (node.CameraList.IsEmpty)
                            TreeCameraRepresentation.Remove(node);
                    }

                }
            }

            CameraTreeViewSelectedItems.Clear();

            await Task.WhenAll(workers);
        }

        private async Task DisposeCamera(string camID, bool removeSelection = true)
        {

            connectedCameras.TryRemove(camID, out CameraBase camInstance);
            if (removeSelection)
                cameraTreeViewSelectedItems.Remove(camID);


            await Task.Run(() =>
                {
                    camInstance?.CoolerControl(Switch.Disabled);
                    camInstance.TemperatureMonitor(Switch.Disabled);
                    camInstance?.Dispose();
                });
           
        }
    }
}
