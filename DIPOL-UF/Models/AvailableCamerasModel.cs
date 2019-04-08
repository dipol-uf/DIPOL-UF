using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Threading;
using System.Threading.Tasks;

using ANDOR_CS.Classes;
using ANDOR_CS.Exceptions;

using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using DIPOL_Remote;
using DIPOL_UF.Converters;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace DIPOL_UF.Models
{
    internal sealed class AvailableCamerasModel : ReactiveObjectEx //-V3073
    {

        private readonly DipolClient[] _remoteClients;

        private readonly SourceCache<(string Id, CameraBase Camera), string> _foundDevices;

        private bool _isClosed;
        private bool _isSelected;

        public DescendantProvider ProgressBarProvider { get; private set; }
        [Reactive]
        public bool IsInteractive { get; private set; }

        public IObservableCache<(string Id, CameraBase Camera), string> FoundCameras { get; private set; }
        public SourceList<string> SelectedIds { get; }

        public ReactiveCommand<Window, Window> WindowContentRenderedCommand { get; private set; }
        public ReactiveCommand<Unit, Unit> QueryCamerasCommand { get; private set; }
        public ReactiveCommand<Window, Unit> CancelButtonCommand { get; private set; }
        public ReactiveCommand<Window, Unit> ConnectButtonCommand { get; private set; }
        public ReactiveCommand<Window, Unit> ConnectAllButtonCommand { get; private set; }
        public ReactiveCommand<Window, Unit> CloseCrossCommand { get; private set; }
        public ReactiveCommand<object, Unit> ClickCommand { get; private set; }

        public AvailableCamerasModel(
            DipolClient[] remoteClients = null)
        {

            _remoteClients = remoteClients;
            IsInteractive = true;
            SelectedIds = new SourceList<string>().DisposeWith(Subscriptions);

            _foundDevices =
                new SourceCache<(string Id, CameraBase Camera), string>(x => x.Id)
                    .DisposeWith(Subscriptions);

            InitializeCommands();
            HookObservables();
            HookValidators();
        }

        private void InitializeCommands()
        {
            var interactivitySrc =
                this.WhenAnyPropertyChanged(nameof(IsInteractive))
                    .Select(x => x.IsInteractive)
                    .ObserveOnUi();

            ClickCommand =
                ReactiveCommand.Create<object>(
                                   ClickCommandExecute,
                                   interactivitySrc.CombineLatest(
                                                       _foundDevices.CountChanged.Select(x => x != 0),
                                                       (x, y) => x && y)
                                                   .ObserveOnUi())
                               .DisposeWith(Subscriptions);

            CloseCrossCommand =
                ReactiveCommand.Create<Window>(
                                   CloseWindow,
                                   interactivitySrc)
                               .DisposeWith(Subscriptions);

            CancelButtonCommand =
                ReactiveCommand.Create<Window>(CancelButtonCommandExecute,
                                   interactivitySrc)
                               .DisposeWith(Subscriptions);

            ConnectAllButtonCommand
                = ReactiveCommand.Create<Window>(ConnectAllButtonCommandExecute,
                                     interactivitySrc.CombineLatest(
                                                         _foundDevices.CountChanged.Select(x => x != 0),
                                                         (x, y) => x && y)
                                                     .ObserveOnUi())
                                 .DisposeWith(Subscriptions);

            ConnectButtonCommand
                = ReactiveCommand.Create<Window>(ConnectButtonCommandExecute,
                                     interactivitySrc.CombineLatest(SelectedIds.CountChanged.Select(x => x != 0),
                                                         (x, y) => x && y)
                                                     .ObserveOnUi())
                                 .DisposeWith(Subscriptions);


            QueryCamerasCommand =
                ReactiveCommand.Create<Unit>(_ => { })
                               .DisposeWith(Subscriptions);

            WindowContentRenderedCommand =
                 ReactiveCommand.Create<Window, Window>(x => x)
                                .DisposeWith(Subscriptions);

            ProgressBarProvider = new DescendantProvider(
                ReactiveCommand.CreateFromTask<object, ReactiveObjectEx>(async x =>
                {
                    var pb = await Task.Run(() =>
                        new ProgressBar()
                        {
                            IsIndeterminate = true,
                            CanAbort = true,
                            DisplayPercents = false,
                            BarTitle = Properties.Localization.AvailableCameras_CheckingConnection_BarTitle
                        });
                    return pb;
                }),
                null, null,
                ReactiveCommand.Create<ReactiveObjectEx>(x => x.Dispose()))
                .DisposeWith(Subscriptions);

            QueryCamerasCommand.InvokeCommand(ProgressBarProvider.ViewRequested)
                                        .DisposeWith(Subscriptions);
        }
        private void HookObservables()
        {
            // Binding source collection to the public read-only interface
            FoundCameras = _foundDevices.AsObservableCache().DisposeWith(Subscriptions);

            _foundDevices.Connect().DisposeManyEx(x =>
            {
                var (id, camera) = x;
                if (!SelectedIds.Items.Contains(id))
                    camera?.Dispose();
            }).Subscribe().DisposeWith(Subscriptions);

            ProgressBarProvider.ViewRequested.Subscribe(async x =>
            {
                await QueryCamerasCommandExecuteAsync((ProgressBar) x);
            }).DisposeWith(Subscriptions);
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
            if(!(_remoteClients is null))
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
#if !DEBUG
                return result.Sum();
            }
            return 0;
#else
            }

            _foundDevices.Edit(context =>
            {
                context.AddOrUpdate(
                    Enumerable.Range(0, 2)
                        .Select(x => DebugCamera.Create(x) as CameraBase)
                        .Select(x => (Id: $"localhost:{x.ToString()}", Camera: x)));
            });

            return _foundDevices.Count;
#endif
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
                        var id = $"localhost;{cam.CameraIndex};{cam.CameraModel};{cam.SerialNumber}";
                        if (_foundDevices.Lookup(id).HasValue)
                        {
                            cam.Dispose();
                            cam = null;
                        }
                        else
                        {
                            _foundDevices.Edit(context => { context.AddOrUpdate((Id: id, Camera: cam)); });
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
                                var id = $"{client.HostAddress};{cam.CameraIndex};{cam.CameraModel};{cam.SerialNumber}";
                                if (_foundDevices.Lookup(id).HasValue)
                                {
                                    cam.Dispose();
                                    cam = null;
                                }
                                else
                                {
                                    _foundDevices.Edit(context => { context.AddOrUpdate((Id: id, Camera: cam)); });
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

        private async Task QueryCamerasCommandExecuteAsync(ProgressBar model)
        {
            var disposable = new CompositeDisposable();
            IsInteractive = false;

            var src = new CancellationTokenSource().DisposeWith(disposable);

            ProgressBarProvider.ViewFinished.Subscribe(_ => src.Cancel()).DisposeWith(disposable);

            var nNewCams = await QueryAvailableCamerasAsync(src.Token, model)
               .ExpectCancellation();

            if (nNewCams == 0)
            {
                model.IsIndeterminate = true;
                model.BarComment = Properties.Localization.AvailableCameras_NoNewCameras;
                await Task.Delay(
                              TimeSpan.Parse(
                                  UiSettingsProvider.Settings.Get("NoCamerasPopUpDelay", "00:00:01.5")),
                              src.Token)
                          .ExpectCancellation();
            }
            else
                await Task.Delay(
                              TimeSpan.Parse(
                                  UiSettingsProvider.Settings.Get("PopUpDelay", "00:00:00.75")),
                              src.Token)
                          .ExpectCancellation();

            if(!ProgressBarProvider.IsDisposed)
                (ProgressBarProvider.ClosingRequested as ICommand).Execute(Unit.Default);

            IsInteractive = true;
            disposable.Dispose();
        }


        private void ConnectButtonCommandExecute(Window param)
        {
            _isSelected = true;
            CloseWindow(param);
        }
        private void ConnectAllButtonCommandExecute(Window param)
        {
            SelectedIds.Edit(context =>
            {
                context.Clear();
                context.AddRange(FoundCameras.Keys);
            });
            _isSelected = true;
            CloseWindow(param);
        }
        private void CancelButtonCommandExecute(Window param)
        {
            _isSelected = false;
            CloseWindow(param);
        }

        private void ClickCommandExecute(object param)
        {
            if (param is List<object> @params
                && @params.Count == 2
                && @params[0] is string id
                && @params[1] is Window window)
            {
                SelectedIds.Edit(context =>
                {
                    context.Clear();
                    context.Add(id);
                });
                _isSelected = true;
                CloseWindow(window);
            }
        }

        private void CloseWindow(Window param)
        {
            if (!_isClosed)
            {
                if (!_isSelected)
                    SelectedIds.Clear();

                _isClosed = true;
                param?.Close();
            }
        }


        public List<(string Id, CameraBase Camera)> RetrieveSelectedDevices()
        {
            var result = new List<(string Id, CameraBase Camera)>();

            _foundDevices.Edit(context =>
            {
                foreach (var id in SelectedIds.Items)
                {
                    var val = context.Lookup(id);
                    if (val.HasValue)
                    {
                        result.Add(val.Value);
                        context.Remove(id);
                    }

                }
            });

            return result;
        }
    }
}
