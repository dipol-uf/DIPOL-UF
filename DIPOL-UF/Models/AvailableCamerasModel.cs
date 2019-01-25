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
using System.Runtime.CompilerServices;
using System.Security.RightsManagement;
using DIPOL_UF.Converters;
using DIPOL_UF.ViewModels;
using DIPOL_UF.Views;
using DynamicData;
using DynamicData.Alias;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace DIPOL_UF.Models
{
    internal sealed class AvailableCamerasModel : ReactiveObjectEx
    {

        private readonly DipolClient[] _remoteClients;

        private readonly SourceCache<(string Id, CameraBase Camera), string> FoundDevices
            = new SourceCache<(string Id, CameraBase Camera), string>(x => x.Id);

        [Reactive]
        public bool IsInteractive { get; private set; }

        public IObservableCache<(string Id, CameraBase Camera), string> FoundCameras { get; private set; }

        public ReactiveCommand<Window, Window> WindowContentRenderedCommand { get; private set; }
        public ReactiveCommand<Window, Unit> WindowClosingCommand { get; private set; }
        public ReactiveCommand<Window, Unit> CancelButtonCommand { get; private set; }
        public ReactiveCommand<Unit, Unit> QueryCamerasCommand { get; private set; }
        public ReactiveCommand<Unit, Unit> ConnectButtonCommand { get; private set; }
        public ReactiveCommand<Unit, Unit> ConnectAllButtonCommand { get; private set; }
        
        
        public AvailableCamerasModel(DipolClient[] remoteClients = null)
        {
            _remoteClients = remoteClients;
            IsInteractive = true;

            FoundDevices.Edit(context =>
            {
                context.AddOrUpdate(
                    new (string Id, CameraBase Camera)[]
                    {
                        ("111111", null),
                        ("222222", null),
                        ("333333", null),
                        ("666666", null),
                        ("444444", null),
                        ("555555", null),
                    });
            });


            InitializeCommands();
            HookValidators();
            HookObservables();
        }

        private void InitializeCommands()
        {
            var interactivitySrc =
                this.WhenAnyPropertyChanged(nameof(IsInteractive))
                    .Select(x => x.IsInteractive)
                    .ObserveOnUi();

            CancelButtonCommand =
                ReactiveCommand.Create<Window>(
                                   x => x?.Close(),
                                   interactivitySrc)
                               .DisposeWith(_subscriptions);

            ConnectAllButtonCommand
                = ReactiveCommand.CreateFromObservable<Unit, Unit>(
                                     _ => Observable.FromAsync(ConnectAllButtonCommandExecuteAsync),
                                     interactivitySrc)
                                 .DisposeWith(_subscriptions);

            ConnectButtonCommand
                = ReactiveCommand.CreateFromObservable<Unit, Unit>(
                                     _ => Observable.FromAsync(ConnectButtonCommandExecuteAsync),
                                     interactivitySrc)
                                 .DisposeWith(_subscriptions);


            QueryCamerasCommand =
                ReactiveCommand.CreateFromObservable<Unit, Unit>(
                                  _ => Observable.FromAsync(QueryCamerasCommandExecuteAsync))
                               .DisposeWith(_subscriptions);

            WindowClosingCommand =
                ReactiveCommand.Create<Window>(
                                   WindowClosingCommandExecute)
                               .DisposeWith(_subscriptions);

            WindowContentRenderedCommand =
                ReactiveCommand.Create<Window, Window>(x => x)
                               .DisposeWith(_subscriptions);
        }

        private async Task<int> QueryAvailableCamerasAsync(CancellationToken cancelToken, ProgressBar pb)
        {
            var nLocal = 0;
            var nRemote = new List<int>();

            try
            {
                nLocal = await Helper.RunNoMarshall(Camera.GetNumberOfCameras);
            }
            catch (AndorSdkException aExp)
            {
                Helper.WriteLog(aExp);
            }

            foreach (var client in _remoteClients)
                try
                {
                    nRemote.Add(await Helper.RunNoMarshall(() => client?.GetNumberOfCameras() ?? 0));
                }
                catch (Exception e)
                {
                    Helper.WriteLog(e.Message);
                }

            cancelToken.ThrowIfCancellationRequested();

            if (nRemote.Sum() is var nRemoteTotal && nLocal + nRemoteTotal > 0)
            {
                pb.IsIndeterminate = false;
                pb.Maximum = nLocal + nRemoteTotal;
                var queryTasks = new List<Task<int>>(2);

                if (nLocal > 0)
                    try
                    {
                        queryTasks.Add(QueryLocalCamerasAsync(nLocal, cancelToken, pb));
                    }
                    catch (Exception e)
                    {
                        Helper.WriteLog(e.Message);
                    }


                if (nRemoteTotal > 0)
                    try
                    {
                        queryTasks.Add(QueryRemoteCamerasAsync(nRemote, cancelToken, pb));
                    }
                    catch (Exception e)
                    {
                        Helper.WriteLog(e.Message);
                    }

                var result = await Task.WhenAll(queryTasks).ExpectCancellation();

                return result.Sum();
            }

            return 0;
        }
        private async Task<int> QueryLocalCamerasAsync(int nLocal, CancellationToken token, ProgressBar pb)
        {
            // Number of cameras
            var counter = 0;
            var workers = new Task[nLocal];
            // For each found local camera
            for (var camIndex = 0; camIndex < nLocal; camIndex++)
            {
                // Check cancellation request
                token.ThrowIfCancellationRequested();
                var index = camIndex;
                workers[camIndex] = Task.Run(async () =>
                {
                    // If camera is nor present on the active list
                    CameraBase cam = null;
                    try
                    {
                        cam = await Camera.CreateAsync(index).ConfigureAwait(false);
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
                        token.ThrowIfCancellationRequested();
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
                        {
                            FoundDevices.Edit(context => { context.AddOrUpdate((Id: id, Camera: cam)); });
                            Interlocked.Increment(ref counter);
                        }
                    }

                    // Try thread-safely increment progress bar
                    if (!(pb is null) && pb.TryIncrement())
                        pb.BarComment = cam == null
                            ? Properties.Localization.AvailableCameras_CameraIsUnavailable
                            : string.Format(Properties.Localization.AvailableCameras_AcquiredLocalCamera,
                                ConverterImplementations.CameraToStringAliasConversion(cam));
                }, token);
            }

            await Task.WhenAll(workers).ExpectCancellation();

            return counter;
        }
        private async Task<int> QueryRemoteCamerasAsync(IReadOnlyList<int> nRemote, CancellationToken token, ProgressBar pb)
        {
            var counter = 0;
            var workers = new Task[_remoteClients.Length];
            var clientIndex = 0;
            // For each remote client
            foreach (var client in _remoteClients)
            {
                // Checks if cancellation is requested
                token.ThrowIfCancellationRequested();
                // Runs task in parallel
                var localIndex = clientIndex;
                workers[clientIndex++] = Task.Run(async () =>
                {
                    try
                    {
                        // For each camera
                        for (var camIndex = 0; camIndex < nRemote[localIndex]; camIndex++)
                        {
                            // If cancellation requested
                            token.ThrowIfCancellationRequested();

                            // Try to create remote camera
                            CameraBase cam = null;
                            try
                            {
                                if (!client.ActiveRemoteCameras().Contains(camIndex))
                                    //cam = client.CreateRemoteCamera(camIndex);  
                                    cam = await RemoteCamera.CreateAsync(camIndex, client)
                                                            .ConfigureAwait(false);
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
                                token.ThrowIfCancellationRequested();
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
                                {
                                    FoundDevices.Edit(context => { context.AddOrUpdate((Id: id, Camera: cam)); });
                                    Interlocked.Increment(ref counter);
                                }
                            }

                            // Try increment progress bar
                            if (!(pb is null) && pb.TryIncrement())
                                pb.BarComment = cam == null
                                    ? Properties.Localization.AvailableCameras_CameraIsUnavailable
                                    : string.Format(Properties.Localization.AvailableCameras_AcquiredRemoteCamera,
                                        ConverterImplementations.CameraToStringAliasConversion(cam));
                            
                        }
                    }
                    catch (Exception e)
                    {
                        Helper.WriteLog(e);
                    }
                }, token);
            }

            await Task.WhenAll(workers).ExpectCancellation();

            return counter;
        }

        private async Task QueryCamerasCommandExecuteAsync(CancellationToken token)
        {
            var disposables = new CompositeDisposable();

            var pb = await Helper.RunNoMarshall(() =>
                new ProgressBar()
                {
                    IsIndeterminate = true,
                    CanAbort = true,
                    DisplayPercents = false,
                    BarTitle = Properties.Localization.AvailableCameras_CheckingConnection_BarTitle
                }.DisposeWith(disposables));

            var viewModel = await Helper.RunNoMarshall(() =>
                new ProgressBarViewModel(pb).DisposeWith(disposables));


            var linkedSrc = CancellationTokenSource
                .CreateLinkedTokenSource(token)
                .DisposeWith(disposables);

            await Helper.RunNoMarshall(() =>
                pb.CancelCommand
                  .Subscribe(_ =>
                  {
                      if(!disposables.IsDisposed)
                         linkedSrc.Cancel();
                  })
                  .DisposeWith(disposables));

            IsInteractive = false;
            var view = Helper.ExecuteOnUi(() => new ProgressWindow()
            {
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            }.WithDataContext(viewModel));


            WindowContentRenderedCommand.Subscribe(x =>
            {
                view.Owner = x;
                Helper.ExecuteOnUI(() => view.ShowDialog());
            }).DisposeWith(disposables);

            var nNewCams = await QueryAvailableCamerasAsync(linkedSrc.Token, pb)
               .ExpectCancellation();

            if (nNewCams == 0)
            {
                pb.IsIndeterminate = true;
                pb.BarComment = Properties.Localization.AvailableCameras_NoNewCameras;
                await Task.Delay(
                              TimeSpan.Parse(
                                  UiSettingsProvider.Settings.Get("NoCamerasPopUpDelay", "00:00:01.5")),
                              linkedSrc.Token)
                          .ExpectCancellation();
            }
            else
                await Task.Delay(
                              TimeSpan.Parse(
                                  UiSettingsProvider.Settings.Get("PopUpDelay", "00:00:00.75")),
                              linkedSrc.Token)
                          .ExpectCancellation();

            Helper.ExecuteOnUi(view.Close);

            await Helper.RunNoMarshall(disposables.Dispose);
            IsInteractive = true;
        }

        private async Task ConnectAllButtonCommandExecuteAsync()
        {
            await Task.Delay(1);
        }

        private async Task ConnectButtonCommandExecuteAsync()
        {
            await Task.Delay(1);
        }

        private void WindowClosingCommandExecute(Window window)
        {
            //WindowShownCommand.Dispose();
            //Parallel.ForEach(FoundCameras.Where(item => !SelectedItems.Contains(item.Key)), (item) => item.Value?.Dispose());
            //_selectedCameras = FoundCameras.Join(SelectedItems, x => x.Key, y => y, (x, y) => x).ToList();
            //FoundCameras.Clear();
            //SelectedItems.Clear();

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

        private void HookObservables()
        {
            FoundCameras = FoundDevices.AsObservableCache();
        }

    }
}
