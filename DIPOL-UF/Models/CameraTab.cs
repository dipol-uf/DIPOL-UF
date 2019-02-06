using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ANDOR_CS.Classes;
using ANDOR_CS.Enums;
using ANDOR_CS.Events;
using DIPOL_UF.Converters;
using ReactiveUI;

namespace DIPOL_UF.Models
{
    internal sealed class CameraTab: ReactiveObjectEx
    {
        public CameraBase Camera { get; }
        public (float Minimum, float Maximum) TemperatureRange { get; }
        public bool CanControlTemperature { get; }
        public bool CanQueryTemperature { get; }
        public string Alias { get; }

        public IObservable<TemperatureStatusEventArgs> WhenTemperatureChecked { get; private set; }

        public ReactiveCommand<Unit, Unit> CoolerCommand { get; private set; }

        public CameraTab(CameraBase camera)
        {
            Camera = camera;

            TemperatureRange = camera.Capabilities.GetFunctions.HasFlag(GetFunction.TemperatureRange)
                ? camera.Properties.AllowedTemperatures
                : default;
            CanControlTemperature = camera.Capabilities.SetFunctions.HasFlag(SetFunction.Temperature);
            CanQueryTemperature = camera.Capabilities.GetFunctions.HasFlag(GetFunction.Temperature);
            Alias = ConverterImplementations.CameraToStringAliasConversion(camera);

            HookObservables();
            InitializeCommands();
        }

        private void HookObservables()
        {
            WhenTemperatureChecked =
                Observable.FromEventPattern<TemperatureStatusEventHandler, TemperatureStatusEventArgs>(
                              x => Camera.TemperatureStatusChecked += x,
                              x => Camera.TemperatureStatusChecked -= x)
                          .Select(x => x.EventArgs)
                          .DistinctUntilChanged();
        }

        private void InitializeCommands()
        {
            CoolerCommand =
                ReactiveCommand.Create(
                                   () => Camera.CoolerControl(Camera.CoolerMode == Switch.Disabled
                                       ? Switch.Enabled
                                       : Switch.Disabled),
                                   Observable.Return(
                                       Camera.Capabilities.SetFunctions.HasFlag(SetFunction.Temperature)))
                               .DisposeWith(_subscriptions);


        }

        public override void Dispose(bool disposing)
        {
            Helper.WriteLog("Disposing Tab model");
            base.Dispose(disposing);
        }
    }
}
