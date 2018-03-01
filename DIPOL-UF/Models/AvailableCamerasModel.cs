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

        private readonly List<Task> _workerPool = new List<Task>();
        private bool _canCancel = false;
        private bool _camerasPresent = false;
        private readonly ProgressBar _progressBar = null;
        private Views.ProgressWindow _progressView = null;
        private readonly DipolClient[] _remoteClients = null;
        private ObservableConcurrentDictionary<string, CameraBase> _foundCameras = new ObservableConcurrentDictionary<string, CameraBase>();
        private ObservableCollection<string> _selectedItems = new ObservableCollection<string>();

        private DelegateCommand _selectionChangedCommand;
        private DelegateCommand _cancelButtonCommand;
        private DelegateCommand _connectButtonCommand;
        private DelegateCommand _connectAllButtonCommand;
        private DelegateCommand _windowClosingCommand;
        private DelegateCommand _windowShownCommand;

        private ObservableCollection<string> SelectedItems
        {
            get => _selectedItems;
            set
            {
                if (value != _selectedItems)
                {
                    _selectedItems = value;

                }
            }
        }

        public event Action<object> CameraSelectionsMade;

        public ObservableConcurrentDictionary<string, CameraBase> FoundCameras
        {
            get => _foundCameras;
            set
            {
                if (value != _foundCameras)
                {
                    _foundCameras = value;
                    RaisePropertyChanged();
                }
            }
        }
       
        public bool CanCancel
        {
            get => _canCancel;
            set
            {
                if (value != _canCancel)
                {
                    _canCancel = value;
                    RaisePropertyChanged();
                }
            }
        }

        public DelegateCommand SelectionChangedCommand
        {
            get => _selectionChangedCommand;
            private set
            {
                if (value != _selectionChangedCommand)
                {
                    _selectionChangedCommand = value;
                    RaisePropertyChanged();
                }
            }
        }
        public DelegateCommand WindowClosingCommand
        {
            get => _windowClosingCommand;
            private set
            {
                if (value != _windowClosingCommand)
                {
                    _windowClosingCommand = value;
                    RaisePropertyChanged();
                }
            }
        }
        public DelegateCommand CancelButtonCommand
        {
            get => _cancelButtonCommand;
            private set
            {
                if (value != _cancelButtonCommand)
                {
                    _cancelButtonCommand = value;
                    RaisePropertyChanged();
                }
            }
        }
        public DelegateCommand ConnectButtonCommand
        {
            get => _connectButtonCommand;
            private set
            {
                if (value != _connectButtonCommand)
                {
                    _connectButtonCommand = value;
                    RaisePropertyChanged();
                }
            }
        }
        public DelegateCommand ConnectAllButtonCommand
        {
            get => _connectAllButtonCommand;
            private set
            {
                if (value != _connectAllButtonCommand)
                {
                    _connectAllButtonCommand = value;
                    RaisePropertyChanged();
                }
            }
        }
        public DelegateCommand WindowShownCommand
        {
            get => _windowShownCommand;
            private set
            {
                if (value != _windowShownCommand)
                {
                    _windowShownCommand = value;
                    RaisePropertyChanged();
                }
            }
        }

        public AvailableCamerasModel(DipolClient[] remoteClients = null)
        {
            this._remoteClients = remoteClients;

            _progressBar = new ProgressBar()
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
                    if (_progressBar != null)
                    {
                        _progressView = new Views.ProgressWindow(new ViewModels.ProgressBarViewModel(_progressBar));
                        _progressView.Owner = (param as CommandEventArgs<EventArgs>)?.Sender as Window;
                        _progressView.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                        if (!_camerasPresent)
                        {
                            _progressBar.BarComment = "No cameras found. Check your connections.";
                                _workerPool.Add(Task.Run(() => {
                                    Task.Delay(3750).Wait();
                                    Application.Current.Dispatcher.Invoke(_progressView.Close);
                                    CanCancel = true;
                            }));
                        }

                        _progressView.Show();
                        _progressBar.AbortButtonClick += (sender, e) => CanCancel = true;
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
            catch (AndorSdkException aExp)
            {
                Helper.WriteLog(aExp);
            }

            foreach (var client in _remoteClients)
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
                _camerasPresent = true;
                CancellationTokenSource cancelSource = new CancellationTokenSource();
                _progressBar.Maximum = nLocal + nRemote;
                _progressBar.IsIndeterminate = false;

                _progressBar.AbortButtonClick += (sender, e) => cancelSource.Cancel();

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
            catch (AndorSdkException aExp)
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
                if (_progressBar != null)
                {
                    if (Application.Current.Dispatcher.IsAvailable())
                        Application.Current.Dispatcher.Invoke(_progressBar.TryIncrement);
                    else
                        lock (_progressBar)
                            _progressBar.TryIncrement();

                    _progressBar.BarComment = cam == null ? "Camera resource is unavailable." : $"Acquired local camera " +
                        $"{new Converters.CameraToStringAliasValueConverter().Convert(cam, typeof(string), null, System.Globalization.CultureInfo.CurrentUICulture)}";
                }

                // Close progress bar if everything is done ?
                if (_progressBar?.Value == _progressBar?.Maximum)
                {
                    Task.Delay(750).Wait();
                    Application.Current.Dispatcher.Invoke(_progressView.Close);
                    CanCancel = true;
                }

            }
        }
        private void QueryRemoteCameras(CancellationToken token)
        {
            // For each remote client
            foreach (var client in _remoteClients)
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
                            if (_progressBar != null)
                            {
                                if (Application.Current.Dispatcher.IsAvailable())
                                    Application.Current.Dispatcher.Invoke(_progressBar.TryIncrement);
                                else
                                    lock (_progressBar)
                                        _progressBar.TryIncrement();

                                _progressBar.BarComment = cam == null ? "Camera resource is unavailable." : $"Acquired remote camera " +
                                    $"{new Converters.CameraToStringAliasValueConverter().Convert(cam, typeof(string), null, System.Globalization.CultureInfo.CurrentUICulture)}";
                            }

                            // Close window?
                            if (_progressBar?.Value == _progressBar?.Maximum)
                            {
                                Task.Delay(TimeSpan.Parse(DIPOL_UF_App.Settings.GetValueOrNullSafe<string>("PopUpDelay", "00:00:00.750"))).Wait();
                                Application.Current.Dispatcher.Invoke(_progressView.Close);
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
                        _selectedItems.Remove(rawItem.Key);

                foreach (var addItem in commandPar.EventArgs.AddedItems)
                    if (addItem is KeyValuePair<string, CameraBase> rawItem)
                        _selectedItems.Add(rawItem.Key);

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
