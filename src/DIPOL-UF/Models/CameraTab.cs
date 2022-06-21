using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using ANDOR_CS;
using ANDOR_CS.AcquisitionMetadata;
using ANDOR_CS.Classes;
using ANDOR_CS.Enums;
using ANDOR_CS.Events;
using DipolImage;
using DIPOL_UF.Converters;
using DIPOL_UF.Jobs;
using DIPOL_UF.Services.Contract;
using DynamicData.Binding;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Timer = System.Timers.Timer;

namespace DIPOL_UF.Models
{
    internal sealed class CameraTab: ReactiveObjectEx
    {

        private Image _cachedPreview;

        private Task _acquisitionTask;
        private CancellationTokenSource _acquisitionTokenSource;

        private DateTimeOffset _acqStartTime;
        private DateTimeOffset _lastImageTime;
        private DateTimeOffset _acqEndTime;

        private readonly Timer _pbTimer = new();
        private readonly IAcquisitionTimerManager _acqTimerManager;

        // WARN: Substitute for scoped DI
        public IServiceScope ServiceScope { get; }
        
        public Camera Camera { get; }
        public (float Minimum, float Maximum) TemperatureRange { get; }
        public bool CanControlTemperature { get; }
        public bool CanControlFan { get; }
        public bool IsThreeStateFan { get; }
        public bool CanQueryTemperature { get; }
        public (bool Internal, bool External) CanControlShutter { get; }
        public string Alias { get; }

        [Reactive]
        public double AcquisitionProgress { get; private set; }
        [Reactive]
        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local
        public (int Min, int Max) AcquisitionProgressRange { get; private set; }

        // ReSharper disable once UnassignedGetOnlyAutoProperty
        public bool IsJobInProgress { [ObservableAsProperty] get; }

        public IObservable<AcquisitionStatusEventArgs> WhenAcquisitionStarted { get; private set; }
        public IObservable<AcquisitionStatusEventArgs> WhenAcquisitionFinished { get; private set; }
        public IObservable<(float Exposure, float Accumulation, float Kinetic)> WhenTimingCalculated { get; private set; }
        public DipolImagePresenter ImagePresenter { get; }
        public DescendantProvider AcquisitionSettingsWindow { get; private set; }
        public DescendantProvider JobSettingsWindow { get; private set; }
        public DescendantProvider CycleConfigWindow { get; private set; }
        public IObservable<TemperatureStatusEventArgs> WhenTemperatureChecked { get; private set; }

        public ReactiveCommand<int, Unit> CoolerCommand { get; private set; }
        public ReactiveCommand<FanMode, Unit> FanCommand { get; private set; }
        public ReactiveCommand<ShutterMode, Unit> InternalShutterCommand { get; private set; }
        public ReactiveCommand<ShutterMode, Unit> ExternalShutterCommand { get; private set; }
        
        public ReactiveCommand<Unit, object> SetUpAcquisitionCommand { get; private set; }
        public ReactiveCommand<Unit, Unit> StartAcquisitionCommand { get; private set; }
        public ReactiveCommand<Unit, bool> StartJobCommand { get; private set; }
        public ReactiveCommand<Unit, object> SetUpJobCommand { get; private set; }

        public ReactiveCommand<Unit,Unit > StartAllAcquisitionsCommand { get; private set; }

        public CameraTab(IDevice camera)
        {
            ServiceScope = Injector.ServiceProvider.CreateScope();
            _acqTimerManager = ServiceScope.ServiceProvider.GetRequiredService<IAcquisitionTimerManager>();
            // WATCH: Temp solution
            Camera = (Camera)camera;
            
            TemperatureRange = camera.Capabilities.GetFunctions.HasFlag(GetFunction.TemperatureRange)
                ? camera.Properties.AllowedTemperatures
                : default;
            CanControlTemperature = camera.Capabilities.SetFunctions.HasFlag(SetFunction.Temperature);
            CanQueryTemperature = camera.Capabilities.GetFunctions.HasFlag(GetFunction.Temperature);
            CanControlFan = camera.Capabilities.Features.HasFlag(SdkFeatures.FanControl);
            IsThreeStateFan = camera.Capabilities.Features.HasFlag(SdkFeatures.LowFanMode);
            CanControlShutter = (
                Internal: camera.Capabilities.Features.HasFlag(SdkFeatures.Shutter),
                External: camera.Capabilities.Features.HasFlag(SdkFeatures.ShutterEx));

            Alias = ConverterImplementations.CameraToStringAliasConversion(camera);


            ImagePresenter = new DipolImagePresenter(ConverterImplementations.DeviceDescriptor(Camera), Camera);

            HookObservables();
            InitializeCommands();

            AcquisitionProgressRange = (0, 50);
            AcquisitionProgress = 0;

            _pbTimer.AutoReset = true;
            _pbTimer.Enabled = false;
            _pbTimer.Elapsed += TimerTick;
        }

        public void StartAcquisition(Request metadata, CancellationToken token)
        {
            var (_, _, kinetic) = Camera.Timings;
            
            _pbTimer.Interval = kinetic /
                (AcquisitionProgressRange.Max - AcquisitionProgressRange.Min + 1) * 1000;
            AcquisitionProgress = AcquisitionProgressRange.Min;
            _acquisitionTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);

            _acqStartTime = DateTimeOffset.UtcNow;
            _lastImageTime = _acqStartTime;
            _acquisitionTask = Camera
                               .StartAcquisitionAsync(metadata, _acquisitionTokenSource.Token)
                               .ExpectCancellationAsync();
            _acqEndTime = _acqStartTime + TimeSpan.FromSeconds(kinetic);

            _acqTimerManager.StartMeasuring(TimeSpan.FromSeconds(kinetic));
            _pbTimer.Start();
        }

        public void StopAcquisition()
        {
            if (Camera.IsAcquiring)
            {
                _acquisitionTokenSource.Cancel();
            }
            _acqTimerManager.StopMeasuring();
            
            _acquisitionTask = null;
            _acquisitionTokenSource.Dispose();
            _acquisitionTokenSource = null;
        }

        private void HookObservables()
        {
            Camera.NewImageReceived += (_, args) =>
            {
                _cachedPreview = Camera.PullPreviewImage(args.Index, ImageFormat.UnsignedInt16);
                _lastImageTime = DateTimeOffset.UtcNow;
            };
            Camera.NewImageReceived += (_, _) =>
            {
                if (_cachedPreview is not null)
                {
                    ImagePresenter.LoadImage(_cachedPreview);
                }
            };


            WhenTemperatureChecked =
                Observable.FromEventPattern<TemperatureStatusEventHandler, TemperatureStatusEventArgs>(
                              x => Camera.TemperatureStatusChecked += x,
                              x => Camera.TemperatureStatusChecked -= x)
                          .Select(x => x.EventArgs)
                          .DistinctUntilChanged();

            void ResetTimer(double val)
            {
                _pbTimer.Stop();
                AcquisitionProgress = val;
            }

            Camera.AcquisitionFinished += (_, _) => ResetTimer(AcquisitionProgressRange.Min);

            WhenAcquisitionStarted =
                Observable
                    .FromEventPattern<AcquisitionStatusEventHandler, AcquisitionStatusEventArgs>(
                        x => Camera.AcquisitionStarted += x,
                        x => Camera.AcquisitionStarted -= x)
                    .Select(x => x.EventArgs);

            WhenAcquisitionFinished =
                Observable
                    .FromEventPattern<AcquisitionStatusEventHandler, AcquisitionStatusEventArgs>(
                        x => Camera.AcquisitionFinished += x,
                        x => Camera.AcquisitionFinished -= x)
                    .Select(x => x.EventArgs);

            JobManager.Manager.WhenPropertyChanged(x => x.IsInProcess)
                      .Select(x => x.Value)
                      .ToPropertyEx(this, x => x.IsJobInProgress)
                      .DisposeWith(Subscriptions);


            WhenAcquisitionFinished.SubscribeDispose(Subscriptions);
        }

        private void InitializeCommands()
        {


            CoolerCommand =
                ReactiveCommand.Create<int>(
                                   (x) =>
                                   {
                                       Camera.SetTemperature(x);
                                       Camera.CoolerControl(Camera.CoolerMode == Switch.Disabled
                                           ? Switch.Enabled
                                           : Switch.Disabled);
                                   },
                                   Observable.Return(
                                       Camera.Capabilities.SetFunctions.HasFlag(SetFunction.Temperature)))
                               .DisposeWith(Subscriptions);

            FanCommand =
                ReactiveCommand.Create<FanMode>(
                                   Camera.FanControl,
                                   Observable.Return(
                                       Camera.Capabilities.Features.HasFlag(SdkFeatures.FanControl)))
                               .DisposeWith(Subscriptions);

            InternalShutterCommand =
                ReactiveCommand.Create<ShutterMode>(
                                   x => Camera.ShutterControl(
                                       x, Camera.Shutter.External ?? ShutterMode.FullyAuto),
                                   Observable.Return(CanControlShutter.Internal))
                               .DisposeWith(Subscriptions);

            ExternalShutterCommand =
                ReactiveCommand.Create<ShutterMode>(
                                   x => Camera.ShutterControl(
                                       Camera.Shutter.Internal, x),
                                   Observable.Return(CanControlShutter.External))
                               .DisposeWith(Subscriptions);

            // Use JobManager.Manager.AnyCameraIsAcquiring
            var canSetupJob =
                this.WhenPropertyChanged(x => x.IsJobInProgress)
                    .CombineLatest(
                        //Camera.WhenPropertyChanged(y => y.IsAcquiring),
                        JobManager.Manager.WhenPropertyChanged(y => y.AnyCameraIsAcquiring),
                        (x, y) => !x.Value && !y.Value)
                    .ObserveOnUi();

            var canSetupSetts = this.WhenPropertyChanged(x => x.IsJobInProgress)
                .CombineLatest(
                    Camera.WhenPropertyChanged(y => y.IsAcquiring),
                    (x, y) => !x.Value && !y.Value)
                .ObserveOnUi();

            SetUpAcquisitionCommand =
                ReactiveCommand.Create<Unit, object>(x => x, canSetupSetts)
                               .DisposeWith(Subscriptions);

            AcquisitionSettingsWindow = new DescendantProvider(
                    ReactiveCommand.Create<object, ReactiveObjectEx>(
                        _ => new ReactiveWrapper<IAcquisitionSettings>(
                            Camera.CurrentSettings ?? Camera.GetAcquisitionSettingsTemplate())),
                    null, null,
                    ReactiveCommand.Create<ReactiveObjectEx>(x =>
                    {
                        if (x is ReactiveWrapper<IAcquisitionSettings> wrapper
                            && wrapper.Object is { } setts
                            && ReferenceEquals(setts, Camera.CurrentSettings))
                            wrapper.Object = null;

                        x.Dispose();
                    }))
                .DisposeWith(Subscriptions);

            SetUpAcquisitionCommand.InvokeCommand(AcquisitionSettingsWindow.ViewRequested).DisposeWith(Subscriptions);

            SetUpJobCommand =
                ReactiveCommand.Create<Unit, object>(x => x, canSetupJob)
                               .DisposeWith(Subscriptions);

            JobSettingsWindow = new DescendantProvider(
                    ReactiveCommand.Create<object, ReactiveObjectEx>(
                        _ => new ReactiveWrapper</*Target*/ Target1>(JobManager.Manager.GenerateTarget1())),
                    null, null, ReactiveCommand.CreateFromTask<ReactiveObjectEx>(async x =>
                    {
                        if (x is ReactiveWrapper<Target1> {Object: { } target})
                            try
                            {
                                await JobManager.Manager.SubmitNewTarget1(target);
                            }
                            catch (Exception e)
                            {
                                MessageBox.Show(
                                    e.Message,
                                    Properties.Localization.JobManager_InvalidState_Header,
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                    }))
                .DisposeWith(Subscriptions);

            SetUpJobCommand.InvokeCommand(JobSettingsWindow.ViewRequested).DisposeWith(Subscriptions);


            CycleConfigWindow = new DescendantProvider(
                ReactiveCommand.Create<object, ReactiveObjectEx>(
                    _ => new ReactiveWrapper<int?>(null)),
                null, null,
                ReactiveCommand.Create<ReactiveObjectEx>(x =>
                {
                    if (x is ReactiveWrapper<int?> wrapper && wrapper.Object is { } iVal)
                    {
                        if (!JobManager.Manager.IsInProcess && JobManager.Manager.ReadyToRun)
                        {
                            JobManager.Manager.StartJob(iVal);
                        }
                    }

                }))
                .DisposeWith(Subscriptions);

            var hasValidSettings = Camera.WhenPropertyChanged(x => x.CurrentSettings)
                                         .CombineLatest(this.WhenPropertyChanged(y => y.IsJobInProgress),
                                             (x, y) => !(x.Value is null) && !y.Value)
                                         .ObserveOnUi();

            WhenTimingCalculated = Camera.WhenPropertyChanged(x => x.CurrentSettings)
                                       .Select(_ => Camera.Timings);

            StartAcquisitionCommand =
                ReactiveCommand.Create(() =>
                        {
                            if (!Camera.IsAcquiring
                                && !(Camera.CurrentSettings is null))
                                StartAcquisition(default, CancellationToken.None);
                            else if (!(_acquisitionTask is null) && !(_acquisitionTokenSource is null))
                                StopAcquisition();
                        },
                        hasValidSettings.CombineLatest(JobManager.Manager.WhenPropertyChanged(y => y.IsRegimeSwitching).ObserveOnUi(),
                            (x, y) => x && !y.Value))
                    .DisposeWith(Subscriptions);


            var jobAvailableObs =
                JobManager.Manager.WhenAnyPropertyChanged(
                              nameof(JobManager.AnyCameraIsAcquiring),
                              nameof(JobManager.ReadyToRun),
                              nameof(JobManager.IsInProcess),
                                nameof(JobManager.IsRegimeSwitching))
                           // (Ready AND NOT Acq) OR Job
                          .Select(x => (!x.AnyCameraIsAcquiring && !x.IsRegimeSwitching && x.ReadyToRun)
                                       || x.IsInProcess)
                          .CombineLatest(
                              Camera.WhenPropertyChanged(y => y.CurrentSettings).Select(y => y.Value is null),
                              // job is ready AND settings are applied
                              (x, y) => x && !y)
                          .ObserveOnUi();

            StartJobCommand =
                ReactiveCommand.Create<Unit, bool>(_ =>
                                   {
                                       if (!JobManager.Manager.IsInProcess)
                                       {
                                           if (JobManager.Manager.ReadyToRun)
                                               return true;
                                           else
                                           {
                                               MessageBox.Show(Properties.Localization.JobManager_InvalidState_Text,
                                                   Properties.Localization.JobManager_InvalidState_Header,
                                                   MessageBoxButton.OK, MessageBoxImage.Error);
                                           }
                                       }
                                       else
                                       {
                                           JobManager.Manager.StopJob();
                                       }
                                       return false;
                                   },
                                   jobAvailableObs)
                               .DisposeWith(Subscriptions);

            StartJobCommand
                .Where(x => x)
                .InvokeCommand(CycleConfigWindow.ViewRequested).DisposeWith(Subscriptions);


            var canStartAllAcquisitions = JobManager.Manager.WhenPropertyChanged(x => x.IsInProcess)
                .CombineLatest(
                    JobManager.Manager.WhenPropertyChanged(y => y.AllCamerasHaveSettings),
                    JobManager.Manager.WhenPropertyChanged(z => z.IsRegimeSwitching),
                    (x, y, z) => !x.Value && y.Value && !z.Value)
                .ObserveOnUi();

            StartAllAcquisitionsCommand = ReactiveCommand.Create(StartStopAcquisitionAll, canStartAllAcquisitions).
                DisposeWith(Subscriptions);

        }

        private static void StartStopAcquisitionAll()
        {
            if (JobManager.Manager.AnyCameraIsAcquiring)
            {
                JobManager.Manager.StopAllAcquisitions();
            }
            else if (!JobManager.Manager.IsInProcess)
            {
                JobManager.Manager.StartAllAcquisitions();
            }
        }

        private void TimerTick(object sender, ElapsedEventArgs e)
        {
            if (
                sender is not Timer {Enabled: true} ||
                _acquisitionTask is null ||
                AcquisitionProgress > AcquisitionProgressRange.Max
            )
            {
                return;
            }

            var dtAct = (e.SignalTime - _lastImageTime).TotalSeconds;
            var dtPred = (_acqEndTime - _acqStartTime).TotalSeconds;

            var frac = (AcquisitionProgressRange.Max - AcquisitionProgressRange.Min) * (dtAct / dtPred);

            AcquisitionProgress = Math.Min(Math.Max(frac, AcquisitionProgressRange.Min), AcquisitionProgressRange.Max);
        }

        protected override void Dispose(bool disposing)
        {
            if(!IsDisposed && disposing)
            {
                _pbTimer.Stop();
            }

            _pbTimer.Elapsed -= TimerTick;

            ServiceScope.Dispose();
            base.Dispose(disposing);
        }
    }
}
