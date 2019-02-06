﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
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
        [Reactive]
        private float ActualTemperature { get; set; }

        public float MinimumAllowedTemperature => Model.TemperatureRange.Minimum;
        public float MaximumAllowedTemperature => Model.TemperatureRange.Maximum;
        public bool CanControlTemperature => Model.CanControlTemperature;
        public bool CanQueryTemperature => Model.CanQueryTemperature;
        public string TabHeader => Model.Alias;


        [Reactive]
        public float TargetTemperature { get; set; }
        [Reactive]
        public string TargetTemperatureText { get; set; }

        public bool IsAcquiring { [ObservableAsProperty]get; }
        public float CurrentTemperature { [ObservableAsProperty] get; }

        public CameraTabViewModel(CameraTab model) : base(model)
        {
            TargetTemperatureText = "0";
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
