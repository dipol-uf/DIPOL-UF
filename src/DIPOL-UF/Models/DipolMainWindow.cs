﻿using ANDOR_CS.Enums;
using DIPOL_Remote;
using DIPOL_UF.ViewModels;
using DIPOL_UF.Views;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO.Ports;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ANDOR_CS;
using DIPOL_UF.Enums;
using DIPOL_UF.Jobs;
using DIPOL_UF.UserNotifications;
using Microsoft.Extensions.Logging;
using StepMotor;
using Exception = System.Exception;
using Localization = DIPOL_UF.Properties.Localization;
using MessageBox = System.Windows.MessageBox;

namespace DIPOL_UF.Models
{
    internal sealed class DipolMainWindow : ReactiveObjectEx
    {
        private readonly IAsyncMotorFactory _motorFactory;
        private readonly IControlClientFactory _clientsFactory;

        private readonly string[] _remoteLocations
            = UiSettingsProvider.Settings.GetArray<string>("RemoteLocations")
              ?? Array.Empty<string>();

        private ImmutableArray<IControlClient> _remoteClients;

        private readonly SourceCache<(string Id, IDevice Camera), string> _connectedCameras;

        private SerialPort _polarimeterPort;
        private SerialPort _retractorPort;

        private Task _polarimeterPortScanningTask;
        private Task _retractorPortScanningTask;

        private readonly IUserNotifier _notifier;
        private readonly ILogger _logger;

        [Reactive]
        private bool PolarimeterMotorTaskCompleted { get; set; }
        [Reactive]
        private bool RetractorMotorTaskCompleted { get; set; }

        [Reactive]
        public bool IsSwitchingRegimes { get; private set; }

        [Reactive]
        public InstrumentRegime Regime { get; private set; } = InstrumentRegime.Unknown;

        [Reactive] 
        public bool WasCalibrated { get; private set; }

        [Reactive]
        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        public IAsyncMotor PolarimeterMotor { get; private set; }
        [Reactive]
        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        public IAsyncMotor RetractorMotor { get; private set; }

        public DescendantProvider ProgressBarProvider { get; private set; }
        public DescendantProvider AvailableCamerasProvider { get; private set; }
        public DescendantProvider RegimeSwitchProvider { get; private set; }

        public bool IsDisposing { get; private set; }

        // ReSharper disable UnassignedGetOnlyAutoProperty
        public bool CanConnect { [ObservableAsProperty] get; }
        // ReSharper restore UnassignedGetOnlyAutoProperty

        public SourceList<string> SelectedDevices { get; }
        public IObservableCache<(string Id, IDevice Camera), string> ConnectedCameras { get; private set; }
        public IObservableCache<(string Id, CameraTab Tab), string> CameraTabs { get; private set; }

        public ReactiveCommand<Unit, Unit> WindowLoadedCommand { get; private set; }

        public ReactiveCommand<object, AvailableCamerasModel> ConnectButtonCommand { get; private set; }
        public ReactiveCommand<Unit, Unit> DisconnectButtonCommand { get; private set; }
        public ReactiveCommand<Unit, Unit> SelectAllCamerasCommand { get; private set; }
        public ReactiveCommand<string, Unit> SelectCameraCommand { get; private set; }
        public ReactiveCommand<string, Unit> ContextMenuCommand { get; private set; }

        public ReactiveCommand<Unit, Unit> PolarimeterMotorButtonCommand { get; private set; }
        public ReactiveCommand<Unit, Unit> RetractorMotorButtonCommand { get; private set; }

        public ReactiveCommand<InstrumentRegime, ProgressBar> ChangeRegimeCommand { get; private set; }

        public DipolMainWindow(
            IUserNotifier notifier, 
            ILogger<DipolMainWindow> logger,
            IAsyncMotorFactory motorFactory,
            IControlClientFactory clientsFactory
        )
        {
            _motorFactory = motorFactory ?? throw new ArgumentNullException(nameof(motorFactory));
            _clientsFactory = clientsFactory ?? throw new ArgumentNullException(nameof(clientsFactory));
            _notifier = notifier ?? throw new ArgumentNullException(nameof(notifier));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _polarimeterPortScanningTask = CheckPolarimeterMotor();
            _retractorPortScanningTask = CheckRetractorMotor();

            _connectedCameras = new SourceCache<(string Id, IDevice Camera), string>(x => x.Id)
                .DisposeWith(Subscriptions);

            SelectedDevices = new SourceList<string>()
                .DisposeWith(Subscriptions);

            InitializeCommands();
            HookObservables();
            HookValidators();
            JobManager.Manager.AttachToMainWindow(this);
        }

        private void SetupRegime()
        {
            if (RetractorMotor != null)
            {
                Regime = PolarimeterMotor is null
                        ? InstrumentRegime.Unknown
                        : InstrumentRegime.Polarimeter;
            }
        }

        private async Task CheckPolarimeterMotor()
        {
            Application.Current?.Dispatcher.InvokeAsync(() => PolarimeterMotorTaskCompleted = false);
            IAsyncMotor motor = null;
            try
            {
                if (PolarimeterMotor is { })
                {
                    PolarimeterMotor.Dispose();
                    PolarimeterMotor = null;
                }

                _polarimeterPort?.Dispose();
                _polarimeterPort = new SerialPort(
                    UiSettingsProvider.Settings
                                      .Get(@"PolarimeterMotorComPort", "COM1").ToUpperInvariant()
                );
                motor = await _motorFactory.CreateFirstOrFromAddress(_polarimeterPort, 1);
                if (motor is null)
                {
                    throw new NullReferenceException();
                }

                await motor.ReferenceReturnToOriginAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Polarimeter motor has failed");
            }
            finally
            {
                PolarimeterMotor = motor;
                SetupRegime();
                Application.Current?.Dispatcher.InvokeAsync(() => PolarimeterMotorTaskCompleted = true);
            }
        }

        private async Task CheckRetractorMotor()
        {

            RetractorMotorTaskCompleted = false;
            IAsyncMotor motor = null;
            try
            {
                if (RetractorMotor is { })
                {
                    RetractorMotor.Dispose();
                    RetractorMotor = null;
                }

                _retractorPort?.Dispose();
                _retractorPort = new SerialPort(
                    UiSettingsProvider.Settings
                                      .Get(@"RetractorMotorComPort", "COM4").ToUpperInvariant()
                );

                motor = await _motorFactory.CreateFirstOrFromAddress(_retractorPort, 1);
                if (motor is null)
                {
                    throw new NullReferenceException();
                }

                await motor.ReturnToOriginAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Retractor motor has failed");
            }
            finally
            {
                RetractorMotor = motor;
                SetupRegime();
                RetractorMotorTaskCompleted = true;
                WasCalibrated = false;
            }
        }

        private void InitializeCommands()
        {

            ChangeRegimeCommand =
                ReactiveCommand.CreateFromTask<InstrumentRegime, ProgressBar>(
                        ChangeRegimeCommandExecute,
                        this
                            .WhenPropertyChanged(x => x.RetractorMotor)
                            .CombineLatest(
                                this.WhenPropertyChanged(y => y.PolarimeterMotor), 
                                (x, y) => x is {} && y is {}))
                    .DisposeWith(Subscriptions);

            ContextMenuCommand =
                ReactiveCommand.CreateFromObservable<string, Unit>(
                                   x => Observable.FromAsync(_ => ContextMenuCommandExecuteAsync(x)),
                                   _connectedCameras.CountChanged.Select(x => x != 0)
                                                    .DistinctUntilChanged()
                                                    .ObserveOnUi())
                               .DisposeWith(Subscriptions);

            SelectCameraCommand =
                ReactiveCommand.Create<string>(
                                   SelectCameraCommandExecute,
                                   _connectedCameras.CountChanged.Select(x => x != 0)
                                                    .DistinctUntilChanged()
                                                    .ObserveOnUi())
                               .DisposeWith(Subscriptions);

            WindowLoadedCommand =
                ReactiveCommand.Create<Unit, Unit>(_ => Unit.Default)
                               .DisposeWith(Subscriptions);

            ConnectButtonCommand =
                ReactiveCommand.Create<object, AvailableCamerasModel>(
                                   _ =>
                                   {
                                       var camQueryModel = new AvailableCamerasModel(_remoteClients);

                                       return camQueryModel;
                                   },
                                   this.WhenAnyPropertyChanged(nameof(CanConnect))
                                       .Select(x => x.CanConnect)
                                       .DistinctUntilChanged()
                                       .ObserveOnUi())
                               .DisposeWith(Subscriptions);

            DisconnectButtonCommand =
                ReactiveCommand.Create(
                                   DisconnectButtonCommandExecute,
                                   SelectedDevices.CountChanged.Select(x => x != 0)
                                                  .DistinctUntilChanged()
                                                  .ObserveOnUi())
                               .DisposeWith(Subscriptions);

            SelectAllCamerasCommand =
                ReactiveCommand.Create(
                                   SelectAllCamerasCommandExecute,
                                   _connectedCameras.CountChanged.Select(x => x != 0)
                                                    .DistinctUntilChanged()
                                                    .ObserveOnUi())
                               .DisposeWith(Subscriptions);

            ProgressBarProvider = new DescendantProvider(
                    ReactiveCommand.Create<object, ReactiveObjectEx>(x => (ReactiveObjectEx)x),
                    null,
                    null,
                    ReactiveCommand.Create<ReactiveObjectEx>(x => x.Dispose()))
                .DisposeWith(Subscriptions);

            RegimeSwitchProvider = new DescendantProvider(
                    ReactiveCommand.Create<object, ReactiveObjectEx>(x => (ReactiveObjectEx)x),
                    null,
                    null,
                    ReactiveCommand.Create<ReactiveObjectEx>(x => x.Dispose()))
                .DisposeWith(Subscriptions);

            AvailableCamerasProvider = new DescendantProvider(
                    ReactiveCommand.Create<object, ReactiveObjectEx>(x => (ReactiveObjectEx) x),
                    null,
                    null,
                    ReactiveCommand.CreateFromTask<ReactiveObjectEx>(async x =>
                    {
                        await ReceiveConnectedCameras((AvailableCamerasModel) x).ExpectCancellation();
                        x.Dispose();
                    }))
                .DisposeWith(Subscriptions);



            PolarimeterMotorButtonCommand =
                ReactiveCommand.CreateFromTask(CheckPolarimeterMotorStatus,
                                   this.WhenPropertyChanged(x => x.PolarimeterMotorTaskCompleted)
                                       .Select(x => x.Value))
                               .DisposeWith(Subscriptions);

            RetractorMotorButtonCommand =
                ReactiveCommand.CreateFromTask(CheckRetractorMotorStatus,
                        this.WhenPropertyChanged(x => x.RetractorMotorTaskCompleted)
                            .Select(x => x.Value))
                    .DisposeWith(Subscriptions);

            RegimeSwitchProvider.ViewFinished.Select(_ =>NotifyIfCalibrationHappened()).SubscribeDispose(Subscriptions);
        }

        private async Task NotifyIfCalibrationHappened()
        {
            var pos = await (RetractorMotor?.GetActualPositionAsync() ?? Task.FromResult(0));
            var polMode = UiSettingsProvider.Settings.Get("RetractorPositionPolarimetry", 0);
            var delta = UiSettingsProvider.Settings.Get(@"RetractorPositionCorrection", 15000);
            var wasCalibrated = Math.Abs(pos - polMode) > 2 * delta && Regime is InstrumentRegime.Polarimeter;

            if (wasCalibrated)
            {
                _notifier.Error(
                    Localization.RegimeCalibration_Restart_Caption, 
                    Localization.RegimeCalibration_Restart_Text
                );
            }
        }
        
        private void HookObservables()
        {
            Observable.Merge(
                          ProgressBarProvider.ViewRequested.Select(_ => false),
                          ProgressBarProvider.ViewFinished.Select(_ => true),
                          AvailableCamerasProvider.ViewRequested.Select(_ => false),
                          AvailableCamerasProvider.ViewFinished.Select(_ => true))
                      .ToPropertyEx(this, x => x.CanConnect)
                      .DisposeWith(Subscriptions);


            _connectedCameras.Connect()
                             .DisposeManyEx(async x => await DisposeCamera(x.Camera))
                             .Subscribe()
                             .DisposeWith(Subscriptions);

            ConnectedCameras = _connectedCameras
                .Connect()
                .Sort(CameraTupleOrderComparer.Default, SortOptimisations.None, 1)
                .AsObservableCache()
                .DisposeWith(Subscriptions);



            CameraTabs = _connectedCameras.Connect()
                .Sort(CameraTupleOrderComparer.Default, SortOptimisations.None, 1)
                .Transform(x => (x.Id, Tab: new CameraTab(x.Camera)))
                .DisposeManyEx(x => x.Tab?.Dispose())
                .AsObservableCache()
                .DisposeWith(Subscriptions);

            WindowLoadedCommand
                .Select(_ => new ProgressBar()
                {
                    Minimum = 0,
                    Maximum = _remoteClients.IsDefaultOrEmpty ? 1 : _remoteClients.Length,
                    Value = 0,
                    IsIndeterminate = true,
                    CanAbort = false,
                    BarTitle = Localization.MainWindow_ConnectingToRemoteLocations
                } as object).InvokeCommand(ProgressBarProvider.ViewRequested)
                .DisposeWith(Subscriptions);

            ProgressBarProvider.ViewRequested.Select(x =>
                    Observable.FromAsync(async () => await InitializeRemoteSessionsAsync((ProgressBar) x)))
                .Merge()
                .CombineLatest(ProgressBarProvider.WindowShown,
                    (x, _) => x)
                .Delay(TimeSpan.Parse(UiSettingsProvider.Settings.Get("PopUpDelay", "00:00:00.750")))
                .Subscribe(async x =>
                {
                    await ProgressBarProvider.ClosingRequested.Execute();

                    foreach (var ex in x.Take(3))
                    {
                        Helper.ExecuteOnUi(() => MessageBox.Show(
                            ex.Message,
                            Localization.RemoteConnection_UnreachableHostTitle,
                            MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK));
                    }
                    if (x.Count > 3)
                        Helper.ExecuteOnUi(() => MessageBox.Show(
                            string.Format(Localization.MB_MoreLeft, x.Count - 3),
                            Localization.RemoteConnection_UnreachableHostTitle,
                            MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK));
                })
                .DisposeWith(Subscriptions);

            ConnectButtonCommand.InvokeCommand(AvailableCamerasProvider.ViewRequested as ICommand)
                                .DisposeWith(Subscriptions);

            AvailableCamerasProvider.WindowShown.WithLatestFrom(
                                        AvailableCamerasProvider.ViewRequested,
                                        (_, y) => y)
                                    .Subscribe(async x =>
                                        await QueryCamerasAsync((AvailableCamerasModel) x)
                                            .ExpectCancellation())
                                    .DisposeWith(Subscriptions);

            ChangeRegimeCommand.Select(x => x as object).InvokeCommand(RegimeSwitchProvider.ViewRequested)
                .DisposeWith(Subscriptions);

        }

        private void SelectAllCamerasCommandExecute()
        {
            if (SelectedDevices.Count < ConnectedCameras.Count)
                SelectedDevices.Edit(context =>
                {
                    context.Clear();
                    context.AddRange(ConnectedCameras.Keys);
                });
            else
                SelectedDevices.Clear();
        }

        private void SelectCameraCommandExecute(string param)
        {
            SelectedDevices.Edit(context =>
            {
                if (context.Contains(param))
                    context.Remove(param);
                else
                    context.Add(param);
            });
        }

        private async Task ContextMenuCommandExecuteAsync(string param)
        {
            var result = await Helper.RunNoMarshall(() => ConnectedCameras.Lookup(param));
            if (result.HasValue)
            {
                var vm = await Helper.RunNoMarshall(() => new CameraPropertiesViewModel(result.Value.Camera));
                var view = Helper.ExecuteOnUi(() => new CameraPropertiesView()
                        {WindowStartupLocation = WindowStartupLocation.CenterScreen}
                    .WithDataContext(vm));

                Helper.ExecuteOnUi(view.ShowDialog);

                await Helper.RunNoMarshall(vm.Dispose);
            }
        }

        private async Task<List<Exception>> InitializeRemoteSessionsAsync(ProgressBar pb)
        {

            var tasks = _remoteLocations.Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => Task.Run(() =>
                {

                    try
                    {
                        var uri = new Uri(x);
                        _logger.LogInformation("Establishing connection to {Uri}", uri);
                        var client = _clientsFactory.Create(
                            uri,
                            TimeSpan.Parse(UiSettingsProvider.Settings.Get("RemoteOpenTimeout", "00:00:30")),
                            TimeSpan.Parse(UiSettingsProvider.Settings.Get("RemoteSendTimeout", "00:00:30")),
                            TimeSpan.Parse(UiSettingsProvider.Settings.Get("RemoteCloseTimeout", "00:00:30")));
                        client.Connect();

                        pb.TryIncrement();
                        pb.BarComment = string.Format(Localization.MainWindow_RemoteConnection_ClientCount,
                            pb.Value, _remoteLocations.Length);

                        _logger.LogInformation("Connection to {Uri} established", uri);
                        return client;
                    }
                    catch (Exception e)
                    {
                        _logger.LogWarning(@"Failed to connect to {Target}", x);
                        return (object)e;
                    }

                }));


            var result = await Task.WhenAll(tasks).ConfigureAwait(false);
            _remoteClients = result.OfType<IControlClient>().ToImmutableArray();

            var exceptions = result.OfType<Exception>().ToList();

             pb.BarComment = string.Format(Localization.MainWindow_RemoteConnection_ClientCount,
                _remoteClients.Length, _remoteLocations.Length);

             return exceptions;
        }

        private async Task ReceiveConnectedCameras(AvailableCamerasModel model)
        {
            var cams = model.RetrieveSelectedDevices();

            if (cams.Length > 0)
                _connectedCameras.Edit(context => { context.AddOrUpdate(cams); });

            await PrepareCamerasAsync(cams.Select(x => x.Camera));
        }

        private async Task CheckPolarimeterMotorStatus()
        {
            var pos = 0;
            try
            {
                pos = await (PolarimeterMotor?.GetActualPositionAsync() ?? Task.FromResult(0));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stepper motor failed to respond");
                PolarimeterMotor?.Dispose();
                PolarimeterMotor = null;
            }
            if (PolarimeterMotor is not null)
            {
                MessageBox.Show(
                    string.Format(Localization.MainWindow_MB_PolarimeterMotorOK_Text,
                        _polarimeterPort.PortName,
                        PolarimeterMotor.Address,
                        pos),
                    Localization.MainWindow_MB_PolarimeterMotorOK_Caption,
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                if (_polarimeterPortScanningTask is not null
                    && !_polarimeterPortScanningTask.IsFaulted)
                {
                    var response = MessageBox.Show(
                        string.Format(Localization.MainWindow_MB_PolarimeterMotorNotFound_Text,
                            UiSettingsProvider.Settings.Get(@"PolarimeterMotorComPort", "COM1").ToUpperInvariant()),
                        Localization.MainWindow_MB_PolarimeterMotorNotFound_Caption,
                        MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (response == MessageBoxResult.Yes)
                    {

                        _logger.LogInformation("Stepper motor re-scanning requested");
                        _polarimeterPortScanningTask = CheckPolarimeterMotor();
                    }
                }
                else
                {
                    MessageBox.Show(
                        Localization.MainWindow_MB_PolarimeterMotorFailure_Text,
                        Localization.MainWindow_MB_PolarimeterMotorFailure_Caption,
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task CheckRetractorMotorStatus()
        {
            var pos = 0;
            var isCloseToPolarimetry = true;
            try
            {
                pos = await (RetractorMotor?.GetActualPositionAsync() ?? Task.FromResult(0));
                var polMode = UiSettingsProvider.Settings.Get("RetractorPositionPolarimetry", 0);
                var delta = UiSettingsProvider.Settings.Get(@"RetractorPositionCorrection", 15000);
                isCloseToPolarimetry &= Math.Abs(pos - polMode) < 2 * delta;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Retractor motor failed to respond");

                RetractorMotor?.Dispose();
                RetractorMotor = null;
            }

            switch (RetractorMotor)
            {
                case { } motor when
                    !JobManager.Manager.AnyCameraIsAcquiring &&
                    !JobManager.Manager.IsInProcess &&
                    !IsSwitchingRegimes &&
                    Regime is InstrumentRegime.Polarimeter &&
                    isCloseToPolarimetry:
                {
                    WasCalibrated = false;
                    var response = _notifier.YesNo(
                        Localization.MainWindow_MB_PolarimeterMotorOK_Caption,
                        string.Format(
                            Localization.MainWindow_MB_PolarimeterMotorOK_Text_2,
                            _retractorPort.PortName,
                            motor.Address,
                            pos
                        )
                    );

                    if(response is YesNoResult.Yes)
                    {
                        _logger.LogInformation("Retractor motor recalibration requested");
                        await ChangeRegimeCommand.Execute(Regime);
                    }

                    break;
                }

                case { } when Regime is InstrumentRegime.Polarimeter && !isCloseToPolarimetry:
                    WasCalibrated = true;
                    _notifier.Error(
                        Localization.RegimeCalibration_Restart_Caption,
                        Localization.RegimeCalibration_Restart_Text
                    );
                    break;
                case { } motor:
                    _notifier.Info(
                        Localization.MainWindow_MB_PolarimeterMotorOK_Caption,
                        string.Format(
                            Localization.MainWindow_MB_PolarimeterMotorOK_Text,
                            _retractorPort.PortName,
                            motor.Address,
                            pos
                        )
                    );
                    break;
                default:
                    if (_retractorPortScanningTask is {IsFaulted: false})
                    {

                        var response = _notifier.YesNoWarning(
                            Localization.MainWindow_MB_PolarimeterMotorNotFound_Caption,
                            string.Format(
                                Localization.MainWindow_MB_PolarimeterMotorNotFound_Text,
                                UiSettingsProvider.Settings.Get(@"RetractorMotorComPort", "COM4").ToUpperInvariant()
                            )
                        );

                        if (response is YesNoResult.Yes)
                        {
                            _logger.LogInformation("Retractor motor re-scanning requested");
                            _retractorPortScanningTask = CheckRetractorMotor();
                        }
                    }
                    else
                    {
                        _notifier.Error(
                            Localization.MainWindow_MB_PolarimeterMotorFailure_Caption,
                            Localization.MainWindow_MB_PolarimeterMotorFailure_Text
                        );
                    }

                    break;
            }

        }

        private void DisconnectButtonCommandExecute()
        {
            foreach (var id in SelectedDevices.Items.ToList())
            {
                SelectedDevices.Remove(id);
                _connectedCameras.RemoveKey(id);
            }
        }

        private async Task<ProgressBar> ChangeRegimeCommandExecute(InstrumentRegime newRegime)
        {
            IsSwitchingRegimes = true;

            if (newRegime is InstrumentRegime.Unknown || RetractorMotor is null || PolarimeterMotor is null)
            {
                throw new ArgumentException(nameof(newRegime));
            }

            Progress<(int Current, int Target)> progress;
            string pbText;
            int pos;
            int target;
            // By default is +450_000
            var posOffset = UiSettingsProvider.Settings.Get(@"RetractorPositionPolarimetry", 0) -
                            UiSettingsProvider.Settings.Get(@"RetractorPositionPhotometry", -450_000);

            var oldRegime = Regime;
            var newAbsPos = (oldRegime, param: newRegime) switch
            {
                // By default, goes to (pos + 450_000)
                (InstrumentRegime.Polarimeter, InstrumentRegime.Polarimeter) => UiSettingsProvider.Settings.Get(
                    @"RetractorPositionPolarimetry", 0
                ) + posOffset,
                (_, InstrumentRegime.Polarimeter) => UiSettingsProvider.Settings.Get(
                    @"RetractorPositionPolarimetry", 0
                ),
                // By default, goes to (pos - 450_000)
                (_, InstrumentRegime.Photometer) =>
                    UiSettingsProvider.Settings.Get(@"RetractorPositionPolarimetry", 0) - posOffset,
                _ => throw new ArgumentException(nameof(newRegime))
            };
            var isCalibration = oldRegime is InstrumentRegime.Polarimeter && newRegime is InstrumentRegime.Polarimeter;

            if (isCalibration)
            {
                // Going for an extra 15_000 to compensate for backtracking in the worst-case scenario
                newAbsPos += UiSettingsProvider.Settings.Get(@"RetractorPositionCorrection", 15_000);
            }
            
            try
            {
                pbText =
                    isCalibration
                        ? Localization.MainWindow_Regime_Switching_Calibration_Text
                        : string.Format(
                            Localization.MainWindow_Regime_Switching_Text,
                            Regime.ToStringEx(),
                            newRegime.ToStringEx()
                        );
                Regime = InstrumentRegime.Unknown;
                progress = new Progress<(int Current, int Target)>();

                pos = await RetractorMotor.GetActualPositionAsync();
                if (pos == newAbsPos)
                {
                    _logger.LogInformation("Final position reached, cannot perform calibration");
                }


                _logger.LogInformation(
                    @"Retractor at {Pos}, rotating to {Regime} by {newPos}",
                    pos,
                    newRegime,
                    newAbsPos
                );
                // Now moving relatively
                var reply = await RetractorMotor.MoveToPosition(newAbsPos);
                if (reply is not {Status: ReturnStatus.Success})
                {
                    throw new InvalidOperationException("Failed to operate retractor.");
                }

                ImmutableDictionary<AxisParameter, int> axis = await RetractorMotor.GetRotationStatusAsync();
                target = axis[AxisParameter.TargetPosition];

            }
            catch (Exception)
            {
                IsSwitchingRegimes = false;
                throw;
            }

            ContinueAfterRegimeSwitched(progress, isCalibration, posOffset, newRegime);

            var pb = new ProgressBar
            {
                Minimum = 0,
                Maximum = Math.Abs(target - pos),
                DisplayPercents = true,
                BarComment = pbText,
                BarTitle = Localization.MainWindow_Regime_Swtitching_Title
            };
            progress.ProgressChanged += (_, e) => pb.Value = Math.Abs(e.Current - pos);

            return pb;


        }

        private async void ContinueAfterRegimeSwitched(
            IProgress<(int Current, int Target)> progress,
            bool isCalibration,
            int posOffset,
            InstrumentRegime newRegime
        )
        {
            await RetractorMotor.WaitForPositionReachedAsync(progress);
            try
            {
                var reachedPos = await RetractorMotor.GetActualPositionAsync();
                _logger.LogInformation("Retractor reached position {pos}", reachedPos);

                // This is re-calibration, need to backtrack
                if (isCalibration)
                {
                    // We need to rotate in the opposite of what calibration did, so
                    // take `- sign(newRelativePos)` and multiply by the backtracking delta
                    var backtrackDelta = UiSettingsProvider.Settings.Get(@"RetractorPositionCorrection", 15000) *
                                         Math.Sign(posOffset) * newRegime switch
                                         {
                                             InstrumentRegime.Polarimeter => -1,
                                             InstrumentRegime.Photometer => 1,
                                             _ => 0
                                         };

                    _logger.LogInformation(
                        "Detected re-calibration, backtracking by {BacktrackDelta}",
                        backtrackDelta
                    );

                    if (await RetractorMotor.MoveToPosition(backtrackDelta, CommandType.Relative) is not
                        {Status: ReturnStatus.Success})
                    {
                        throw new InvalidOperationException("Backtracking has failed");
                    }

                    await RetractorMotor.WaitForPositionReachedAsync();
                    reachedPos = await RetractorMotor.GetActualPositionAsync();
                    _logger.LogInformation("Retractor reached position {pos}", reachedPos);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Regime switching has failed");
            }

            await RegimeSwitchProvider.ClosingRequested.Execute();

            Regime = newRegime;

            IsSwitchingRegimes = false;

            if (isCalibration)
            {
                WasCalibrated = true;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed || IsDisposing || !disposing) return;
            // This one overrides on [Disconnect] disposal,
            // as it is "fire & forget", and time-consuming.
            // When window is closed, the unobserved disposals take some time
            // and can use client infrastructure when client has been disconnected.
            // [IsDisposing] overrides that behaviour, and each camera is removed & disposed
            // individually and synchronously.

            IsDisposing = true;
            PolarimeterMotor?.Dispose();
            RetractorMotor?.Dispose();
            _polarimeterPort?.Dispose();
            _retractorPort?.Dispose();
                    
            var keys = _connectedCameras.Keys.ToList();
            foreach (var val in 
                keys
                    .Select(key => _connectedCameras.Lookup(key))
                    .Where(val => val.HasValue))
            {
                _connectedCameras.Remove(val.Value.Id);
                if(!val.Value.Camera.IsDisposed)
                    val.Value.Camera.Dispose();
            }

            if (!(_remoteClients.IsEmpty))
                Parallel.ForEach(_remoteClients, (client) =>
                {
                    try
                    {
                        client?.Disconnect();
                        client?.Dispose();
                    }
                    catch (Exception)
                    {
                        // TODO : may be important
                        // Ignored
                    }
                });
            IsDisposing = false;
            base.Dispose(true);
        }

        private static async Task<AvailableCamerasModel> QueryCamerasAsync(AvailableCamerasModel model)
        {
            (await Helper.RunNoMarshall(() => model.QueryCamerasCommand.Execute()))
                .Subscribe(_ => { }, () => { });

            return model;
        }

        private static async Task PrepareCamerasAsync(IEnumerable<IDevice> cams)
        {
            await Helper.RunNoMarshall(() =>
            {
                foreach (var cam in cams)
                {
                    if (cam.Capabilities.GetFunctions.HasFlag(GetFunction.Temperature))
                        cam.TemperatureMonitor(Switch.Enabled,
                            (int) (TimeSpan.Parse(UiSettingsProvider.Settings.Get("UICamStatusUpdateDelay", "00:00:01"))
                                           .TotalMilliseconds));

                    if (
                        (cam.Capabilities.Features & SdkFeatures.ShutterEx) == SdkFeatures.ShutterEx ||
                        (cam.Capabilities.Features & SdkFeatures.Shutter) == SdkFeatures.Shutter
                    )
                    {
                        cam.ShutterControl(ShutterMode.PermanentlyOpen, ShutterMode.PermanentlyOpen);
                    }

                    if (cam.Capabilities.Features.HasFlag(SdkFeatures.FanControl))
                    {
                        cam.FanControl(FanMode.FullSpeed);
                    }
                }
            });
        }

        private async Task DisposeCamera(IDevice cam)
            => await Helper.RunNoMarshall(() =>
            {
                // This one only works if a camera is disposed through 
                // removal from collection - as in "user pressed Disconnect"
                if (cam?.IsDisposed != false || IsDisposing) return;

                cam.CoolerControl(Switch.Disabled);
                cam.TemperatureMonitor(Switch.Disabled, 0);
                cam.Dispose();
            });
    }
}
