using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Linq;

using ANDOR_CS.Classes;

using DIPOL_Remote.Classes;



namespace DIPOL_UF.Models
{
    class DipolMainWindow : ObservableObject, IDisposable
    {
        private bool isDisposed = false;
        private string[] remoteLocations = //new string[0];
           { "dipol-2", "dipol-3" };
        private DipolClient[] remoteClients;


        private Commands.DelegateCommand connectButtonCommand;
        private Commands.DelegateCommand disconnectButtonCommand;
        private Commands.DelegateCommand cameraTreeViewSelectionChangedCommand;

        private ObservableCollection<ViewModels.MenuItemViewModel> menuBarItems
            = new ObservableCollection<ViewModels.MenuItemViewModel>();
        private ObservableConcurrentDictionary<string, CameraBase> connectedCameras 
            = new ObservableConcurrentDictionary<string, CameraBase>();
        private ObservableCollection<ViewModels.ConnectedCamerasTreeViewModel> treeCameraRepresentation
            = new ObservableCollection<ViewModels.ConnectedCamerasTreeViewModel>();


        public ObservableCollection<ViewModels.MenuItemViewModel> MenuBarItems
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
        public ObservableCollection<ViewModels.ConnectedCamerasTreeViewModel> TreeCameraRepresentation
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
                Task[] pool = new Task[connectedCameras.Count];
                int taskInd = 0;
                foreach (var cam in connectedCameras)
                {
                    connectedCameras.TryRemove(cam.Key, out CameraBase camInstance);
                    pool[taskInd++] = Task.Run(() => camInstance?.Dispose());
                }

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
                (param) => true);

            DisconnectButtonCommand = new Commands.DelegateCommand(
                (param) => { },
                CanDisconnectCameras);

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

                    var camID = (context is KeyValuePair<string, CameraBase>) ? ((KeyValuePair<string, CameraBase>)context).Key : null;

                    var state = (sender as System.Windows.Controls.CheckBox)?.IsChecked ?? false;
                }              
            }
        }
        
        private bool CanDisconnectCameras(object parameter)
            => !connectedCameras.IsEmpty;
        private void ListAndSelectAvailableCameras(object parameter)
        {
            var cameraQueryModel = new AvailableCamerasModel(remoteClients);
            var viewModel = new ViewModels.AvailableCamerasViewModel(cameraQueryModel);
            var wind = new Views.AvailableCameraView(viewModel);
            if (parameter is Window owner)
                wind.Owner = owner;
            wind.Show();

            cameraQueryModel.CameraSelectionsMade += (e) =>
            {
                var providedCameras = e as IEnumerable<KeyValuePair<string, CameraBase>>;

                foreach (var x in providedCameras)
                    ConnectedCameras.TryAdd(x.Key, x.Value);

                string[]  categories = providedCameras.Select(item => item.Key.Split(':')[0])
                .ToArray();

                foreach (var cat in categories)
                {
                    treeCameraRepresentation.Add(new ViewModels.ConnectedCamerasTreeViewModel(new ConnectedCamerasTreeModel()
                    {
                        Name = cat,
                        CameraList = new ObservableConcurrentDictionary<string, CameraBase>(providedCameras.Where(item => item.Key.Substring(0, cat.Length) == cat))
                    }));
                }

            };

        }
    }
}
