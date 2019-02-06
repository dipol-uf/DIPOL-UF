using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using DIPOL_UF.Models;
using DynamicData.Binding;
using ReactiveUI.Fody.Helpers;
// ReSharper disable UnassignedGetOnlyAutoProperty

namespace DIPOL_UF.ViewModels
{
    internal sealed class CameraTabViewModel : ReactiveViewModel<CameraTab>
    {
        public float MinimumAllowedTemperature => 0; // Model.TemperatureRange.Minimum;
        public float MaximumAllowedTemperature => 100; //Model.TemperatureRange.Maximum;
        public bool CanControlTemperature => Model.CanControlTemperature;
        public bool CanQueryTemperature => Model.CanQueryTemperature;
        public string TabHeader => Model.Alias;

        
        [Reactive]
        public float TargetTemperature { get; set; }
        public bool IsAcquiring { [ObservableAsProperty]get; }
        public float CurrentTemperature { [ObservableAsProperty] get; }

        public CameraTabViewModel(CameraTab model) : base(model)
        {
            HookObservables();
            HookValidators();
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
        }

        protected override void HookValidators()
        {
            CreateValidator(
                this.WhenAnyPropertyChanged(nameof(TargetTemperature))
                    .Select(x => (Type: nameof(Validators.Validate.ShouldFallWithinRange),
                        Message: Validators.Validate.ShouldFallWithinRange(
                            x.TargetTemperature,
                            20 /*x.MinimumAllowedTemperature*/,
                            50 /*x.MaximumAllowedTemperature*/))),
                nameof(TargetTemperature));

            base.HookValidators();
        }

        public string Name => Model.ToString();

    }
}
