using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using ANDOR_CS.Classes;

using DIPOL_Remote.Classes;


namespace DIPOL_UF.Models
{
    class DipolMainWindow : ObservableObject, IDisposable
    {
        private bool isDisposed = false;
        private string[] remoteLocations =
           { "dipol-2" };
        private DipolClient[] remoteClients;


        private Commands.DelegateCommand connectButtonCommand;
        private Commands.DelegateCommand disconnectButtonCommand;
        private ObservableCollection<ViewModels.MenuItemViewModel> menuBarItems
            = new ObservableCollection<ViewModels.MenuItemViewModel>();
        private ObservableConcurrentDictionary<string, CameraBase> connectedCameras 
            = new ObservableConcurrentDictionary<string, CameraBase>();


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
                foreach (var cam in connectedCameras)
                {
                    connectedCameras.TryRemove($"{cam.Value.CameraModel}{cam.Value.SerialNumber}", out CameraBase camInstance);
                    camInstance?.Dispose();
                }

                foreach (var client in remoteClients)
                {
                    client?.Disconnect();
                    client?.Dispose();
                }

                IsDisposed = true;

            }
        }

        private void InitializeMenu()
        {

        }
        private void InitializeCommands()
        {
            connectButtonCommand = new Commands.DelegateCommand(
                ListAndSelectAvailableCameras,
                (param) => true);
            disconnectButtonCommand = new Commands.DelegateCommand(
                (param) => { },
                CanDisconnectCameras);

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
                foreach (var x in e as IEnumerable<KeyValuePair<string, CameraBase>>)
                    ConnectedCameras.TryAdd(x.Key, x.Value);
            };

        }
    }
}
