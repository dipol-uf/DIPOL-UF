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
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Security.RightsManagement;
using DIPOL_UF.ViewModels;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace DIPOL_UF.Models
{
    internal sealed class AvailableCamerasModel : ReactiveObjectEx
    {

        internal enum ClosingState : byte
        {
            Canceled = 0,
            Connect = 1,
            ConnectAll = 2
        }

        private readonly ProgressBar _progressBar;
        private readonly DipolClient[] _remoteClients;

       
        [Reactive]
        public bool CanCancel { get; private set; }

        public ReactiveCommand<Window, Unit> WindowClosingCommand { get; }
        public ReactiveCommand<Window, Unit> CancelButtonCommand { get; }
        public ReactiveCommand<Window, Unit> WindowShownCommand { get; }
        public ReactiveCommand<Unit, Unit> ConnectButtonCommand { get; }
        public ReactiveCommand<Unit, Unit> ConnectAllButtonCommand { get; }
        
        
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

            _progressBar.DisposeWith(_subscriptions);

            CancelButtonCommand = ReactiveCommand.Create<Window>(
                x => x?.Close(),
                this.WhenAnyPropertyChanged(nameof(CanCancel))
                    .Select(x => x.CanCancel)
                    .ObserveOnUi());

            WindowShownCommand = ReactiveCommand.Create<Window>(
                WindowShownCommandExecute);

            HookValidators();
            HookObservers();

            
            QueryAvailableCameras();
        
        }

        private void InitializeCommands()
        {
            //WindowShownCommand = new DelegateCommand(
            //    (param) =>
            //    {
            //        if (_progressBar == null) return;

            //        //_progressView = new Views.ProgressWindow(new ViewModels.ProgressBarViewModel(_progressBar))
            //        //{
            //        //    Owner = (param as CommandEventArgs<EventArgs>)?.Sender as Window,
            //        //    WindowStartupLocation = WindowStartupLocation.CenterOwner
            //        //};

            //        //if (!_camerasPresent)
            //        //{
            //        //    _progressBar.BarComment = "No cameras found. Check your connections.";
            //        //    Task.Run(() =>
            //        //    {
            //        //        Task.Delay(3750).Wait();
            //        //        Application.Current?.Dispatcher?.Invoke(_progressView.Close);
            //        //        CanCancel = true;
            //        //    });
            //        //}

            //        //_progressView.Show();
            //        // TODO: Fix here
            //        //_progressBar.AbortButtonClick += (sender, e) => CanCancel = true;
                    

            //    },
            //    DelegateCommand.CanExecuteAlways);

           

            
            //ConnectButtonCommand = new DelegateCommand(
            //    (param) => ButtonClickCloseWindow(param as Window, ClosingState.Connect),
            //    (param) => SelectedItems.Count > 0);
            //SelectedItems.CollectionChanged += (sender, e) => ConnectButtonCommand.OnCanExecuteChanged();

            //ConnectAllButtonCommand = new DelegateCommand(
            //    (param) => ButtonClickCloseWindow(param as Window, ClosingState.ConnectAll),
            //    (param) => FoundCameras.Count > 0);
            //FoundCameras.CollectionChanged += (sender, e) => ConnectAllButtonCommand.OnCanExecuteChanged();

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
                // TODO : Check here
                //_camerasPresent = true;
                var cancelSource = new CancellationTokenSource();
                _progressBar.Maximum = nLocal + nRemote;
                _progressBar.IsIndeterminate = false;

                // TODO : Fix here
                //_progressBar.AbortButtonClick += (sender, e) => cancelSource.Cancel();
                using (_progressBar
                       .WhenAnyPropertyChanged(nameof(_progressBar.IsAborted))
                       .DistinctUntilChanged()
                       .Where(x => x.IsAborted)
                       .Subscribe(_ => cancelSource.Cancel()))
                {

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
                    // TODO : Fix here
                    //if (cam != null)
                    //    FoundCameras.TryAdd($"localhost:{cam.CameraIndex}:{cam.CameraModel}:{cam.SerialNumber}", cam);

                    // Try thread-safely increment progress bar
                    if (!(_progressBar is null))
                    {
                        _progressBar.TryIncrement();

                        _progressBar.BarComment = cam == null
                            ? "Camera resource is unavailable."
                            : "Acquired local camera " +
                              $"{new Converters.CameraToStringAliasValueConverter().Convert(cam, typeof(string), null, System.Globalization.CultureInfo.CurrentUICulture)}";
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
                            // TODO : Fix here
                            //if (cam != null)
                            //    FoundCameras.TryAdd($"{client.HostAddress}:{cam.CameraIndex}:{cam.CameraModel}:{cam.SerialNumber}", cam);

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

        private void WindowShownCommandExecute(Window param)
        {
            if (_progressBar is null)
                return;
        }

        private void ButtonClickCloseWindow(object sender, EventArgs e)
        {
            //if (state == ClosingState.ConnectAll)
            //    SelectedItems = new ObservableCollection<string>(FoundCameras.Keys);

            if(sender is Window w)
                w.Close();
            //window?.Close();
        }

        private void SelectionChangedHandler(object sender, EventArgs e)
        {
            if (e is SelectionChangedEventArgs param)
            {
                //foreach (var remItem in param.RemovedItems)
                //    if (remItem is KeyValuePair<string, string> rawItem)
                //        _selectedItems.Remove(rawItem.Key);

                //foreach (var addItem in param.AddedItems)
                //    if (addItem is KeyValuePair<string, string> rawItem)
                //        _selectedItems.Add(rawItem.Key);

            }
        }
        private void WindowClosingHandler()
        {
            //Parallel.ForEach(FoundCameras.Where(item => !SelectedItems.Contains(item.Key)), (item) => item.Value?.Dispose());
            //_selectedCameras = FoundCameras.Join(SelectedItems, x => x.Key, y => y, (x, y) => x).ToList();
            //FoundCameras.Clear();
            //SelectedItems.Clear();
                
        }

        private void HookObservers()
        {
            _progressBar
                ?.WhenPropertyChanged(x => x.IsAborted)
                .DistinctUntilChanged()
                .Subscribe(x => CanCancel = x.Value)
                .DisposeWith(_subscriptions);

            //_progressBar
                //?.MaximumReached
                //.Delay(TimeSpan.Parse(UiSettingsProvider.Settings.Get("PopUpDelay", "00:00:00.750")))
                //.ObserveOnUi()
                //.Subscribe(_ =>
                //{
                //    if(_progressView?.IsVisible ?? false)
                //        _progressView.Close();
                //    CanCancel = true;
                //})
                //.DisposeWith(_subscriptions);
        }

    }
}
