//    This file is part of Dipol-3 Camera Manager.

//    Dipol-3 Camera Manager is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.

//    Dipol-3 Camera Manager is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//    GNU General Public License for more details.

//    You should have received a copy of the GNU General Public License
//    along with Dipol-3 Camera Manager.  If not, see<http://www.gnu.org/licenses/>.
//
//    Copyright 2017, Ilia Kosenkov, Tuorla Observatory, Finland


using System;
using System.Threading.Tasks;
using System.Threading;
using ANDOR_CS.Enums;
using ANDOR_CS.DataStructures;
using DipolImage;
using System.Collections.Concurrent;

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

        public override ConcurrentQueue<Image> AcquiredImages => throw new NotImplementedException();

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
                GetFunctions = GetFunction.Temperature | GetFunction.TemperatureRange,
                SetFunctions = SetFunction.Temperature,
                Features = SdkFeatures.FanControl 
                           | SdkFeatures.LowFanMode
                           | SdkFeatures.Shutter
                           | SdkFeatures.ShutterEx
            };
            Properties = new CameraProperties()
            {
                DetectorSize = new Size(256, 512),
                AllowedTemperatures = (Minimum:-50, Maximum: 30),
                HasInternalMechanicalShutter = true
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
            throw new NotImplementedException();
        }

        public override void EnableAutosave(in string pattern)
        {
            throw new NotImplementedException();
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
            await Task.Delay(TimeSpan.FromMilliseconds(1));
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
    }
}
