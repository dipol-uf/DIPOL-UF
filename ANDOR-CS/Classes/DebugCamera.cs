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
using ANDOR_CS.Events;

using ImageDisplayLib;
using System.Collections.Concurrent;

namespace ANDOR_CS.Classes
{
    public sealed class DebugCamera : CameraBase
    {
        private static readonly Random _r = new Random();
        private static volatile object _locker = new object();
        private const ConsoleColor Green = ConsoleColor.DarkGreen;
        private const ConsoleColor Red = ConsoleColor.Red;
        private const ConsoleColor Blue = ConsoleColor.Blue;

        private Task _temperatureMonitorWorker;
        private readonly CancellationTokenSource _temperatureMonitorCancellationSource
            = new CancellationTokenSource();

        public override ConcurrentQueue<Image> AcquiredImages => throw new NotImplementedException();

        private void TemperatureMonitorCycler(CancellationToken token, int delay)
        {
            while (true)
            {
                if (token.IsCancellationRequested)
                    return;

                (var status, var temp) = GetCurrentTemperature();

                OnTemperatureStatusChecked(new TemperatureStatusEventArgs(status, temp));

                Task.Delay(delay, token).Wait(token);
            }

        }

        public override CameraStatus GetStatus()
        {
            WriteMessage("Status checked.", Blue);
            return CameraStatus.Idle;
        }
        public override (TemperatureStatus Status, float Temperature) GetCurrentTemperature()
        {
            WriteMessage("Current temperature returned.", Blue);
            return (Status: TemperatureStatus.Stabilized, Temperature: 10.0f);
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

        public override void ShutterControl(int clTime, int opTime, ShutterMode inter, ShutterMode exter = ShutterMode.FullyAuto, TtlShutterSignal type = TtlShutterSignal.Low) 
            => WriteMessage("Shutter settings were changed.", Blue);

        public override void TemperatureMonitor(Switch mode, int timeout)
        {
            // If monitor shold be enbled
            if (mode == Switch.Enabled)
            {
                // If background task has not been started yet
                if (_temperatureMonitorWorker == null ||
                    _temperatureMonitorWorker.Status == TaskStatus.Canceled ||
                    _temperatureMonitorWorker.Status == TaskStatus.RanToCompletion ||
                    _temperatureMonitorWorker.Status == TaskStatus.Faulted)
                    // Starts new with a cancellation token
                    _temperatureMonitorWorker = Task.Factory.StartNew(
                        () => TemperatureMonitorCycler(_temperatureMonitorCancellationSource.Token, timeout),
                        _temperatureMonitorCancellationSource.Token);

                // If task was created, but has not started, start it
                if (_temperatureMonitorWorker.Status == TaskStatus.Created)
                    _temperatureMonitorWorker.Start();

                WriteMessage("Temperature monitor enabled.", Green);
            }
            else if (mode == Switch.Disabled)
            {
                // if there is a working background monitor
                if (_temperatureMonitorWorker?.Status == TaskStatus.Running ||
                    _temperatureMonitorWorker?.Status == TaskStatus.WaitingForActivation ||
                    _temperatureMonitorWorker?.Status == TaskStatus.WaitingToRun)
                    // Stops it via cancellation token
                    _temperatureMonitorCancellationSource.Cancel();

                WriteMessage("Temperature monitor disabled.", Red);
            }

            // If monitor should be disabled
        }
        public DebugCamera(int camIndex)
        {
            CameraIndex = camIndex;
            SerialNumber = $"XYZ-{_r.Next(9999):0000}";
            Capabilities = new DeviceCapabilities()
            {
                CameraType = CameraType.IXonUltra
            };
            Properties = new CameraProperties()
            {
                DetectorSize = new Size(256, 512)
            };
            IsActive = true;
            IsInitialized = true;
            CameraModel = "DEBUG-CAMERA-INTERFACE";
            FanMode = FanMode.Off;
            CoolerMode = Switch.Disabled;

            PropertyChanged += (sender, prop) => WriteMessage($"{prop.PropertyName} was changed.", Blue);
            TemperatureStatusChecked += (sender, args) => WriteMessage($"Temperature: {args.Temperature}\tStatus: {args.Status}", Blue);
            WriteMessage("Camera created.", Green);
        }
        public override SettingsBase GetAcquisitionSettingsTemplate()
        {
            throw new NotImplementedException();
        }

        public override void Dispose()
            => WriteMessage("Camera disposed.", Red);


        private void WriteMessage(string message, ConsoleColor col)
        {
            lock (_locker)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("[{0,-3:000}-{1:hh:mm:ss.ff}] > ", CameraIndex, DateTime.Now);
                Console.ForegroundColor = col;
                Console.WriteLine(message);
                Console.ForegroundColor = ConsoleColor.White;
            }

        }

        public async override Task StartAcquistionAsync(CancellationTokenSource token, int timeout)
        {
            throw new NotImplementedException();
        }

        public override void StartAcquisition()
        {
            throw new NotImplementedException();
        }

        public override void AbortAcquisition()
        {
            throw new NotImplementedException();
        }
    }
}
