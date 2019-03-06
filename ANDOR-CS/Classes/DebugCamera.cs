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
using ANDOR_CS.Enums;
using ANDOR_CS.DataStructures;
using ANDOR_CS.Exceptions;
using DipolImage;
using FITS_CS;

#if X86
using SDK = ATMCD32CS.AndorSDK;
#endif
#if X64
using SDK = ATMCD64CS.AndorSDK;
#endif

#pragma warning disable 1591
namespace ANDOR_CS.Classes
{
    public sealed class DebugCamera : CameraBase
    {
        private static readonly Random R = new Random();
        private static readonly object Locker = new object();
        private const ConsoleColor Green = ConsoleColor.DarkGreen;
        private const ConsoleColor Red = ConsoleColor.Red;
        private const ConsoleColor Blue = ConsoleColor.Blue;
        private const ConsoleColor Yellow = ConsoleColor.DarkYellow;

        public override CameraStatus GetStatus()
        {
            WriteMessage("Status checked.", Blue);
            return CameraStatus.Idle;
        }
        public override (TemperatureStatus Status, float Temperature) GetCurrentTemperature()
        {
            WriteMessage("Current temperature returned.", Blue);
            return (Status: TemperatureStatus.Stabilized, Temperature: R.Next(-40, 25));
        }
        public override void SetActive()
            => WriteMessage("Camera is manually set active.", Green);
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
            IsActive = true;
            IsInitialized = true;
            CameraModel = "DEBUG-CAMERA-INTERFACE";
            FanMode = FanMode.Off;
            CoolerMode = Switch.Disabled;

            PropertyChanged += (sender, prop) =>
                WriteMessage($"{prop.PropertyName} was changed to " +
                             $"{GetType().GetProperty(prop.PropertyName)?.GetValue(this)}.", Yellow);
            TemperatureStatusChecked += (sender, args) => WriteMessage($"Temperature: {args.Temperature}\tStatus: {args.Status}", Blue);

            ShutterControl(ShutterMode.PermanentlyClosed, ShutterMode.PermanentlyClosed,
                SettingsProvider.Settings.Get("ShutterOpenTimeMS", 27),
                SettingsProvider.Settings.Get("ShutterCloseTimeMS", 27),
                (TtlShutterSignal)SettingsProvider.Settings.Get("TTLShutterSignal", 1));
            WriteMessage("Camera created.", Green);
        }
        public override SettingsBase GetAcquisitionSettingsTemplate()
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

        public override async Task StartAcquisitionAsync(CancellationToken token)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(1), token);
            throw new NotImplementedException();
        }

        public override Image PullPreviewImage<T>(int index)
        {
            throw new NotImplementedException();
        }

        public override Image PullPreviewImage(int index, ImageFormat format)
        {
            throw new NotImplementedException();
        }

        public override int GetTotalNumberOfAcquiredImages()
        {
            throw new NotImplementedException();
        }

        public override void SaveNextAcquisitionAs(string folderPath, string imagePattern, ImageFormat format, FitsKey[] extraKeys = null)
        {
            throw new NotImplementedException();
        }

        protected override void StartAcquisition()
        {
            throw new NotImplementedException();
        }

        protected override void AbortAcquisition()
        {
            throw new NotImplementedException();
        }

        public override void ApplySettings(SettingsBase settings)
        {
            //throw new AndorSdkException("Failed", SDK.DRV_P1INVALID,
            //    nameof(AndorSdkInitialization.SdkInstance.SetImage));
        }
    }
}
#endif
