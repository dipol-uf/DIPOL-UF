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

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ANDOR_CS.Classes;
using ANDOR_CS.DataStructures;
using ANDOR_CS.Enums;
using ANDOR_CS.Events;
using DipolImage;
using FITS_CS;
using CameraBase = ANDOR_CS.Classes.CameraBase;
using AcquisitionEventType = DIPOL_Remote.Enums.AcquisitionEventType;

namespace DIPOL_Remote
{
    public sealed class RemoteCamera : CameraBase
    {
        internal static readonly ConcurrentDictionary<int, RemoteCamera> RemoteCameras
            = new ConcurrentDictionary<int, RemoteCamera>();

        private readonly ConcurrentDictionary<string, bool> _changedProperties
            = new ConcurrentDictionary<string, bool>();

        private DipolClient _client;

        public override bool IsTemperatureMonitored
        {
            get
            {
                if (_changedProperties.TryGetValue(NameofProperty(), out var hasChanged) && hasChanged)
                {
                    IsTemperatureMonitored = _client.GetIsTemperatureMonitored(CameraIndex);
                    _changedProperties.TryUpdate(NameofProperty(), false, true);
                }

                return base.IsTemperatureMonitored;
            }
        }
        public override string CameraModel
        {
            get
            {
                if (_changedProperties.TryGetValue(NameofProperty(), out var hasChanged) && hasChanged)
                {
                    CameraModel = _client.GetCameraModel(CameraIndex);
                    _changedProperties.TryUpdate(NameofProperty(), false, true);
                }

                return base.CameraModel;
            }

        }
        public override string SerialNumber
        {
            get
            {
                if (_changedProperties.TryGetValue(NameofProperty(), out var hasChanged) && hasChanged)
                {
                    SerialNumber = _client.GetSerialNumber(CameraIndex);
                    _changedProperties.TryUpdate(NameofProperty(), false, true);
                }

                return base.SerialNumber;
            }
        }
        public override bool IsActive
        {
            get
            {
                if (_changedProperties.TryGetValue(NameofProperty(), out var hasChanged) && hasChanged)
                {
                    IsActive = _client.GetIsActive(CameraIndex);
                    _changedProperties.TryUpdate(NameofProperty(), false, true);
                }

                return base.IsActive;
            }
        }
        public override CameraProperties Properties
        {
            get
            {
                if (_changedProperties.TryGetValue(NameofProperty(), out var hasChanged) && hasChanged)
                {
                    Properties = _client.GetProperties(CameraIndex);
                    _changedProperties.TryUpdate(NameofProperty(), false, true);
                }

                return base.Properties;
            }
        }
        public override bool IsInitialized
        {
            get
            {
                if (_changedProperties.TryGetValue(NameofProperty(), out var hasChanged) && hasChanged)
                {
                    IsInitialized = _client.GetIsInitialized(CameraIndex);
                    _changedProperties.TryUpdate(NameofProperty(), false, true);
                }

                return base.IsInitialized;
            }
        }
        public override FanMode FanMode
        {
            get
            {
                if (_changedProperties.TryGetValue(NameofProperty(), out var hasChanged) && hasChanged)
                {
                    FanMode = _client.GetFanMode(CameraIndex);
                    _changedProperties.TryUpdate(NameofProperty(), false, true);
                }

                return base.FanMode;
            }
        }
        public override Switch CoolerMode
        {
            get
            {
                if (_changedProperties.TryGetValue(NameofProperty(), out var hasChanged) && hasChanged)
                {
                    CoolerMode = _client.GetCoolerMode(CameraIndex);
                    _changedProperties.TryUpdate(NameofProperty(), false, true);
                }

                return base.CoolerMode;
            }
        }
        public override DeviceCapabilities Capabilities
        {
            get
            {
                if (_changedProperties.TryGetValue(NameofProperty(), out var hasChanged) && hasChanged)
                {
                    Capabilities = _client.GetCapabilities(CameraIndex);
                    _changedProperties.TryUpdate(NameofProperty(), false, true);
                }

                return base.Capabilities;
            }
        }
        public override bool IsAcquiring
        {
            get
            {
                if (_changedProperties.TryGetValue(NameofProperty(), out var hasChanged) && hasChanged)
                {
                    IsAcquiring = _client.GetIsAcquiring(CameraIndex);
                    _changedProperties.TryUpdate(NameofProperty(), false, true);
                }

                return base.IsAcquiring;
            }
        }
        public override (
           ShutterMode Internal,
           ShutterMode? External,
           TtlShutterSignal Type,
           int OpenTime,
           int CloseTime) Shutter
        {
            get
            {
                if (_changedProperties.TryGetValue(NameofProperty(), out var hasChanged) && hasChanged)
                {
                    Shutter = _client.GetShutter(CameraIndex);
                    _changedProperties.TryUpdate(NameofProperty(), false, true);
                }

                return base.Shutter;
            }
        }
        public override (Version EPROM, Version COFFile, Version Driver, Version Dll)
            Software
        {
            get
            {
                if (_changedProperties.TryGetValue(NameofProperty(), out var hasChanged) && hasChanged)
                {
                    Software = _client.GetSoftware(CameraIndex);
                    _changedProperties.TryUpdate(NameofProperty(), false, true);
                }

                return base.Software;
            }
        }
        public override (Version PCB, Version Decode, Version CameraFirmware)
            Hardware
        {
            get
            {
                if (_changedProperties.TryGetValue(NameofProperty(), out var hasChanged) && hasChanged)
                {
                    Hardware = _client.GetHardware(CameraIndex);
                    _changedProperties.TryUpdate(NameofProperty(), false, true);
                }

                return base.Hardware;
            }
        }

        private RemoteCamera(int camIndex, DipolClient sessionInstance)
        {
            _client = sessionInstance ?? throw new ArgumentNullException(nameof(sessionInstance));
            CameraIndex = camIndex;


            CameraModel = _client.GetCameraModel(CameraIndex);
            SerialNumber = _client.GetSerialNumber(CameraIndex);
            IsActive = _client.GetIsActive(CameraIndex);
            Properties = _client.GetProperties(CameraIndex);
            IsInitialized = _client.GetIsInitialized(CameraIndex);
            FanMode = _client.GetFanMode(CameraIndex);
            CoolerMode = _client.GetCoolerMode(CameraIndex);
            Capabilities = _client.GetCapabilities(CameraIndex);
            IsAcquiring = _client.GetIsAcquiring(CameraIndex);
            Shutter = _client.GetShutter(CameraIndex);
            Software = _client.GetSoftware(CameraIndex);
            Hardware = _client.GetHardware(CameraIndex);

            RemoteCameras.TryAdd(camIndex, this);
        }

        public override CameraStatus GetStatus()
            => _client.CallGetStatus(CameraIndex);
        public override (TemperatureStatus Status, float Temperature) GetCurrentTemperature()
            => _client.CallGetCurrentTemperature(CameraIndex);
        public override void FanControl(FanMode mode)
            => _client.CallFanControl(CameraIndex, mode);
        public override void CoolerControl(Switch mode)
            => _client.CallCoolerControl(CameraIndex, mode);
        public override void SetTemperature(int temperature)
            => _client.CallSetTemperature(CameraIndex, temperature);
        public override void ShutterControl(
            ShutterMode shutterMode,
            ShutterMode extrn,
            int opTime,
            int clTime,
            TtlShutterSignal type)
            => _client.CallShutterControl(
                CameraIndex,
                clTime,
                opTime,
                shutterMode,
                extrn,
                type);

        public override void ShutterControl(ShutterMode inter, ShutterMode extrn)
        {
            ShutterControl(inter, extrn,
                SettingsProvider.Settings.Get("ShutterOpenTimeMS", 27),
                SettingsProvider.Settings.Get("ShutterCloseTimeMS", 27),
                (TtlShutterSignal)SettingsProvider.Settings.Get("TTLShutterSignal", 1));
        }

        public override void TemperatureMonitor(Switch mode, int timeout = TempCheckTimeOutMs)
            => _client.CallTemperatureMonitor(CameraIndex, mode, timeout);

        public override SettingsBase GetAcquisitionSettingsTemplate()
            => new RemoteSettings(this, _client.CreateSettings(CameraIndex), _client);

        public override Task StartAcquisitionAsync(CancellationToken cancellationToken)
            => _client.StartAcquisitionAsync(CameraIndex, cancellationToken);

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

        public override void ApplySettings(SettingsBase settings)
        {
            if (!(settings is RemoteSettings remoteSetts))
                throw new ArgumentException(nameof(settings));

            using (var memory = new MemoryStream())
            {
                remoteSetts.Serialize(memory);
                memory.Flush();
                _client.CallApplySetting(CameraIndex, remoteSetts.SettingsID, memory.GetBuffer());
            }

            base.ApplySettings(settings);
        }

        protected override void StartAcquisition()
            => throw new NotSupportedException();

        protected override void AbortAcquisition()
            => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _client.RemoveCamera(CameraIndex);
                RemoteCameras.TryRemove(CameraIndex, out _);
                _client = null;
            }
                base.Dispose(disposing);
        }

        protected override void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string property = "")
            => OnPropertyChangedRemotely(property, true);

        private void OnPropertyChangedRemotely(
            [System.Runtime.CompilerServices.CallerMemberName] string property = "",
            bool suppressBaseEvent = false)
        {
            //CheckIsDisposed();
            if (!IsDisposed && !suppressBaseEvent)
                base.OnPropertyChanged(property);
        }


        internal static void NotifyRemotePropertyChanged(int camIndex, string property)
        {
            if (RemoteCameras.TryGetValue(camIndex, out var camera))
            {
                camera._changedProperties.AddOrUpdate(property, true, (prop, oldVal) => true);
                camera.OnPropertyChangedRemotely(property);
            }
        }

        internal static void NotifyRemoteTemperatureStatusChecked(
            int camIndex, TemperatureStatusEventArgs args)
        {
            if (RemoteCameras.TryGetValue(camIndex, out var camera))
                camera.OnTemperatureStatusChecked(args);
        }

        internal static void NotifyRemoteAcquisitionEventHappened(int camIndex, 
            AcquisitionEventType type, AcquisitionStatusEventArgs args)
        {
            if (RemoteCameras.TryGetValue(camIndex, out var camera))
            {
                switch (type)
                {
                    case AcquisitionEventType.Started:
                        camera.OnAcquisitionStarted(args);
                        return;
                    case AcquisitionEventType.Finished:
                        camera.OnAcquisitionFinished(args);
                        return;
                    case AcquisitionEventType.StatusChecked:
                        camera.OnAcquisitionStatusChecked(args);
                        return;
                    case AcquisitionEventType.ErrorReturned:
                        camera.OnAcquisitionErrorReturned(args);
                        return;
                    case AcquisitionEventType.Aborted:
                        camera.OnAcquisitionAborted(args);
                        return;
                }
            }
        }

        internal static void NotifyRemoteNewImageReceivedEventHappened(int camIndex, NewImageReceivedEventArgs e)
        {
            // TODO: ReImplement
        }

        private static string NameofProperty([System.Runtime.CompilerServices.CallerMemberName] string name = "")
            => name;

        public new static RemoteCamera Create(int camIndex = 0, params object[] @params)
        {
            if (!(@params?.Length == 1 && @params[0] is DipolClient client))
                throw new ArgumentException(
                              $"{nameof(Create)} requires additional parameter of type {typeof(DipolClient)}.",
                              nameof(@params));

            client.CreateRemoteCamera(camIndex);

            return new RemoteCamera(camIndex, client);
        }

        public new static async Task<RemoteCamera> CreateAsync(int camIndex = 0, params object[] @params)
        {
            if (!(@params?.Length == 1 && @params[0] is DipolClient client))
                throw new ArgumentException(
                    $"{nameof(Create)} requires additional parameter of type {typeof(DipolClient)}.",
                    nameof(@params));

            await client.CreateCameraAsync(camIndex);
            return new RemoteCamera(camIndex, client);
        }
    }
}
