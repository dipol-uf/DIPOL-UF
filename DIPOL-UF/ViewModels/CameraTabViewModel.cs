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
using System.Globalization;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using ANDOR_CS.Classes;
using ANDOR_CS.Enums;
using DIPOL_UF.Jobs;
using DIPOL_UF.Models;
using DIPOL_UF.Properties;
using DynamicData.Binding;
using MathNet.Numerics;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
// ReSharper disable UnassignedGetOnlyAutoProperty

namespace DIPOL_UF.ViewModels
{
    internal sealed class CameraTabViewModel : ReactiveViewModel<CameraTab>
    {
        public float MinimumAllowedTemperature => Model.TemperatureRange.Minimum;
        public float MaximumAllowedTemperature => Model.TemperatureRange.Maximum;
        public bool CanControlTemperature => Model.CanControlTemperature;
        public bool CanQueryTemperature => Model.CanQueryTemperature;
        public bool CanControlFan => Model.CanControlFan;
        public bool CanControlInternalShutter => Model.CanControlShutter.Internal;
        public bool CanControlExternalShutter => Model.CanControlShutter.External;

        public int FanTickFrequency => Model.IsThreeStateFan ? 1 : 2;
        // ReSharper disable once UnusedMember.Global
        public string TabHeader => Model.Alias;

        public DipolImagePresenterViewModel DipolImagePresenter { get; }
        public DescendantProxy AcquisitionSettingsWindow { get; private set; }
        public DescendantProxy JobSettingsWindow { get; private set; }

        public float ExposureTime { [ObservableAsProperty] get; }
        public int AcquisitionPbMax { [ObservableAsProperty] get; }
        public int AcquisitionPbMin { [ObservableAsProperty] get; }
        public double AcquisitionPbVal { [ObservableAsProperty] get; }


        [Reactive]
        public float TargetTemperature { get; set; }
        [Reactive]
        public string TargetTemperatureText { get; set; }
        [Reactive]
        public int FanMode { get; set; }

        [Reactive]
        public ShutterMode InternalShutterState { get; set; }
        [Reactive]
        public ShutterMode? ExternalShutterMode { get; set; }

        public bool IsJobInProgress { [ObservableAsProperty] get; }
        public bool IsAcquiring { [ObservableAsProperty]get; }
        public float CurrentTemperature { [ObservableAsProperty] get; }
        public Switch CoolerMode { [ObservableAsProperty] get; }
        public string JobName { [ObservableAsProperty] get; }
        public string JobProgressString { [ObservableAsProperty] get; }
        public int JobCumulativeTotal { [ObservableAsProperty] get; }
        public int JobCumulativeCurrent { [ObservableAsProperty] get; }
        public string JobMotorProgress { [ObservableAsProperty] get; }
        public bool IsPolarimetryJob { [ObservableAsProperty] get; }
        // TODO :Make this prettier

        public ReactiveCommand<Unit, Unit> CoolerCommand { get; private set; }
        public ICommand SetUpAcquisitionCommand => Model.SetUpAcquisitionCommand;
        public ICommand StartAcquisitionCommand => Model.StartAcquisitionCommand;
        public ICommand StartJobCommand => Model.StartJobCommand;
        public ICommand SetUpJobCommand => Model.SetUpJobCommand;
        public CameraTabViewModel(CameraTab model) : base(model)
        {

            DipolImagePresenter = new DipolImagePresenterViewModel(Model.ImagePresenter);
            //AcquisitionSettingsWindow = new DescendantProxy(Model.AcquisitionSettingsWindow, null);

            HookValidators();
            HookObservables();
            InitializeCommands();

            TargetTemperatureText = "0";
        }

        private void InitializeCommands()
        {
            var canUseCooler =
                this.WhenAnyPropertyChanged(nameof(IsJobInProgress), nameof(IsAcquiring))
                    .Select(x => !x.IsAcquiring && !x.IsJobInProgress)
                    .StartWith(true)
                    .CombineLatest(ObserveSpecificErrors(nameof(TargetTemperatureText)),
                        Model.CoolerCommand.CanExecute,
                        (x, y, z) => x && !y && z);

            CoolerCommand =
                ReactiveCommand.Create(() => Unit.Default,
                                  canUseCooler)
                               .DisposeWith(Subscriptions);

            CoolerCommand.Select(_ => (int)TargetTemperature).InvokeCommand(Model.CoolerCommand).DisposeWith(Subscriptions);

            AcquisitionSettingsWindow =
                new DescendantProxy(Model.AcquisitionSettingsWindow,
                        x => new AcquisitionSettingsViewModel((ReactiveWrapper<SettingsBase>) x))
                    .DisposeWith(Subscriptions);
            
            JobSettingsWindow
                = new DescendantProxy(Model.JobSettingsWindow,
                    x => new JobSettingsViewModel((ReactiveWrapper<Target>)x))
                    .DisposeWith(Subscriptions);

            if (CanControlExternalShutter)
                ExternalShutterMode = ShutterMode.PermanentlyOpen;
            if (CanControlInternalShutter)
                InternalShutterState = ShutterMode.PermanentlyOpen;
        }

        private void HookObservables()
        {
            JobManager.Manager.WhenPropertyChanged(x => x.MotorPosition).Select(x => x.Value.HasValue)
                .DistinctUntilChanged()
                .ObserveOnUi()
                .ToPropertyEx(this, x => x.IsPolarimetryJob)
                .DisposeWith(Subscriptions);

            JobManager.Manager.WhenPropertyChanged(x => x.MotorPosition).Select(x => x.Value ?? 0f)
                      .Select(x => string.Format(Localization.CameraTab_StepMotorAngleFormat, x))
                .ObserveOnUi()
                .ToPropertyEx(this, x => x.JobMotorProgress)
                .DisposeWith(Subscriptions);

            JobManager.Manager.WhenAnyPropertyChanged(nameof(JobManager.Progress), nameof(JobManager.Total))
                      .Select(x => string.Format(Localization.CameraTab_JobProgressFormat, x.Progress, x.Total))
                      .ObserveOnUi()
                      .ToPropertyEx(this, x => x.JobProgressString)
                      .DisposeWith(Subscriptions);

            JobManager.Manager.WhenPropertyChanged(x => x.CurrentJobName).Select(x => x.Value)
                      .Sample(UiSettingsProvider.UiThrottlingDelay)
                      .ObserveOnUi()
                      .ToPropertyEx(this, x => x.JobName)
                      .DisposeWith(Subscriptions);

            JobManager.Manager.WhenAnyPropertyChanged(nameof(JobManager.IsInProcess))
                      .Sample(UiSettingsProvider.UiThrottlingDelay)
                      .Select(x => x.IsInProcess
                          ? x.TotalAcquisitionActionCount + x.BiasActionCount + x.DarkActionCount
                          : 0)
                      .ObserveOnUi()
                      .ToPropertyEx(this, x => x.JobCumulativeTotal)
                      .DisposeWith(Subscriptions);
                      
            JobManager.Manager.WhenPropertyChanged(x => x.CumulativeProgress)
                      .Select(x => x.Value)
                      .Sample(UiSettingsProvider.UiThrottlingDelay)
                      .ObserveOnUi()
                      .ToPropertyEx(this, x => x.JobCumulativeCurrent)
                      .DisposeWith(Subscriptions);



            Model.Camera.WhenAnyPropertyChanged(nameof(Model.Camera.IsAcquiring))
                 .Select(x => x.IsAcquiring)
                 .ObserveOnUi()
                 .ToPropertyEx(this, x => x.IsAcquiring)
                 .DisposeWith(Subscriptions);

            Model.Camera.WhenAnyPropertyChanged(nameof(Model.Camera.CoolerMode))
                 .Select(x => x.CoolerMode)
                 .ObserveOnUi()
                 .ToPropertyEx(this, x => x.CoolerMode)
                 .DisposeWith(Subscriptions);

            Model.WhenTemperatureChecked
                 .Select(x => x.Temperature)
                 .ObserveOnUi()
                 .ToPropertyEx(this, x => x.CurrentTemperature)
                 .DisposeWith(Subscriptions);

            // Handles temperature changes

            this.WhenPropertyChanged(x => x.TargetTemperatureText)
                .Subscribe(x =>
                {
                    var canParseMessage = Validators.Validate.CanBeParsed(x.Value, out float temp);
                    string inRangeMessage = null;
                    if (canParseMessage is null)
                        inRangeMessage = Validators.Validate.ShouldFallWithinRange(temp,
                            x.Sender.MinimumAllowedTemperature,
                            x.Sender.MaximumAllowedTemperature);

                    BatchUpdateErrors(
                        (nameof(TargetTemperatureText), nameof(Validators.Validate.CanBeParsed), canParseMessage),
                        (nameof(TargetTemperatureText), nameof(Validators.Validate.ShouldFallWithinRange),
                            inRangeMessage));

                    if (canParseMessage is null && inRangeMessage is null)
                        TargetTemperature = temp;
                })
                .DisposeWith(Subscriptions);

            this.WhenPropertyChanged(x => x.TargetTemperature)
                .Where(x => !x.Value.AlmostEqual(float.TryParse(
                    x.Sender.TargetTemperatureText, NumberStyles.Any, NumberFormatInfo.InvariantInfo,
                    out var temp)
                    ? temp
                    : float.NaN))
                .Select(x => x.Value.ToString(Localization.General_FloatTempFormat))
                .BindTo(this, x => x.TargetTemperatureText)
                .DisposeWith(Subscriptions);

            // End of temperature changes


            Model.Camera.WhenAnyPropertyChanged(nameof(Model.Camera.FanMode))
                 .Select(x => 2 - (uint) x.FanMode)
                 .DistinctUntilChanged()
                 .ObserveOnUi()
                 .BindTo(this, x => x.FanMode)
                 .DisposeWith(Subscriptions);

            this.WhenPropertyChanged(x => x.FanMode)
                .Select(x => (FanMode) (2 - x.Value))
                .InvokeCommand(Model.FanCommand)
                .DisposeWith(Subscriptions);


            var shutterSrc =
                Model.Camera.WhenAnyPropertyChanged(nameof(Model.Camera.Shutter))
                     .Select(x => x.Shutter)
                     .DistinctUntilChanged()
                     .ObserveOnUi();

            shutterSrc.Select(x => x.Internal)
                      .DistinctUntilChanged()
                      .BindTo(this, x => x.InternalShutterState)
                      .DisposeWith(Subscriptions);

            this.WhenPropertyChanged(x => x.InternalShutterState)
                .Select(x => x.Value)
                .DistinctUntilChanged()
                .InvokeCommand(Model.InternalShutterCommand)
                .DisposeWith(Subscriptions);

            if (CanControlExternalShutter)
            {
                shutterSrc.Select(x => x.External)
                          .DistinctUntilChanged()
                          .BindTo(this, x => x.ExternalShutterMode)
                          .DisposeWith(Subscriptions);

                this.WhenPropertyChanged(x => x.ExternalShutterMode)
                    .Select(x => x.Value)
                    .DistinctUntilChanged()
                    .InvokeCommand(Model.ExternalShutterCommand)
                    .DisposeWith(Subscriptions);

            }

            Model.WhenTimingCalculated.Select(x => x.Kinetic)
                 .ObserveOnUi()
                 .ToPropertyEx(this, x => x.ExposureTime)
                 .DisposeWith(Subscriptions);

            Model.WhenPropertyChanged(x => x.AcquisitionProgress)
                 .Select(x => x.Value)
                 //.Sample(UiSettingsProvider.UiThrottlingDelay)
                 .ObserveOnUi()
                 .ToPropertyEx(this, x => x.AcquisitionPbVal)
                 .DisposeWith(Subscriptions);

            Model.WhenPropertyChanged(x => x.AcquisitionProgressRange)
                 .Select(x => x.Value.Min)
                 .ObserveOnUi()
                 .ToPropertyEx(this, x => x.AcquisitionPbMin)
                 .DisposeWith(Subscriptions);

            Model.WhenPropertyChanged(x => x.AcquisitionProgressRange)
                 .Select(x => x.Value.Max)
                 .ObserveOnUi()
                 .ToPropertyEx(this, x => x.AcquisitionPbMax)
                 .DisposeWith(Subscriptions);

            PropagateReadOnlyProperty(this, x => x.IsJobInProgress, y => y.IsJobInProgress);
        }

        public string Name => Model.ToString();

    }
}
