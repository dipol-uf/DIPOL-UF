using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
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
    internal class AvailableCamerasModel : ObservableObject
    {

        internal enum ClosingState : byte
        {
            Canceled = 0,
            Connect = 1,
            ConnectAll = 2
        }

        private List<KeyValuePair<string, CameraBase>> _selectedCameras;
        private bool _canCancel;
        private bool _camerasPresent;
        private readonly ProgressBar _progressBar;
        private Views.ProgressWindow _progressView;
        private readonly DipolClient[] _remoteClients;
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
            set => _selectedItems = value ?? _selectedItems;
        }

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
            _remoteClients = remoteClients;

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
                    if (_progressBar == null) return;

                    _progressView = new Views.ProgressWindow(new ViewModels.ProgressBarViewModel(_progressBar))
                    {
                        Owner = (param as CommandEventArgs<EventArgs>)?.Sender as Window,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    };

                    if (!_camerasPresent)
                    {
                        _progressBar.BarComment = "No cameras found. Check your connections.";
                        Task.Run(() =>
                        {
                            Task.Delay(3750).Wait();
                            Application.Current?.Dispatcher?.Invoke(_progressView.Close);
                            CanCancel = true;
                        });
                    }

                    _progressView.Show();
                    _progressBar.AbortButtonClick += (sender, e) => CanCancel = true;
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
            var nLocal = 0;
            var nRemote = 0;

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
                var cancelSource = new CancellationTokenSource();
                _progressBar.Maximum = nLocal + nRemote;
                _progressBar.IsIndeterminate = false;

                _progressBar.AbortButtonClick += (sender, e) => cancelSource.Cancel();

                if (nLocal > 0)
                    try
                    {
                        QueryLocalCamerasAsync(cancelSource.Token);
                    }
                    catch (Exception e)
                    {
                        Helper.WriteLog(e.Message);
                    }


                if (nRemote > 0)
                    Task.Run(() =>
                    {
                        try
                        {
                            QueryRemoteCamerasAsync(cancelSource.Token);
                        }
                        catch (Exception e)
                        {
                            Helper.WriteLog(e.Message);
                        }

                    }, cancelSource.Token);
            }
            
        }

        private async void QueryLocalCamerasAsync(CancellationToken token)
        {
            // Number of cameras
            int nCams;
            try
            {
                nCams = Camera.GetNumberOfCameras();
            }
            // If for some reason camera number retrieval fails
            catch (AndorSdkException aExp)
            {
                Helper.WriteLog(aExp);
                nCams = 0;
            }

            var workers = new Task[nCams];

            // For each found local camera
            for (var camIndex = 0; camIndex < nCams; camIndex++)
            {
                // Check cancellation request
                if (token.IsCancellationRequested)
                    break;
                var index = camIndex;
                workers[camIndex] = Task.Run(async () => 
                {
                    // If camera is nor present on the active list
                    CameraBase cam = null;
                    try
                    {
                        if (!Camera.CamerasInUse.Values.Select(item => item.CameraIndex).Contains(index))
                            //cam = new Camera(camIndex);
                            cam = await Camera.CreateAsync(index);
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
                        return;
                    }

                    // If camera is OK, add it to the list
                    if (cam != null)
                        FoundCameras.TryAdd($"localhost:{cam.CameraIndex}:{cam.CameraModel}:{cam.SerialNumber}", cam);

                    // Try thread-safely increment progress bar
                    if (_progressBar != null)
                    {
                        if (Application.Current?.Dispatcher?.IsAvailable() ?? false)
                            Application.Current?.Dispatcher?.Invoke(_progressBar.TryIncrement);
                        else
                            _progressBar.TryIncrement();

                        _progressBar.BarComment = cam == null
                            ? "Camera resource is unavailable."
                            : "Acquired local camera " +
                              $"{new Converters.CameraToStringAliasValueConverter().Convert(cam, typeof(string), null, System.Globalization.CultureInfo.CurrentUICulture)}";
                    }

                    // Close progress bar if everything is done ?
                    if (_progressBar?.Value == _progressBar?.Maximum)
                    {
                        Task.Delay(750, token).Wait(token);
                        Application.Current?.Dispatcher?.Invoke(_progressView.Close);
                        CanCancel = true;
                    }
                }, token);
            }

            await Task.WhenAll(workers);
        }
        private async void QueryRemoteCamerasAsync(CancellationToken token)
        {

            var workers = new Task[_remoteClients.Length];
            var clientIndex = 0;
            // For each remote client
            foreach (var client in _remoteClients)
            {
                // Checks if cancellation is requested
                if (token.IsCancellationRequested)
                    break;
                // Runs task in parallel
                workers[clientIndex++] = Task.Run(async () =>
                {
                    try
                    {
                        // Number of available cameras
                        var nCams = client.GetNumberOfCameras();

                        // For each camera
                        for (var camIndex = 0; camIndex < nCams; camIndex++)
                        {
                            // If cancellation requested
                            if (token.IsCancellationRequested)
                                break;

                            // Try to create remote camera
                            CameraBase cam = null;
                            try
                            {
                                if (!client.ActiveRemoteCameras().Contains(camIndex))
                                    //cam = client.CreateRemoteCamera(camIndex);  
                                    cam = await RemoteCamera.CreateAsync(camIndex, client);
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
                                break;
                            }

                            // Add to collection
                            if (cam != null)
                                FoundCameras.TryAdd($"{client.HostAddress}:{cam.CameraIndex}:{cam.CameraModel}:{cam.SerialNumber}", cam);

                            // Try increment progress bar
                            if (_progressBar != null)
                            {
                                if (Application.Current?.Dispatcher?.IsAvailable() ?? false)
                                    Application.Current?.Dispatcher?.Invoke(_progressBar.TryIncrement);
                                else
                                    lock (_progressBar)
                                        _progressBar.TryIncrement();

                                _progressBar.BarComment = cam == null ? "Camera resource is unavailable." : "Acquired remote camera " +
                                    $"{new Converters.CameraToStringAliasValueConverter().Convert(cam, typeof(string), null, System.Globalization.CultureInfo.CurrentUICulture)}";
                            }

                            // Close window?
                            if (_progressBar?.Value == _progressBar?.Maximum)
                            {
                                Task.Delay(TimeSpan.Parse(UiSettingsProvider.Settings.Get("PopUpDelay", "00:00:00.750")), token).Wait(token);
                                Application.Current?.Dispatcher?.Invoke(_progressView.Close);
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

            await Task.WhenAll(workers);
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
                    if (remItem is KeyValuePair<string, string> rawItem)
                        _selectedItems.Remove(rawItem.Key);

                foreach (var addItem in commandPar.EventArgs.AddedItems)
                    if (addItem is KeyValuePair<string, string> rawItem)
                        _selectedItems.Add(rawItem.Key);

            }
        }
        private void WindowClosingHandler(object parameter)
        {
            Parallel.ForEach(FoundCameras.Where(item => !SelectedItems.Contains(item.Key)), (item) => item.Value?.Dispose());
            _selectedCameras = FoundCameras.Join(SelectedItems, x => x.Key, y => y, (x, y) => x).ToList();
            FoundCameras.Clear();
            SelectedItems.Clear();
                
        }

        protected override void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(sender, e);

            if (e.PropertyName == nameof(CanCancel) && CancelButtonCommand != null)
                Application.Current?.Dispatcher?.Invoke(CancelButtonCommand.OnCanExecuteChanged);
        }

        public List<KeyValuePair<string, CameraBase>> GetSelection() => _selectedCameras;
    }
}
