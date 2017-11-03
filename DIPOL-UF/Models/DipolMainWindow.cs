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

namespace DIPOL_UF.Models
{
    class DipolMainWindow : ObservableObject, IDisposable
    {
        private bool isDisposed = false;

        private Commands.DelegateCommand connectButtonCommand;
        private Commands.DelegateCommand disconnectButtonCommand;
        private ObservableCollection<ViewModels.MenuItemViewModel> menuBarItems;
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
        private bool CanDisconnectCameras(object parameter)
            => !connectedCameras.IsEmpty;
        private void ListAndSelectAvailableCameras(object parameter)
        {

        }
    }
}
