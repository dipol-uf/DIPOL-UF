using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
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


        public CameraTabViewModel(CameraTab model) : base(model)
        {
            TargetTemperatureText = "0";
            InternalShutterState = Model.Camera.Shutter.Internal;
            ExternalShutterMode = CanControlExternalShutter ? Model.Camera.Shutter.External : null;

            DipolImagePresenter = new DipolImagePresenterViewModel(Model.ImagePresenter);
            //AcquisitionSettingsWindow = new DescendantProxy(Model.AcquisitionSettingsWindow, null);

            HookValidators();
            HookObservables();
            InitializeCommands();
        }

        private void InitializeCommands()
        {
            CoolerCommand =
                ReactiveCommand.Create(() => Unit.Default,
                                   Model.CoolerCommand.CanExecute.CombineLatest(
                                       ObserveSpecificErrors(nameof(TargetTemperatureText)),
                                       //WhenErrorsChangedTyped
                                       //    .Where(x => x.Property == nameof(TargetTemperatureText))
                                       //    .Select(x => !HasSpecificErrors(x.Property)),
                                     (x, y) => x && !y))
                               .DisposeWith(_subscriptions);
        }

        private void HookObservables()
        {

            Model.Camera.WhenAnyPropertyChanged(nameof(Model.Camera.IsAcquiring)).Select(x => x.IsAcquiring)
                 .LogObservable("Acquiring", _subscriptions);

            Model.Camera.WhenAnyPropertyChanged(nameof(Model.Camera.IsAcquiring))
                 .Select(x => x.IsAcquiring)
                 .ObserveOnUi()
                 .ToPropertyEx(this, x => x.IsAcquiring)
                 .DisposeWith(_subscriptions);

            Model.Camera.WhenAnyPropertyChanged(nameof(Model.Camera.CoolerMode))
                 .Select(x => x.CoolerMode)
                 .ObserveOnUi()
                 .ToPropertyEx(this, x => x.CoolerMode)
                 .DisposeWith(_subscriptions);

            Model.WhenTemperatureChecked
                 .Select(x => x.Temperature)
                 .ObserveOnUi()
                 .ToPropertyEx(this, x => x.CurrentTemperature)
                 .DisposeWith(_subscriptions);

            // Handles temperature changes
            this.WhenPropertyChanged(x => x.TargetTemperature)
                .Select(x => x.Value)
                .DistinctUntilChanged()
                .ObserveOnUi()
                .BindTo(this, x => x.ActualTemperature)
                .DisposeWith(_subscriptions);

            this.WhenPropertyChanged(x => x.TargetTemperatureText)
                .DistinctUntilChanged()
                .Where(x => !HasSpecificErrors(nameof(TargetTemperatureText)))
                .Select(x => float.Parse(x.Value, NumberStyles.Any, CultureInfo.CurrentUICulture))
                .ObserveOnUi()
                .BindTo(this, x => x.ActualTemperature)
                .DisposeWith(_subscriptions);

            var actTempObs =
                this.WhenPropertyChanged(x => x.ActualTemperature)
                    .Select(x => x.Value)
                    .ObserveOnUi();

            actTempObs.BindTo(this, x => x.TargetTemperature).DisposeWith(_subscriptions);
            actTempObs.Select(x => x.ToString(
                          Properties.Localization.General_TemperatureFloatFormat,
                          CultureInfo.CurrentUICulture))
                      .BindTo(this, x => x.TargetTemperatureText)
                      .DisposeWith(_subscriptions);

            Model.Camera.WhenAnyPropertyChanged(nameof(Model.Camera.FanMode))
                 .Select(x => 2 - (uint) x.FanMode)
                 .DistinctUntilChanged()
                 .ObserveOnUi()
                 .BindTo(this, x => x.FanMode)
                 .DisposeWith(_subscriptions);

            this.WhenPropertyChanged(x => x.FanMode)
                .Select(x => (FanMode) (2 - x.Value))
                .InvokeCommand(Model.FanCommand)
                .DisposeWith(_subscriptions);


            var shutterSrc =
                Model.Camera.WhenAnyPropertyChanged(nameof(Model.Camera.Shutter))
                     .Select(x => x.Shutter)
                     .DistinctUntilChanged()
                     .ObserveOnUi();

            shutterSrc.Select(x => x.Internal)
                      .DistinctUntilChanged()
                      .BindTo(this, x => x.InternalShutterState)
                      .DisposeWith(_subscriptions);

            this.WhenPropertyChanged(x => x.InternalShutterState)
                .Select(x => x.Value)
                .DistinctUntilChanged()
                .InvokeCommand(Model.InternalShutterCommand)
                .DisposeWith(_subscriptions);

            if (CanControlExternalShutter)
            {
                shutterSrc.Select(x => x.External.ToStringEx())
                          .DistinctUntilChanged()
                          .BindTo(this, x => x.ExternalShutterMode)
                          .DisposeWith(_subscriptions);

                this.WhenPropertyChanged(x => x.ExternalShutterMode)
                    .Select(x => x.Value)
                    .DistinctUntilChanged()
                    .InvokeCommand(Model.ExternalShutterCommand)
                    .DisposeWith(_subscriptions);

            }

        }

        protected override void HookValidators()
        {
            base.HookValidators();
            
            CreateValidator(
                this.WhenAnyPropertyChanged(nameof(TargetTemperatureText))
                    .Select(x => (
                        Type: nameof(Validators.Validate.MatchesRegex),
                        Message: Validators.Validate.MatchesRegex(
                            x.TargetTemperatureText,
                            "^[+-]?[0-9]+\\.?[0-9]*$",
                            "Only numbers are allowed"))),
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
                                : float.PositiveInfinity,
                            x.MinimumAllowedTemperature,
                            x.MaximumAllowedTemperature))),
                nameof(TargetTemperatureText));

        }

        public string Name => Model.ToString();

    }
}
