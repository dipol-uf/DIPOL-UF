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
using System.ComponentModel;

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

        private List<Task> workerPool = new List<Task>();
        private bool canCancel = false;
        private bool camerasPresent = false;
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
       
        public bool CanCancel
        {
            get => canCancel;
            set
            {
                if (value != canCancel)
                {
                    canCancel = value;
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

            progressBar = new ProgressBar()
            {
                Minimum = 0,
                Value = 0,
                Maximum = 1,
                IsIndeterminate = true,
                CanAbort = true,
                DisplayPercents = false,
                BarTitle = "Checking connections...."
            };

            InitializeCommands();
            QueryAvailableCameras();
        
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

                        if (!camerasPresent)
                        {
                            progressBar.BarComment = "No cameras found. Check your connections.";
                                workerPool.Add(Task.Run(() => {
                                    Task.Delay(3750).Wait();
                                    Application.Current.Dispatcher.Invoke(progressView.Close);
                            }));
                        }

                        progressView.Show();
                        progressBar.AbortButtonClick += (sender, e) => CanCancel = true;
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
                (param) => CanCancel);
            
            ConnectButtonCommand = new DelegateCommand(
                (param) => ButtonClickCloseWindow(param as Window, ClosingState.Connect),
                (param) => SelectedItems.Count > 0);
            SelectedItems.CollectionChanged += (sender, e) => ConnectButtonCommand.OnCanExecuteChanged();

            ConnectAllButtonCommand = new DelegateCommand(
                (param) => ButtonClickCloseWindow(param as Window, ClosingState.ConnectAll),
                (param) => FoundCameras.Count > 0);
            FoundCameras.CollectionChanged += (sender, e) => ConnectAllButtonCommand.OnCanExecuteChanged();



        }
        private void QueryAvailableCameras()
        {
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


            if (nLocal + nRemote > 0)
            {
                camerasPresent = true;
                CancellationTokenSource cancelSource = new CancellationTokenSource();
                progressBar.Maximum = nLocal + nRemote;
                progressBar.IsIndeterminate = false;

                progressBar.AbortButtonClick += (sender, e) => cancelSource.Cancel();

                if (nLocal > 0)
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

                if (nRemote > 0)
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
            
        }

        private void QueryLocalCameras(CancellationToken token)
        {
            // Number of cameras
            int nCams;
            try
            {
                nCams = Camera.GetNumberOfCameras();
            }
            // If for some reason camera retrieval number fails
            catch (AndorSDKException aExp)
            {
                Helper.WriteLog(aExp);
                nCams = 0;
            }

            // For each found local camera
            for (int camIndex = 0; camIndex < nCams; camIndex++)
            {
                // Check cancellation request
                if (token.IsCancellationRequested)
                    break;

                // If camera is nor present on the active list
                CameraBase cam = null;
                try
                {
                    if(!Camera.CamerasInUse.Values.Select(item => item.CameraIndex).Contains(camIndex))
                        cam = new Camera(camIndex);
                }
                // Silently catch exception and continue
                catch (Exception aExp)
                {
                    Helper.WriteLog(aExp);
                }

                // If canceled, free camera
                if (token.IsCancellationRequested)
                {
                    cam?.Dispose();
                    cam = null;
                    break;
                }

                // If camera is OK, add it to the list
                if (cam != null)
                    FoundCameras.TryAdd($"localhost:{cam.CameraIndex}:{cam.CameraModel}:{cam.SerialNumber}", cam);

                // Try thread-safely increment progress bar
                if (progressBar != null)
                {
                    if (Application.Current.Dispatcher.IsAvailable())
                        Application.Current.Dispatcher.Invoke(progressBar.TryIncrement);
                    else
                        lock (progressBar)
                            progressBar.TryIncrement();

                    progressBar.BarComment = cam == null ? "Camera resource is unavailable." : $"Acquired local camera {cam.ToString()}";
                }

                // Close progress bar if everything is done ?
                if (progressBar?.Value == progressBar?.Maximum)
                {
                    Task.Delay(750).Wait();
                    Application.Current.Dispatcher.Invoke(progressView.Close);
                    CanCancel = true;
                }

            }
        }
        private void QueryRemoteCameras(CancellationToken token)
        {
            // For each remote client
            foreach (var client in remoteClients)
            {
                // Checks if cancellation is requested
                if (token.IsCancellationRequested)
                    break;
                // Runs task in parallel
                Task.Run(() =>
                {
                    try
                    {
                        // Number of available cameras
                        int nCams = client.GetNumberOfCameras();

                        // For each camera
                        for (int camIndex = 0; camIndex < nCams; camIndex++)
                        {
                            // If cancellation requested
                            if (token.IsCancellationRequested)
                                break;

                            // Try to create remote camera
                            CameraBase cam = null;
                            try
                            {
                                if(!client.ActiveRemoteCameras().Contains(camIndex))
                                    cam = client.CreateRemoteCamera(camIndex);  
                            }
                            // Catch silently
                            catch (Exception aExp)
                            {
                                Helper.WriteLog(aExp);
                            }

                            // Free camera if cancelled
                            if (token.IsCancellationRequested)
                            {
                                cam?.Dispose();
                                cam = null;
                                break;
                            }

                            // Add to collection
                            if (cam != null)
                                FoundCameras.TryAdd($"{client.HostAddress}:{cam.CameraIndex}:{cam.CameraModel}:{cam.SerialNumber}", cam);

                            // Try increment progress bar
                            if (progressBar != null)
                            {
                                if (Application.Current.Dispatcher.IsAvailable())
                                    Application.Current.Dispatcher.Invoke(progressBar.TryIncrement);
                                else
                                    lock (progressBar)
                                        progressBar.TryIncrement();

                                progressBar.BarComment = cam == null ? "Camera resource is unavailable." : $"Acquired remote camera {cam.ToString()}";
                            }

                            // Close window?
                            if (progressBar?.Value == progressBar?.Maximum)
                            {
                                Task.Delay(TimeSpan.Parse(DIPOL_UF_App.Settings.GetValueOrNullSafe<string>("PopUpDelay", "00:00:00.750"))).Wait();
                                Application.Current.Dispatcher.Invoke(progressView.Close);
                                CanCancel = true;
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
            if (parameter is CommandEventArgs<SelectionChangedEventArgs> commandPar)
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

        protected override void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(sender, e);

            if (e.PropertyName == nameof(CanCancel) && CancelButtonCommand != null)
                Application.Current.Dispatcher.Invoke(CancelButtonCommand.OnCanExecuteChanged);
        }
    }
}
