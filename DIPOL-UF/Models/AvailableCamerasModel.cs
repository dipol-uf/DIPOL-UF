using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Threading;
using System.Threading.Tasks;

using ANDOR_CS.Classes;
using ANDOR_CS.Exceptions;

using DIPOL_Remote.Classes;

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

        private ProgressBar progressBar = null;
        private Views.ProgressWindow progressView = null;
        private DipolClient[] remoteClients = null;
        private ClosingState closingWindowState = ClosingState.Canceled;
        private ObservableConcurrentDictionary<string, CameraBase> foundCameras = new ObservableConcurrentDictionary<string, CameraBase>();
        private ObservableCollection<string> selectedItems = new ObservableCollection<string>();

        private DelegateCommand selectionChangedCommand;
        private DelegateCommand cancelButtonCommand;
        private DelegateCommand connectButtonCommand;
        private DelegateCommand connectAllButtonCommand;
        private DelegateCommand windowClosingCommand;
        private DelegateCommand windowShownCommand;

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
        public DelegateCommand WindowShownCommand
        {
            get => windowShownCommand;
            private set
            {
                if (value != windowShownCommand)
                {
                    windowShownCommand = value;
                    RaisePropertyChanged();
                }
            }
        }

        public AvailableCamerasModel(DipolClient[] remoteClients = null)
        {
            this.remoteClients = remoteClients;

            InitializeCommands();

            int nLocal = 0;
            int nRemote = 0;

            try
            {
                nLocal = Camera.GetNumberOfCameras();
            }
            catch (AndorSDKException aExp)
            {
                Helper.WriteLog(aExp);
            }

            foreach (var client in remoteClients)
                try
                {
                    nRemote += client?.GetNumberOfCameras() ?? 0;
                }
                catch (Exception e)
                {
                    Helper.WriteLog(e.Message);
                }

            CancellationTokenSource cancelSource = new CancellationTokenSource();

            if (nLocal + nRemote > 0)
            {
                progressBar = new ProgressBar()
                {
                    Minimum = 0,
                    Value = 0,
                    Maximum = nLocal + nRemote,
                    IsIndeterminate = false,
                    CanAbort = true,
                    DisplayPercents = false,
                    BarTitle = "Checking connections...."
                };

                progressBar.AbortButtonClick += (sender, e) => cancelSource.Cancel();
            }

            if(nLocal > 0)
                Task.Run(() =>
                {
                    try
                    {
                        QueryLocalCameras(cancelSource.Token);
                    }
                    catch (Exception e)
                    {
                        Helper.WriteLog(e.Message);
                    }
                });

            if(nRemote > 0)
                Task.Run(() =>
                {
                    try
                    {
                        QueryRemoteCameras(cancelSource.Token);
                    }
                    catch (Exception e)
                    {
                        Helper.WriteLog(e.Message);
                    }

                });

        }

        private void InitializeCommands()
        {
            WindowShownCommand = new DelegateCommand(
                (param) =>
                {
                    if (progressBar != null)
                    {
                        progressView = new Views.ProgressWindow(new ViewModels.ProgressBarViewModel(progressBar));
                        progressView.Owner = (param as CommandEventArgs<EventArgs>)?.Sender as Window;
                        progressView.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        progressView.Show();
                    }
                },
                DelegateCommand.CanExecuteAlways);

            
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

        private void QueryLocalCameras(CancellationToken token)
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



            for (int camIndex = 0; camIndex < nCams; camIndex++)
            {
                if (token.IsCancellationRequested)
                    break;
                CameraBase cam = null;
                try
                {
                    if(!Camera.CamerasInUse.Values.Select(item => item.CameraIndex).Contains(camIndex))
                        cam = new Camera(camIndex);
                }
                catch (Exception aExp)
                {
                    Helper.WriteLog(aExp);
                }

                if (token.IsCancellationRequested)
                {
                    cam?.Dispose();
                    break;
                }

                if (cam != null)
                    FoundCameras.TryAdd($"localhost:{cam.CameraIndex}:{cam.CameraModel}:{cam.SerialNumber}", cam);


                if (progressBar?.TryIncrement() ?? false)
                {
                    progressBar.BarComment = cam == null ? "Camera resource is unavailable." : $"Acquired local camera {cam.ToString()}";
                }

                if (progressBar?.Value == progressBar?.Maximum)
                {
                    Task.Delay(750).Wait();
                    Application.Current.Dispatcher.Invoke(progressView.Close);
                }

            }
        }
        private void QueryRemoteCameras(CancellationToken token)
        {
            foreach (var client in remoteClients)
            {
                if (token.IsCancellationRequested)
                    break;
                Task.Run(() =>
                {
                    try
                    {

                        int nCams = client.GetNumberOfCameras();

                        for (int camIndex = 0; camIndex < nCams; camIndex++)
                        {
                            if (token.IsCancellationRequested)
                                break;

                            CameraBase cam = null;
                            try
                            {
                                cam = client.CreateRemoteCamera(camIndex);
                            }
                            catch (Exception aExp)
                            {
                                Helper.WriteLog(aExp);
                            }

                            if (token.IsCancellationRequested)
                            {
                                cam?.Dispose();
                                break;
                            }

                            if (cam != null)
                                FoundCameras.TryAdd($"{client.HostAddress}:{cam.CameraIndex}:{cam.CameraModel}:{cam.SerialNumber}", cam);

                            if (progressBar?.TryIncrement() ?? false)
                            {
                                progressBar.BarComment = cam == null ? "Camera resource is unavailable." : $"Acquired remote camera {cam.ToString()}";
                            }

                            if (progressBar?.Value == progressBar?.Maximum)
                            {
                                Task.Delay(750).Wait();
                                Application.Current.Dispatcher.Invoke(progressView.Close);

                            }

                        }
                    }
                    catch (Exception e)
                    {
                        Helper.WriteLog(e);
                    }
                }, token);
            }
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
            Parallel.ForEach(FoundCameras.Where(item => !SelectedItems.Contains(item.Key)), (item) => item.Value?.Dispose());
                        
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
