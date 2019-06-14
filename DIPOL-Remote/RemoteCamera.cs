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
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using ANDOR_CS;
using ANDOR_CS.AcquisitionMetadata;
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
    public sealed partial class RemoteCamera : CameraBase
    {
        private static readonly ConcurrentDictionary<int, RemoteCamera> RemoteCameras
            = new ConcurrentDictionary<int, RemoteCamera>();

        private readonly ConcurrentDictionary<string, bool> _changedProperties
            = new ConcurrentDictionary<string, bool>();

        private bool _isActive;
        private DipolClient _client;

        public override IAcquisitionSettings CurrentSettings { get; protected set; }
        public override (float Exposure, float Accumulation, float Kinetic) Timings
        {
            get => _client.CallGetTimings(CameraIndex);
            protected set => throw new NotSupportedException();
        }

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
                    _isActive = _client.GetIsActive(CameraIndex);
                    _changedProperties.TryUpdate(NameofProperty(), false, true);
                    
                }

                return _isActive;
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


            _isActive = _client.GetIsActive(CameraIndex);
            CameraModel = _client.GetCameraModel(CameraIndex);
            SerialNumber = _client.GetSerialNumber(CameraIndex);
            Properties = _client.GetProperties(CameraIndex);
            IsInitialized = _client.GetIsInitialized(CameraIndex);
            FanMode = _client.GetFanMode(CameraIndex);
            CoolerMode = _client.GetCoolerMode(CameraIndex);
            Capabilities = _client.GetCapabilities(CameraIndex);
            IsAcquiring = _client.GetIsAcquiring(CameraIndex);
            Shutter = _client.GetShutter(CameraIndex);
            Software = _client.GetSoftware(CameraIndex);
            Hardware = _client.GetHardware(CameraIndex);

            RemoteCameras.TryAdd((_client.SessionID + CameraIndex).GetHashCode(), this);
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
                shutterMode,
                extrn,
                clTime,
                opTime,
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

       
        public override IAcquisitionSettings GetAcquisitionSettingsTemplate()
            => new RemoteSettings(this, _client.CreateSettings(CameraIndex), _client);

        public override Task StartAcquisitionAsync(Request metadata = default, CancellationToken cancellationToken = default)
            => _client.StartAcquisitionAsync(CameraIndex, metadata, cancellationToken);

        public override Image PullPreviewImage<T>(int index)
        {
            if (!(typeof(T) == typeof(ushort) || typeof(T) == typeof(int)))
                throw new ArgumentException($"Current SDK only supports {typeof(ushort)} and {typeof(int)} images.");

            if (CurrentSettings?.ImageArea is null)
                throw new NullReferenceException(
                    "Pulling image requires acquisition settings with specified image area applied to the current camera.");

            return PullPreviewImage(index,
                typeof(T) == typeof(ushort) ? ImageFormat.UnsignedInt16 : ImageFormat.SignedInt32);
        }

        public override Image PullPreviewImage(int index, ImageFormat format)
        {
            var data = _client.CallPullPreviewImage(CameraIndex, index, format);
            if (data.Payload is null)
                return null;
            return new Image(data.Payload, data.Width, data.Height,
                format == ImageFormat.UnsignedInt16 ? TypeCode.UInt16 : TypeCode.Int32);
        }

        public override int GetTotalNumberOfAcquiredImages()
            => _client.CallGetTotalNumberOfAcquiredImages(CameraIndex);

        public override void SetAutosave(Switch mode, ImageFormat format = ImageFormat.SignedInt32)
            => _client.CallSetAutosave(CameraIndex, mode, format);

        public override void ApplySettings(IAcquisitionSettings settings)
        {
            if (!(settings is RemoteSettings remoteSetts))
                throw new ArgumentException(nameof(settings));
            
            base.ApplySettings(settings);
            using (var memory = new MemoryStream())
            {
                remoteSetts.Serialize(memory);
                memory.Flush();
                _client.CallApplySetting(CameraIndex, remoteSetts.SettingsID, memory.GetBuffer());
            }
            

        }

        public override Task<Image[]> PullAllImagesAsync(ImageFormat format, CancellationToken token)
        {
            switch (format)
            {
                case ImageFormat.UnsignedInt16:
                case ImageFormat.SignedInt32:
                    return _client.PullAllImagesAsync(CameraIndex, format, token);
                default:
                    throw new ArgumentException("Unsupported image type.", nameof(format));
            }
        }

        public override void StartImageSavingSequence(string folderPath, string imagePattern, string filter, FitsKey[] extraKeys = null)
        {
            _client.CallStartImageSavingSequence(CameraIndex, folderPath, imagePattern, filter, extraKeys);
        }

        public override Task FinishImageSavingSequenceAsync()
        {
            return _client.FinishImageSavingSequenceAsync(CameraIndex);
        }

        public override Task<Image[]> PullAllImagesAsync<T>(CancellationToken token)
        {
            if(!(typeof(T) == typeof(ushort) || typeof(T) == typeof(int)))
                throw new ArgumentException($"Current SDK only supports {typeof(ushort)} and {typeof(int)} images.");
            return PullAllImagesAsync(typeof(T) == typeof(int) ? ImageFormat.SignedInt32 : ImageFormat.UnsignedInt16,
                token);
        }

        protected override void StartAcquisition()
            => throw new NotSupportedException();

        protected override void AbortAcquisition()
            => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                IsDisposing = true;
                if(_client.State == CommunicationState.Opened)
                    _client.RemoveCamera(CameraIndex);
                RemoteCameras.TryRemove((_client.SessionID + CameraIndex).GetHashCode(), out _);
            }

            base.Dispose(disposing);
            _client = null;
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


        internal static void NotifyRemotePropertyChanged(int globalId, string property)
        {
            if (RemoteCameras.TryGetValue(globalId, out var camera))
            {
                camera._changedProperties.AddOrUpdate(property, true, (prop, oldVal) => true);
                camera.OnPropertyChangedRemotely(property);
            }
        }

        internal static void NotifyRemoteTemperatureStatusChecked(
            int globalId, TemperatureStatusEventArgs args)
        {
            if (RemoteCameras.TryGetValue(globalId, out var camera))
                camera.OnTemperatureStatusChecked(args);
        }

        internal static void NotifyRemoteAcquisitionEventHappened(int globalId, 
            AcquisitionEventType type, AcquisitionStatusEventArgs args)
        {
            if (RemoteCameras.TryGetValue(globalId, out var camera))
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

        internal static void NotifyRemoteNewImageReceivedEventHappened(int globalId, NewImageReceivedEventArgs e)
        {
            if (RemoteCameras.TryGetValue(globalId, out var cam))
                cam.OnNewImageReceived(e);
        }

        internal static void NotifyRemoteImageSavedEventHappened(int globalId, ImageSavedEventArgs e)
        {
            if (RemoteCameras.TryGetValue(globalId, out var cam))
                cam.OnImageSaved(e);
        }

        private static string NameofProperty([System.Runtime.CompilerServices.CallerMemberName] string name = "")
            => name;

    }
}
