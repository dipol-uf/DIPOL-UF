//    This file is part of Dipol-3 Camera Manager.

//     MIT License
//     
//     Copyright(c) 2018-2019 Ilia Kosenkov
//     
//     Permission is hereby granted, free of charge, to any person obtaining a copy
//     of this software and associated documentation files (the "Software"), to deal
//     in the Software without restriction, including without limitation the rights
//     to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//     copies of the Software, and to permit persons to whom the Software is
//     furnished to do so, subject to the following conditions:
//     
//     The above copyright notice and this permission notice shall be included in all
//     copies or substantial portions of the Software.
//     
//     THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//     IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//     FITNESS FOR A PARTICULAR PURPOSE AND NONINFINGEMENT. IN NO EVENT SHALL THE
//     AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//     LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//     OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//     SOFTWARE.

using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using ANDOR_CS.Classes;
using ANDOR_CS.Enums;
using ANDOR_CS.Events;
using DipolImage;
using DIPOL_UF.Converters;
using DIPOL_UF.Jobs;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Timer = System.Timers.Timer;

namespace DIPOL_UF.Models
{
    internal sealed class CameraTab: ReactiveObjectEx
    {
        private Task _acquisitionTask;
        private CancellationTokenSource _acquisitionTokenSource;

        private readonly Timer _pbTimer = new Timer();

        public CameraBase Camera { get; }
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
        public (int Min, int Max) AcquisitionProgressRange { get; private set; }

        public IObservable<AcquisitionStatusEventArgs> WhenAcquisitionStarted { get; private set; }
        public IObservable<AcquisitionStatusEventArgs> WhenAcquisitionFinished { get; private set; }
        public IObservable<(float Exposure, float Accumulation, float Kinetic)> WhenTimingCalculated { get; private set; }
        public IObservable<Image> WhenNewPreviewArrives { get; private set; }
        public DipolImagePresenter ImagePresenter { get; }
        public DescendantProvider AcquisitionSettingsWindow { get; private set; }
        public DescendantProvider JobSettingsWindow { get; private set; }
        public IObservable<TemperatureStatusEventArgs> WhenTemperatureChecked { get; private set; }

        public ReactiveCommand<int, Unit> CoolerCommand { get; private set; }
        public ReactiveCommand<FanMode, Unit> FanCommand { get; private set; }
        public ReactiveCommand<ShutterMode, Unit> InternalShutterCommand { get; private set; }
        public ReactiveCommand<ShutterMode, Unit> ExternalShutterCommand { get; private set; }
        
        public ReactiveCommand<Unit, object> SetUpAcquisitionCommand { get; private set; }
        public ReactiveCommand<Unit, Unit> StartAcquisitionCommand { get; private set; }
        public ReactiveCommand<Unit, Unit> StartJobCommand { get; private set; }
        public ReactiveCommand<Unit, object> SetUpJobCommand { get; private set; }

        public CameraTab(CameraBase camera)
        {
            Camera = camera;
            
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


            ImagePresenter = new DipolImagePresenter();

            HookObservables();
            InitializeCommands();

            AcquisitionProgressRange = (0, 50);
            AcquisitionProgress = 0;

            _pbTimer.AutoReset = true;
            _pbTimer.Enabled = false;
            _pbTimer.Elapsed += TimerTick;
        }

        private void HookObservables()
        {
            WhenTemperatureChecked =
                Observable.FromEventPattern<TemperatureStatusEventHandler, TemperatureStatusEventArgs>(
                              x => Camera.TemperatureStatusChecked += x,
                              x => Camera.TemperatureStatusChecked -= x)
                          .Select(x => x.EventArgs)
                          .DistinctUntilChanged();

           WhenNewPreviewArrives =
                Observable.FromEventPattern<NewImageReceivedHandler, NewImageReceivedEventArgs>(
                              x => Camera.NewImageReceived += x,
                              x => Camera.NewImageReceived -= x)
                          .Sample(TimeSpan.Parse(UiSettingsProvider.Settings.Get("NewImagePollDelay", "00:00:00.500")))
                          .Select(x => Camera.PullPreviewImage(x.EventArgs.Index, ImageFormat.UnsignedInt16))
                          .Where(x => !(x is null));
            //TODO : ImageFormat for displaying

            WhenNewPreviewArrives
                .Select(x => Observable.FromAsync(_ => ImagePresenter.LoadImageAsync(x)))
                .Merge()
                .SubscribeDispose(Subscriptions);

            void ResetTimer(double val)
            {
                _pbTimer.Stop();
                AcquisitionProgress = val;
            }

            Camera.AcquisitionFinished += (_, e) => ResetTimer(AcquisitionProgressRange.Min);

            Camera.NewImageReceived += (_, e) => ResetTimer(AcquisitionProgressRange.Max);

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

            SetUpAcquisitionCommand =
                ReactiveCommand.Create<Unit, object>(x => x)
                               .DisposeWith(Subscriptions);

            AcquisitionSettingsWindow = new DescendantProvider(
                    ReactiveCommand.Create<object, ReactiveObjectEx>(
                        _ => new ReactiveWrapper<SettingsBase>(
                            Camera.CurrentSettings ?? Camera.GetAcquisitionSettingsTemplate())),
                    ReactiveCommand.Create<Unit>(_ => { }),
                    null,
                    ReactiveCommand.Create<ReactiveObjectEx>(x =>
                    {
                        if (x is ReactiveWrapper<SettingsBase> wrapper
                            && wrapper.Object is SettingsBase setts
                            && ReferenceEquals(setts, Camera.CurrentSettings))
                            wrapper.Object = null;

                        x.Dispose();
                    }))
                .DisposeWith(Subscriptions);

            SetUpAcquisitionCommand.InvokeCommand(AcquisitionSettingsWindow.ViewRequested).DisposeWith(Subscriptions);


            SetUpJobCommand =
                ReactiveCommand.Create<Unit, object>(x => x)
                               .DisposeWith(Subscriptions);

            JobSettingsWindow = new DescendantProvider(
                    ReactiveCommand.Create<object, ReactiveObjectEx>(
                        _ => new ReactiveWrapper<Target>(JobManager.Manager.CurrentTarget)),
                    ReactiveCommand.Create<Unit>(_ => { }),
                    null,
                    ReactiveCommand.Create<ReactiveObjectEx>(x =>
                    {
                        // TODO : assign updated target
                    }))
                .DisposeWith(Subscriptions);

            SetUpJobCommand.InvokeCommand(JobSettingsWindow.ViewRequested).DisposeWith(Subscriptions);


            //var hasValidSettings = AcquisitionSettingsWindow
            //                    .ViewFinished
            //                    .Select(_ => !(Camera.CurrentSettings is null));
            // WATCH : switched to [INotifyPropertyChanged] behavior
            // WATCH : [PropertyValue<T>] spoils all [is null] checks
            var hasValidSettings = Camera.WhenPropertyChanged(x => x.CurrentSettings)
                                         .Select(x => !(x.Value is null))
                                         .ObserveOnUi();

            WhenTimingCalculated =
                AcquisitionSettingsWindow.ViewFinished
                                         .CombineLatest(JobSettingsWindow.ViewFinished,
                                             (x, y) => Unit.Default)
                                         .Select(_ => Camera.Timings);

            StartAcquisitionCommand =
                ReactiveCommand.Create(() =>
                               {
                                   if (!Camera.IsAcquiring
                                       && !(Camera.CurrentSettings is null))
                                   {
                                       _pbTimer.Interval = Camera.Timings.Kinetic /
                                                           (AcquisitionProgressRange.Max - AcquisitionProgressRange.Min
                                                           );
                                       AcquisitionProgress = AcquisitionProgressRange.Min;
                                       _pbTimer.Start();
                                       _acquisitionTokenSource = new CancellationTokenSource();
                                       _acquisitionTask = Camera
                                                          .StartAcquisitionAsync(_acquisitionTokenSource.Token)
                                                          .ExpectCancellationAsync();
                                   }
                                   else if (!(_acquisitionTask is null) && !(_acquisitionTokenSource is null))
                                   {
                                       if (Camera.IsAcquiring)
                                           _acquisitionTokenSource.Cancel();
                                       _acquisitionTask = null;
                                       _acquisitionTokenSource.Dispose();
                                       _acquisitionTokenSource = null;
                                   }
                               }, hasValidSettings)
                               .DisposeWith(Subscriptions);

            var jobAvailableObs = JobManager.Manager.WhenPropertyChanged(x => x.AnyCameraIsAcquiring)
                                            .CombineLatest(JobManager.Manager.WhenPropertyChanged(y => y.ReadyToRun),
                                                // NOT any camera acquiring AND ready to run
                                                (x, y) => !x.Value && y.Value);

            StartJobCommand =
                ReactiveCommand.Create(() =>
                                   {
                                       if (JobManager.Manager.ReadyToRun)
                                           JobManager.Manager.StartJobAsync(default);
                                   },
                                   jobAvailableObs)
                               .DisposeWith(Subscriptions);
        }

        private void TimerTick(object sender, ElapsedEventArgs e)
        {
            if (sender is Timer t && t.Enabled && _acquisitionTask != null
                && AcquisitionProgress <= AcquisitionProgressRange.Max)
                AcquisitionProgress +=
                    Math.Floor((AcquisitionProgressRange.Max - AcquisitionProgressRange.Min) * t.Interval);
        }

        protected override void Dispose(bool disposing)
        {
            if(!IsDisposed && disposing)
                _pbTimer.Stop();
                _pbTimer.Elapsed -= TimerTick;

            base.Dispose(disposing);
        }
    }
}
