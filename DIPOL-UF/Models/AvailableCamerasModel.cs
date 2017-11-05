using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;

using ANDOR_CS.Classes;
using ANDOR_CS.Exceptions;
using DIPOL_UF.Commands;

namespace DIPOL_UF.Models
{
    class AvailableCamerasModel : ObservableObject
    {

        internal enum ClosingState : byte
        {
            Canceled = 0,
            Connect = 1,
            ConnectAll = 2
        }

        private ClosingState closingWindowState = ClosingState.Canceled;
        private ObservableConcurrentDictionary<string, CameraBase> foundCameras = new ObservableConcurrentDictionary<string, CameraBase>();
        private ObservableCollection<string> selectedItems = new ObservableCollection<string>();

        private DelegateCommand selectionChangedCommand;
        private DelegateCommand cancelButtonCommand;
        private DelegateCommand connectButtonCommand;
        private DelegateCommand connectAllButtonCommand;
        private DelegateCommand windowClosingCommand;

        private ObservableCollection<string> SelectedItems
        {
            get => selectedItems;
            set
            {
                if (value != selectedItems)
                {
                    selectedItems = value;

                }
            }
        }


        public event Action<object> CameraSelectionsMade;

        public ObservableConcurrentDictionary<string, CameraBase> FoundCameras
        {
            get => foundCameras;
            set
            {
                if (value != foundCameras)
                {
                    foundCameras = value;
                    RaisePropertyChanged();
                }
            }
        }
       
        public DelegateCommand SelectionChangedCommand
        {
            get => selectionChangedCommand;
            private set
            {
                if (value != selectionChangedCommand)
                {
                    selectionChangedCommand = value;
                    RaisePropertyChanged();
                }
            }
        }
        public DelegateCommand WindowClosingCommand
        {
            get => windowClosingCommand;
            private set
            {
                if (value != windowClosingCommand)
                {
                    windowClosingCommand = value;
                    RaisePropertyChanged();
                }
            }
        }
        public DelegateCommand CancelButtonCommand
        {
            get => cancelButtonCommand;
            private set
            {
                if (value != cancelButtonCommand)
                {
                    cancelButtonCommand = value;
                    RaisePropertyChanged();
                }
            }
        }
        public DelegateCommand ConnectButtonCommand
        {
            get => connectButtonCommand;
            private set
            {
                if (value != connectButtonCommand)
                {
                    connectButtonCommand = value;
                    RaisePropertyChanged();
                }
            }
        }
        public DelegateCommand ConnectAllButtonCommand
        {
            get => connectAllButtonCommand;
            private set
            {
                if (value != connectAllButtonCommand)
                {
                    connectAllButtonCommand = value;
                    RaisePropertyChanged();
                }
            }
        }

        public AvailableCamerasModel()
        {
            InitializeCommands();

            foundCameras.PropertyChanged += (sender, e) => Helper.WriteLog(e.PropertyName);
            Task.Run(() =>
            {
                try
                {
                    QueryLocalCameras();
                    QueryRemoteCameras();
                }
                catch (Exception e)
                {
                    Helper.WriteLog(e.Message);
                }
            });
        }

        private void InitializeCommands()
        {
            
            SelectionChangedCommand = new DelegateCommand(
                SelectionChangedHandler,
                DelegateCommand.CanExecuteAlways);

            WindowClosingCommand = new DelegateCommand(
                WindowClosingHandler,
                DelegateCommand.CanExecuteAlways);

            CancelButtonCommand = new DelegateCommand(
                (param) => ButtonClickCloseWindow(param as Window, ClosingState.Canceled),
                DelegateCommand.CanExecuteAlways);

            ConnectButtonCommand = new DelegateCommand(
                (param) => ButtonClickCloseWindow(param as Window, ClosingState.Connect),
                (param) => SelectedItems.Count > 0);
            SelectedItems.CollectionChanged += (sender, e) => ConnectButtonCommand.OnCanExecuteChanged();

            ConnectAllButtonCommand = new DelegateCommand(
                (param) => ButtonClickCloseWindow(param as Window, ClosingState.ConnectAll),
                (param) => FoundCameras.Count > 0);
            FoundCameras.CollectionChanged += (sender, e) => ConnectAllButtonCommand.OnCanExecuteChanged();



        }

        private void QueryLocalCameras()
        {
            int nCams;
            try
            {
                nCams = Camera.GetNumberOfCameras();
            }
            catch (AndorSDKException aExp)
            {
                Helper.WriteLog(aExp);
                nCams = 0;
            }

            nCams = 10;


            for (int camIndex = 0; camIndex < nCams; camIndex++)
            {
                CameraBase cam = null;
                try
                {
                    //cam = new Camera(camIndex);
                    cam = Camera.GetDebugInterface(camIndex);
                }
                catch (AndorSDKException aExp)
                {
                    Helper.WriteLog(aExp);
                }

                if (cam != null)
                    FoundCameras.TryAdd($"localhost:{cam.CameraIndex}:{cam.CameraModel}:{cam.SerialNumber}", cam);

            }
        }
        private void QueryRemoteCameras()
        {
        }

        private void ButtonClickCloseWindow(Window window, ClosingState state)
        {
            closingWindowState = state;
            if (state == ClosingState.ConnectAll)
                SelectedItems = new ObservableCollection<string>(FoundCameras.Keys);

            window?.Close();
        }

        private void SelectionChangedHandler(object parameter)
        {
            if (parameter is Commands.CommandEventArgs<SelectionChangedEventArgs> commandPar)
            {
                foreach (var remItem in commandPar.EventArgs.RemovedItems)
                    if (remItem is KeyValuePair<string, CameraBase> rawItem)
                        selectedItems.Remove(rawItem.Key);

                foreach (var addItem in commandPar.EventArgs.AddedItems)
                    if (addItem is KeyValuePair<string, CameraBase> rawItem)
                        selectedItems.Add(rawItem.Key);
            }
        }
        private void WindowClosingHandler(object parameter)
        {
            OnCameraSelectionsMade();
            foreach (var camObj in FoundCameras.Where(item => !SelectedItems.Contains(item.Key)))
                camObj.Value.Dispose();

            FoundCameras.Clear();
            SelectedItems.Clear();
                
        }

        protected virtual void OnCameraSelectionsMade()
        {
            var query = from camObj in FoundCameras
                        join key in SelectedItems
                        on camObj.Key equals key
                        select camObj;
            CameraSelectionsMade?.Invoke(query.ToArray());
        }

    }
}
