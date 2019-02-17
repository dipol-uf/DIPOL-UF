using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ANDOR_CS.Classes;
using ANDOR_CS.Enums;
using ANDOR_CS.Events;
using DipolImage;
using DIPOL_UF.Converters;
using DIPOL_UF.Properties;
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
        public (bool Internal, bool External) CanControlShutter { get; }
        public string Alias { get; }


        public DipolImagePresenter ImagePresenter { get; private set; }

        public DescendantProvider AcquisitionSettingsWindow { get; private set; }

        public IObservable<TemperatureStatusEventArgs> WhenTemperatureChecked { get; private set; }

        public ReactiveCommand<Unit, Unit> CoolerCommand { get; private set; }
        public ReactiveCommand<FanMode, Unit> FanCommand { get; private set; }
        public ReactiveCommand<ShutterMode, Unit> InternalShutterCommand { get; private set; }
        public ReactiveCommand<ShutterMode, Unit> ExternalShutterCommand { get; private set; }

        public ReactiveCommand<Unit, object> SetUpAcquisitionCommand { get; private set; }

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

#if DEBUG
            var imageArr = new int[256 * 512];
            var byteImg = new byte[256 * 512 * sizeof(int)];
            var r = new Random();
            r.NextBytes(byteImg);

            Buffer.BlockCopy(byteImg, 0, imageArr, 0, byteImg.Length);

            var img = new Image(imageArr, 512, 256);

            Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(5));
                ImagePresenter.LoadImage(img);
            });
#endif

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
                                       x, Camera.Shutter.External ?? ShutterMode.FullyAuto),
                                   Observable.Return(CanControlShutter.Internal))
                               .DisposeWith(_subscriptions);

           ExternalShutterCommand =
                ReactiveCommand.Create<ShutterMode>(
                                   x => Camera.ShutterControl(
                                       Camera.Shutter.Internal, x),
                                   Observable.Return(CanControlShutter.External))
                               .DisposeWith(_subscriptions);

           SetUpAcquisitionCommand =
               ReactiveCommand.Create<Unit, object>(x => x)
                              .DisposeWith(_subscriptions);

           AcquisitionSettingsWindow = new DescendantProvider(
                   ReactiveCommand.Create<object, ReactiveObjectEx>(
                       _ => new ReactiveWrapper<SettingsBase>(Camera.GetAcquisitionSettingsTemplate())),
                   ReactiveCommand.Create<Unit>(_ => { }),
                   ReactiveCommand.Create<Unit>(_ => { }),
                   ReactiveCommand.Create<ReactiveObjectEx>(x => x.Dispose()))
               .DisposeWith(_subscriptions);

            SetUpAcquisitionCommand.InvokeCommand(AcquisitionSettingsWindow.ViewRequested).DisposeWith(_subscriptions);

        }

    }
}
