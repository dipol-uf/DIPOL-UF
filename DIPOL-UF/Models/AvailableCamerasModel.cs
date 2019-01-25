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
using System.Net.Mime;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Security.RightsManagement;
using DIPOL_UF.Converters;
using DIPOL_UF.ViewModels;
using DynamicData;
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
        private readonly List<int> _nRemote;

        private readonly SourceCache<(string Id, CameraBase Camera), string> FoundDevices
            = new SourceCache<(string Id, CameraBase Camera), string>(x => x.Id);

        private int _nLocal;
       
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
            _nRemote = new List<int>(remoteClients?.Length ?? 0);

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
            try
            {
                _nLocal = Camera.GetNumberOfCameras();
            }
            catch (AndorSdkException aExp)
            {
                Helper.WriteLog(aExp);
            }

            foreach (var client in _remoteClients)
                try
                {
                    _nRemote.Add(client?.GetNumberOfCameras() ?? 0);
                }
                catch (Exception e)
                {
                    Helper.WriteLog(e.Message);
                }

            if (_nRemote.Sum() is var nRemoteTotal && _nLocal + nRemoteTotal > 0)
            {
                var cancelSource = new CancellationTokenSource();
                _progressBar.Maximum = _nLocal + nRemoteTotal;
                _progressBar.IsIndeterminate = false;

                using (_progressBar
                       .WhenAnyPropertyChanged(nameof(_progressBar.IsAborted))
                       .DistinctUntilChanged()
                       .Where(x => x.IsAborted)
                       .Subscribe(_ => cancelSource.Cancel()))
                {
                    var queryTasks = new List<Task>(2);

                    if (_nLocal > 0)
                        try
                        {
                            queryTasks.Add(QueryLocalCamerasAsync(cancelSource.Token));
                        }
                        catch (Exception e)
                        {
                            Helper.WriteLog(e.Message);
                        }


                    if (nRemoteTotal > 0)
                        try
                        {
                            queryTasks.Add(QueryRemoteCamerasAsync(cancelSource.Token));
                        }
                        catch (Exception e)
                        {
                            Helper.WriteLog(e.Message);
                        }

                    Task.WhenAll(queryTasks);
                }
            }
            
        }

        private async Task QueryLocalCamerasAsync(CancellationToken token)
        {
            // Number of cameras
            
            var workers = new Task[_nLocal];

            // For each found local camera
            for (var camIndex = 0; camIndex < _nLocal; camIndex++)
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
                    {
                        var id = $"localhost:{cam.CameraIndex}:{cam.CameraModel}:{cam.SerialNumber}";
                        if (FoundDevices.Lookup(id).HasValue)
                        {
                            cam.Dispose();
                            cam = null;
                        }
                        else
                            FoundDevices.Edit(context =>
                            {
                                context.AddOrUpdate((Id : id, Camera : cam));
                            });
                    }
                   
                    // Try thread-safely increment progress bar
                    if (!(_progressBar is null))
                        _progressBar.BarComment = cam == null
                            ? Properties.Localization.AvailableCameras_CameraIsUnavailable
                            : string.Format(Properties.Localization.AvailableCameras_AcquiredLocalCamera,
                                ConverterImplementations.CameraToStringAliasConversion(cam));
                }, token);
            }

            await Task.WhenAll(workers);
        }
        private async Task QueryRemoteCamerasAsync(CancellationToken token)
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
                var localIndex = clientIndex;
                workers[clientIndex++] = Task.Run(async () =>
                {
                    try
                    {
                        // For each camera
                        for (var camIndex = 0; camIndex < _nRemote[localIndex]; camIndex++)
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
                            {
                                var id = $"{client.HostAddress}:{cam.CameraIndex}:{cam.CameraModel}:{cam.SerialNumber}";
                                if (FoundDevices.Lookup(id).HasValue)
                                {
                                    cam.Dispose();
                                    cam = null;
                                }
                                else
                                    FoundDevices.Edit(context =>
                                    {
                                        context.AddOrUpdate((Id : id, Camera : cam));
                                    });
                            }

                            // Try increment progress bar
                            if (!(_progressBar is null))
                            {
                                //_progressBar.TryIncrement();
                                _progressBar.BarComment = cam == null
                                    ? Properties.Localization.AvailableCameras_CameraIsUnavailable
                                    : string.Format(Properties.Localization.AvailableCameras_AcquiredRemoteCamera,
                                        ConverterImplementations.CameraToStringAliasConversion(cam));
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
