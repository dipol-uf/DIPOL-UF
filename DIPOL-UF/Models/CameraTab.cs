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
        public bool CanControlFan { get; }
        public bool IsThreeStateFan { get; }
        public bool CanQueryTemperature { get; }
        public (bool Internal, bool External, bool IsJoined) CanControlShutter { get; }
        public string Alias { get; }


        public IObservable<TemperatureStatusEventArgs> WhenTemperatureChecked { get; private set; }

        public ReactiveCommand<Unit, Unit> CoolerCommand { get; private set; }
        public ReactiveCommand<FanMode, Unit> FanCommand { get; private set; }
        public ReactiveCommand<ShutterMode, Unit> InternalShutterCommand { get; private set; }

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
                External: camera.Capabilities.Features.HasFlag(SdkFeatures.ShutterEx),
                IsJoined: camera.Properties.HasInternalMechanicalShutter);

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

            FanCommand =
                ReactiveCommand.Create<FanMode>(
                                   Camera.FanControl,
                                   Observable.Return(
                                       Camera.Capabilities.Features.HasFlag(SdkFeatures.FanControl)))
                               .DisposeWith(_subscriptions);

            InternalShutterCommand =
                ReactiveCommand.Create<ShutterMode>(
                                   x => Camera.ShutterControl(
                                       SettingsProvider.Settings.Get("ShutterCloseTimeMS", 27),
                                       SettingsProvider.Settings.Get("ShutterOpenTimeMS", 27),
                                       x, Camera.Shutter.External ?? ShutterMode.FullyAuto,
                                       (TtlShutterSignal) SettingsProvider.Settings.Get("TTLShutterSignal", 1)),
                                   Observable.Return(CanControlShutter.Internal))
                               .DisposeWith(_subscriptions);

        }

    }
}
