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


#if DEBUG
using System;
using System.Threading.Tasks;
using System.Threading;
using ANDOR_CS.AcquisitionMetadata;
using ANDOR_CS.Enums;
using ANDOR_CS.DataStructures;
using ANDOR_CS.Events;
using DipolImage;
using FITS_CS;

#pragma warning disable 1591
namespace ANDOR_CS.Classes
{
    public sealed partial class DebugCamera : CameraBase
    {
        private static readonly Random R = new Random();
        private static readonly object Locker = new object();
        private const ConsoleColor Green = ConsoleColor.DarkGreen;
        private const ConsoleColor Red = ConsoleColor.Red;
        private const ConsoleColor Blue = ConsoleColor.Blue;
        private const ConsoleColor Yellow = ConsoleColor.DarkYellow;

        public override bool IsActive => true;

        public override CameraStatus GetStatus()
        {
            //WriteMessage("Status checked.", Blue);
            return CameraStatus.Idle;
        }
        public override (TemperatureStatus Status, float Temperature) GetCurrentTemperature()
        {
            //WriteMessage("Current temperature returned.", Blue);
            return (Status: TemperatureStatus.Stabilized, Temperature: R.Next(-40, 25));
        }
        //public override void SetActive()
            //=> WriteMessage("Camera is manually set active.", Green);
        public override void FanControl(FanMode mode)
        {
            FanMode = mode;
            WriteMessage($"Fan mode is set to {mode}", Blue);
        }
        public override void CoolerControl(Switch mode)
        {
            CoolerMode = mode;
            WriteMessage($"Cooler mode is set to {mode}", Blue);
        }
        public override void SetTemperature(int temperature) 
            => WriteMessage($"Temperature was set to {temperature}.", Blue);

        public override void ShutterControl(
            ShutterMode inter,
            ShutterMode extrn, 
            int opTime, int clTime,
            TtlShutterSignal type)
        {
            Shutter = (Internal: inter, External: extrn, Type: type, OpenTime: opTime, CloseTime: clTime);
            WriteMessage("Shutter settings were changed.", Blue);
        }

        public override void ShutterControl(
            ShutterMode inter,
            ShutterMode extrn)
        {
            ShutterControl(inter, extrn,
                SettingsProvider.Settings.Get("ShutterOpenTimeMS", 27),
                SettingsProvider.Settings.Get("ShutterCloseTimeMS", 27),
                (TtlShutterSignal)SettingsProvider.Settings.Get("TTLShutterSignal", 1));
        }

        public DebugCamera(int camIndex)
        {
            Task.Delay(TimeSpan.FromSeconds(1.5)).GetAwaiter().GetResult();
            
            CameraIndex = camIndex;
            SerialNumber = $"XYZ-{R.Next(9999):0000}";
            Capabilities = new DeviceCapabilities()
            {
                CameraType = CameraType.IXonUltra,
                AcquisitionModes = AcquisitionMode.SingleScan
                                   | AcquisitionMode.RunTillAbort
                                   | AcquisitionMode.Accumulation
                                   | AcquisitionMode.FastKinetics
                                   | AcquisitionMode.Kinetic,
                GetFunctions = GetFunction.Temperature | GetFunction.TemperatureRange,
                SetFunctions = SetFunction.Temperature
                               | SetFunction.VerticalReadoutSpeed
                               | SetFunction.VerticalClockVoltage
                               | SetFunction.HorizontalReadoutSpeed
                               | SetFunction.PreAmpGain
                               | SetFunction.EMCCDGain,
                Features = SdkFeatures.FanControl
                           | SdkFeatures.LowFanMode
                           | SdkFeatures.Shutter
                           | SdkFeatures.ShutterEx,
                TriggerModes = TriggerMode.Internal | TriggerMode.External,
                ReadModes = ReadMode.FullImage | ReadMode.SubImage,
                FtReadModes = ReadMode.FullImage | ReadMode.FullVerticalBinning
            };
            Properties = new CameraProperties()
            {
                DetectorSize = new Size(256, 512),
                AllowedTemperatures = (Minimum:-50, Maximum: 30),
                HasInternalMechanicalShutter = true,
                VSSpeeds = new float[] {1, 3, 5, 10},
                ADConverters = new [] {16, 32},
                OutputAmplifiers = new (string Name, OutputAmplification OutputAmplifier, float MaxSpeed)[]
                {
                    (@"EMCCD", OutputAmplification.ElectronMultiplication, 10),
                    (@"Conventional", OutputAmplification.Conventional, 100)
                },
                PreAmpGains = new []{"Gain1", "Gain2"}
            };
            IsInitialized = true;
            CameraModel = "DEBUG-CAMERA-INTERFACE";
            FanMode = FanMode.Off;
            CoolerMode = Switch.Disabled;

            PropertyChanged += (sender, prop) =>
                WriteMessage($"{prop.PropertyName} was changed to " +
                             $"{GetType().GetProperty(prop.PropertyName)?.GetValue(this)}.", Yellow);
            //TemperatureStatusChecked += (sender, args) => WriteMessage($"Temperature: {args.Temperature}\tStatus: {args.Status}", Blue);

            ShutterControl(ShutterMode.PermanentlyClosed, ShutterMode.PermanentlyClosed,
                SettingsProvider.Settings.Get("ShutterOpenTimeMS", 27),
                SettingsProvider.Settings.Get("ShutterCloseTimeMS", 27),
                (TtlShutterSignal)SettingsProvider.Settings.Get("TTLShutterSignal", 1));
            WriteMessage("Camera created.", Green);
        }

       

        public override IAcquisitionSettings GetAcquisitionSettingsTemplate()
        {
            return new DebugSettings(this);
        }

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    IsDisposing = true;
                }
            }
            base.Dispose(disposing);
            WriteMessage("Camera disposed.", Red);
        }


        private void WriteMessage(string message, ConsoleColor col)
        {
            lock (Locker)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("[{0,-3:000}-{1:hh:mm:ss.ff}] > ", CameraIndex, DateTime.Now);
                Console.ForegroundColor = col;
                Console.WriteLine(message);
                Console.ForegroundColor = ConsoleColor.White;
            }

        }

        public override async Task StartAcquisitionAsync(Request metadata, CancellationToken token)
        {
            StartAcquisition();
            OnAcquisitionStarted(new AcquisitionStatusEventArgs(CameraStatus.Acquiring));
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(Timings.Kinetic), token);
                token.ThrowIfCancellationRequested();

                OnNewImageReceived(new NewImageReceivedEventArgs(0, DateTimeOffset.Now));
            }
            catch (Exception)
            {
                OnAcquisitionAborted(new AcquisitionStatusEventArgs(CameraStatus.Idle));
            }
            finally
            {
                OnAcquisitionFinished(new AcquisitionStatusEventArgs(CameraStatus.Idle));
                IsAcquiring = false;
            }
        }

        public override Image PullPreviewImage<T>(int index)
        {
            if (!(typeof(T) == typeof(ushort) || typeof(T) == typeof(int)))
                throw new ArgumentException($"Current SDK only supports {typeof(ushort)} and {typeof(int)} images.");

            if (CurrentSettings?.ImageArea is null)
                throw new NullReferenceException(
                    "Pulling image requires acquisition settings with specified image area applied to the current camera.");

            var size = CurrentSettings.ImageArea.Value; // -V3125
            var matrixSize = size.Width * size.Height;
            var r = new Random();
            var sz = typeof(T) == typeof(ushort) ? sizeof(ushort) : sizeof(int);

            var data = new byte[matrixSize * sz];
            r.NextBytes(data);
            return new Image(data, size.Width, size.Height,
                typeof(T) == typeof(ushort) ? TypeCode.UInt16 : TypeCode.Int32);

        }

        public override int GetTotalNumberOfAcquiredImages()
            => 1;

        public override Task<Image[]> PullAllImagesAsync(ImageFormat format, CancellationToken token)
        {
            return Task.FromResult(new[] {PullPreviewImage(0, format), PullPreviewImage(0, format)});
        }

        public override void StartImageSavingSequence(string folderPath, string imagePattern, string filter, FitsKey[] extraKeys = null)
        {
            Console.WriteLine(@"Start saving sequence");
        }
        public override Task FinishImageSavingSequenceAsync()
        {
            Console.WriteLine(@"Stopped saving sequence");
            return Task.CompletedTask;
        }

        protected override void StartAcquisition()
        {
            IsAcquiring = true;
        }

        protected override void AbortAcquisition()
        {
            IsAcquiring = false;
        }

        public override void ApplySettings(IAcquisitionSettings settings)
        {
            var delta = 0.5f * CameraIndex;
            Timings = (1.5f + delta, 1.5f + delta, 1.5f + delta);
            base.ApplySettings(settings);
        }

        public new static DebugCamera Create(int camIndex = 0, params object[] @params)
            => new DebugCamera(camIndex);

        public new static async Task<DebugCamera> CreateAsync(int camIndex = 0, params object[] @params)
            => await Task.Run(() => Create(camIndex, @params));
    }
}
#endif
