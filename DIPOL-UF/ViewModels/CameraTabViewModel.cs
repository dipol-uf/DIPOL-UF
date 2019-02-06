using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;
using DIPOL_UF.Models;
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
        public string TabHeader => Model.Alias;


        [Reactive]
        public float TargetTemperature { get; set; }
        [Reactive]
        public string TargetTemperatureText { get; set; } =
            0f.ToString(
                Properties.Localization.General_TemperatureFloatFormat,
                CultureInfo.CurrentUICulture);

        public bool IsAcquiring { [ObservableAsProperty]get; }
        public float CurrentTemperature { [ObservableAsProperty] get; }

        public CameraTabViewModel(CameraTab model) : base(model)
        {
            HookValidators();
            HookObservables();
        }

        private void HookObservables()
        {
            Model.Camera.WhenAnyPropertyChanged(nameof(Model.Camera.IsAcquiring))
                 .Select(x => x.IsAcquiring)
                 .ObserveOnUi()
                 .ToPropertyEx(this, x => x.IsAcquiring)
                 .DisposeWith(_subscriptions);

            Model.WhenTemperatureChecked
                 .Select(x => x.Temperature)
                 .ObserveOnUi()
                 .ToPropertyEx(this, x => x.CurrentTemperature)
                 .DisposeWith(_subscriptions);

            this.WhenPropertyChanged(x => x.TargetTemperature)
                .CombineLatest(
                    this.WhenPropertyChanged(y => y.TargetTemperatureText),
                    (x, y) => (Temp: x.Value, Text: y.Value))
                .DistinctUntilChanged()
                .Buffer(2)
                .Subscribe(x =>
                {
                    if (x.Count == 2)
                    {
                        var (temp, text) = x[0];
                        var (temp2, text2) = x[1];

                        if (!temp2.AlmostEqualRelative(temp, Precision.MachineEpsilon))
                            TargetTemperatureText =
                                temp2.ToString(
                                    Properties.Localization.General_TemperatureFloatFormat,
                                    CultureInfo.CurrentUICulture);

                        if (text2.Trim() != text.Trim()
                            && float.TryParse(text2, 
                                NumberStyles.Any,
                                CultureInfo.CurrentUICulture, 
                                out var var))
                            TargetTemperature = var;
                    }

                })
                .DisposeWith(_subscriptions);
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
