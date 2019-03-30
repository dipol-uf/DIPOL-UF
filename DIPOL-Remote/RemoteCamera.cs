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
using System.Threading;
using System.Threading.Tasks;
using ANDOR_CS.Classes;
using ANDOR_CS.DataStructures;
using ANDOR_CS.Enums;
using ANDOR_CS.Events;
using DipolImage;
using DIPOL_Remote.Remote;
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

        private DipolClient _session;

        public override bool IsTemperatureMonitored
        {
            get
            {
                if (_changedProperties.TryGetValue(NameofProperty(), out var hasChanged) && hasChanged)
                {
                    IsTemperatureMonitored = _session.GetIsTemperatureMonitored(CameraIndex);
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
                    CameraModel = _session.GetCameraModel(CameraIndex);
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
                    SerialNumber = _session.GetSerialNumber(CameraIndex);
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
                    IsActive = _session.GetIsActive(CameraIndex);
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
                    Properties = _session.GetProperties(CameraIndex);
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
                    IsInitialized = _session.GetIsInitialized(CameraIndex);
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
                    FanMode = _session.GetFanMode(CameraIndex);
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
                    CoolerMode = _session.GetCoolerMode(CameraIndex);
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
                    Capabilities = _session.GetCapabilities(CameraIndex);
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
                    IsAcquiring = _session.GetIsAcquiring(CameraIndex);
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
                    Shutter = _session.GetShutter(CameraIndex);
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
                    Software = _session.GetSoftware(CameraIndex);
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
                    Hardware = _session.GetHardware(CameraIndex);
                    _changedProperties.TryUpdate(NameofProperty(), false, true);
                }

                return base.Hardware;
            }
        }

        private RemoteCamera(int camIndex, DipolClient sessionInstance)
        {
            _session = sessionInstance ?? throw new ArgumentNullException(nameof(sessionInstance));
            CameraIndex = camIndex;


            CameraModel = _session.GetCameraModel(CameraIndex);
            SerialNumber = _session.GetSerialNumber(CameraIndex);
            IsActive = _session.GetIsActive(CameraIndex);
            Properties = _session.GetProperties(CameraIndex);
            IsInitialized = _session.GetIsInitialized(CameraIndex);
            FanMode = _session.GetFanMode(CameraIndex);
            CoolerMode = _session.GetCoolerMode(CameraIndex);
            Capabilities = _session.GetCapabilities(CameraIndex);
            IsAcquiring = _session.GetIsAcquiring(CameraIndex);
            Shutter = _session.GetShutter(CameraIndex);
            Software = _session.GetSoftware(CameraIndex);
            Hardware = _session.GetHardware(CameraIndex);

            RemoteCameras.TryAdd(camIndex, this);
        }

        public override CameraStatus GetStatus()
            => _session.CallGetStatus(CameraIndex);
        public override (TemperatureStatus Status, float Temperature) GetCurrentTemperature()
            => _session.CallGetCurrentTemperature(CameraIndex);
        public override void FanControl(FanMode mode)
            => _session.CallFanControl(CameraIndex, mode);
        public override void CoolerControl(Switch mode)
            => _session.CallCoolerControl(CameraIndex, mode);
        public override void SetTemperature(int temperature)
            => _session.CallSetTemperature(CameraIndex, temperature);
        public override void ShutterControl(
            ShutterMode shutterMode,
            ShutterMode extrn,
            int opTime,
            int clTime,
            TtlShutterSignal type)
            => _session.CallShutterControl(
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
            => _session.CallTemperatureMonitor(CameraIndex, mode, timeout);

        public override SettingsBase GetAcquisitionSettingsTemplate()
            => new RemoteSettings(_session.SessionID, CameraIndex, _session.CreateSettings(CameraIndex), _session);

        public override async Task StartAcquisitionAsync(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
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
            => throw new NotSupportedException();

        protected override void AbortAcquisition()
            => _session.CallAbortAcquisition(CameraIndex);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _session.RemoveCamera(CameraIndex);
                RemoteCameras.TryRemove(CameraIndex, out _);
                _session = null;
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
