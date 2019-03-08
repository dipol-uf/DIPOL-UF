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

using System.Globalization;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using ANDOR_CS.Classes;
using ANDOR_CS.Enums;
using DIPOL_UF.Models;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
// ReSharper disable UnassignedGetOnlyAutoProperty

namespace DIPOL_UF.ViewModels
{
    internal sealed class CameraTabViewModel : ReactiveViewModel<CameraTab>
    {
        [Reactive]
        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        private float ActualTemperature { get; set; }

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

        public DipolImagePresenterViewModel DipolImagePresenter { get; private set; }
        public DescendantProxy AcquisitionSettingsWindow { get; private set; }

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

        public bool IsAcquiring { [ObservableAsProperty]get; }
        public float CurrentTemperature { [ObservableAsProperty] get; }
        public Switch CoolerMode { [ObservableAsProperty] get; }

        public ReactiveCommand<Unit, Unit> CoolerCommand { get; private set; }
        public ICommand SetUpAcquisitionCommand => Model.SetUpAcquisitionCommand;

        public CameraTabViewModel(CameraTab model) : base(model)
        {

            DipolImagePresenter = new DipolImagePresenterViewModel(Model.ImagePresenter);
            //AcquisitionSettingsWindow = new DescendantProxy(Model.AcquisitionSettingsWindow, null);

            HookValidators();
            HookObservables();
            InitializeCommands();

            TargetTemperatureText = "0";
            InternalShutterState = Model.Camera.Shutter.Internal;
            ExternalShutterMode = CanControlExternalShutter ? Model.Camera.Shutter.External : null;
        }

        private void InitializeCommands()
        {
            CoolerCommand =
                ReactiveCommand.Create(() => Unit.Default,
                                   Model.CoolerCommand.CanExecute
                                        .CombineLatest(
                                       ObserveSpecificErrors(nameof(TargetTemperatureText)),
                                     (x, y) => x && !y))
                               .DisposeWith(Subscriptions);

            CoolerCommand.Select(_ => (int)TargetTemperature).InvokeCommand(Model.CoolerCommand).DisposeWith(Subscriptions);

            AcquisitionSettingsWindow = new DescendantProxy(Model.AcquisitionSettingsWindow,
                x => new AcquisitionSettingsViewModel((ReactiveWrapper<SettingsBase>) x)).DisposeWith(Subscriptions);
            
        }

        private void HookObservables()
        {

            Model.Camera.WhenAnyPropertyChanged(nameof(Model.Camera.IsAcquiring)).Select(x => x.IsAcquiring)
                 .LogObservable("Acquiring", Subscriptions);

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
            this.WhenPropertyChanged(x => x.TargetTemperature)
                .Select(x => x.Value)
                .DistinctUntilChanged()
                .ObserveOnUi()
                .BindTo(this, x => x.ActualTemperature)
                .DisposeWith(Subscriptions);

            this.WhenPropertyChanged(x => x.TargetTemperatureText)
                .DistinctUntilChanged()
                .Where(x => !HasSpecificErrors(nameof(TargetTemperatureText)) && !string.IsNullOrEmpty(x.Value))
                .Select(x => float.Parse(x.Value, NumberStyles.Any, CultureInfo.CurrentUICulture))
                .ObserveOnUi()
                .BindTo(this, x => x.ActualTemperature)
                .DisposeWith(Subscriptions);

            var actTempObs =
                this.WhenPropertyChanged(x => x.ActualTemperature)
                    .Select(x => x.Value)
                    .ObserveOnUi();

            actTempObs.BindTo(this, x => x.TargetTemperature).DisposeWith(Subscriptions);
            actTempObs.Select(x => x.ToString(
                          Properties.Localization.General_TemperatureFloatFormat,
                          CultureInfo.CurrentUICulture))
                      .BindTo(this, x => x.TargetTemperatureText)
                      .DisposeWith(Subscriptions);

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
                shutterSrc.Select(x => x.External.ToStringEx())
                          .DistinctUntilChanged()
                          .BindTo(this, x => x.ExternalShutterMode)
                          .DisposeWith(Subscriptions);

                this.WhenPropertyChanged(x => x.ExternalShutterMode)
                    .Select(x => x.Value)
                    .DistinctUntilChanged()
                    .InvokeCommand(Model.ExternalShutterCommand)
                    .DisposeWith(Subscriptions);

            }

        }

        protected override void HookValidators()
        {
            base.HookValidators();

            CreateValidator(
                this.WhenAnyPropertyChanged(nameof(TargetTemperatureText))
                    .Sample(UiSettingsProvider.UiThrottlingDelay)
                    .ObserveOnUi()
                    .Select(x => (Type: nameof(Validators.Validate.CanBeParsed),
                            Message: Validators.Validate.CanBeParsed(x.TargetTemperatureText, out float _))),
                        nameof(TargetTemperatureText));

            CreateValidator(
                this.WhenAnyPropertyChanged(nameof(TargetTemperatureText))
                    .Select(x => (
                        Type: nameof(Validators.Validate.ShouldFallWithinRange),
                        Message: Validators.Validate.ShouldFallWithinRange(
                            float.TryParse(x.TargetTemperatureText,
                                NumberStyles.Any,
                                CultureInfo.CurrentUICulture,
                                out var val)
                                ? val
                                : x.MinimumAllowedTemperature,
                            x.MinimumAllowedTemperature,
                            x.MaximumAllowedTemperature))),
                nameof(TargetTemperatureText));

        }

        public string Name => Model.ToString();

    }
}
