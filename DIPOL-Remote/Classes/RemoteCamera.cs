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
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using CameraBase = ANDOR_CS.Classes.CameraBase;
using IRemoteControl = DIPOL_Remote.Interfaces.IRemoteControl;
using AcquisitionEventType = DIPOL_Remote.Enums.AcquisitionEventType;

using ANDOR_CS.Enums;
using ANDOR_CS.DataStructures;
using ANDOR_CS.Events;
using ANDOR_CS.Classes;
using ANDOR_CS.Exceptions;
using DipolImage;
using FITS_CS;

namespace DIPOL_Remote.Classes
{
    public sealed class RemoteCamera : CameraBase
    {
        private static readonly ConcurrentDictionary<(string SessionID, int CameraIndex), CameraBase> remoteCameras
            = new ConcurrentDictionary<(string SessionID, int CameraIndex), CameraBase>();


        private readonly ConcurrentDictionary<string, bool> changedProperties
            = new ConcurrentDictionary<string, bool>();

        private bool _isTemperatureMonitored;
        private IRemoteControl session;

        internal static IDictionary<(string SessionID, int CameraIndex), CameraBase> RemoteCameras
            => remoteCameras as IDictionary<(string SessionID, int CameraIndex), CameraBase>;

        public override bool IsTemperatureMonitored
        {
            get
            {
                if (changedProperties.TryGetValue(NameofProperty(), out var hasChanged) && hasChanged)
                {
                    IsTemperatureMonitored = session.GetIsTemperatureMonitored(CameraIndex);
                    changedProperties.TryUpdate(NameofProperty(), false, true);
                }

                return false;
            }
        }
        public override string CameraModel
        {
            get
            {
                if (changedProperties.TryGetValue(NameofProperty(), out bool hasChanged) && hasChanged)
                {
                    CameraModel = session.GetCameraModel(CameraIndex);
                    changedProperties.TryUpdate(NameofProperty(), false, true);
                }

                return base.CameraModel;
            }

        }
        public override string SerialNumber
        {
            get
            {
                if (changedProperties.TryGetValue(NameofProperty(), out bool hasChanged) && hasChanged)
                {
                    SerialNumber = session.GetSerialNumber(CameraIndex);
                    changedProperties.TryUpdate(NameofProperty(), false, true);
                }

                return base.SerialNumber;
            }
        }
        public override bool IsActive
        {
            get
            {
                if (changedProperties.TryGetValue(NameofProperty(), out bool hasChanged) && hasChanged)
                {
                    IsActive = session.GetIsActive(CameraIndex);
                    changedProperties.TryUpdate(NameofProperty(), false, true);
                }

                return base.IsActive;
            }
        }
        public override CameraProperties Properties
        {
            get
            {
                if (changedProperties.TryGetValue(NameofProperty(), out bool hasChanged) && hasChanged)
                {
                    Properties = session.GetProperties(CameraIndex);
                    changedProperties.TryUpdate(NameofProperty(), false, true);
                }

                return base.Properties;
            }
        }
        public override bool IsInitialized
        {
            get
            {
                if (changedProperties.TryGetValue(NameofProperty(), out bool hasChanged) && hasChanged)
                {
                    IsInitialized = session.GetIsInitialized(CameraIndex);
                    changedProperties.TryUpdate(NameofProperty(), false, true);
                }

                return base.IsInitialized;
            }
        }
        public override FanMode FanMode
        {
            get
            {
                if (changedProperties.TryGetValue(NameofProperty(), out bool hasChanged) && hasChanged)
                {
                    FanMode = session.GetFanMode(CameraIndex);
                    changedProperties.TryUpdate(NameofProperty(), false, true);
                }

                return base.FanMode;
            }
        }
        public override Switch CoolerMode
        {
            get
            {
                if (changedProperties.TryGetValue(NameofProperty(), out bool hasChanged) && hasChanged)
                {
                    CoolerMode = session.GetCoolerMode(CameraIndex);
                    changedProperties.TryUpdate(NameofProperty(), false, true);
                }

                return base.CoolerMode;
            }
        }
        public override DeviceCapabilities Capabilities
        {
            get
            {
                if (changedProperties.TryGetValue(NameofProperty(), out bool hasChanged) && hasChanged)
                {
                    Capabilities = session.GetCapabilities(CameraIndex);
                    changedProperties.TryUpdate(NameofProperty(), false, true);
                }

                return base.Capabilities;
            }
        }
        public override bool IsAcquiring
        {
            get
            {
                if (changedProperties.TryGetValue(NameofProperty(), out bool hasChanged) && hasChanged)
                {
                    IsAcquiring = session.GetIsAcquiring(CameraIndex);
                    changedProperties.TryUpdate(NameofProperty(), false, true);
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
                if (changedProperties.TryGetValue(NameofProperty(), out bool hasChanged) && hasChanged)
                {
                    Shutter = session.GetShutter(CameraIndex);
                    changedProperties.TryUpdate(NameofProperty(), false, true);
                }

                return base.Shutter;
            }
        }
        public override (Version EPROM, Version COFFile, Version Driver, Version Dll)
            Software
        {
            get
            {
                if (changedProperties.TryGetValue(NameofProperty(), out bool hasChanged) && hasChanged)
                {
                    Software = session.GetSoftware(CameraIndex);
                    changedProperties.TryUpdate(NameofProperty(), false, true);
                }

                return base.Software;
            }
        }
        public override (Version PCB, Version Decode, Version CameraFirmware)
            Hardware
        {
            get
            {
                if (changedProperties.TryGetValue(NameofProperty(), out bool hasChanged) && hasChanged)
                {
                    Hardware = session.GetHardware(CameraIndex);
                    changedProperties.TryUpdate(NameofProperty(), false, true);
                }

                return base.Hardware;
            }
        }

        //public override ConcurrentQueue<Image> AcquiredImages => throw new NotImplementedException();

        internal RemoteCamera(IRemoteControl sessionInstance, int camIndex)
        {
            session = sessionInstance ?? throw new ArgumentNullException(nameof(sessionInstance));
            CameraIndex = camIndex;

            remoteCameras.TryAdd((session.SessionID, camIndex), this);

            CameraModel = session.GetCameraModel(CameraIndex);
            SerialNumber = session.GetSerialNumber(CameraIndex);
            IsActive = session.GetIsActive(CameraIndex);
            Properties = session.GetProperties(CameraIndex);
            IsInitialized = session.GetIsInitialized(CameraIndex);
            FanMode = session.GetFanMode(CameraIndex);
            CoolerMode = session.GetCoolerMode(CameraIndex);
            Capabilities = session.GetCapabilities(CameraIndex);
            IsAcquiring = session.GetIsAcquiring(CameraIndex);
            Shutter = session.GetShutter(CameraIndex);
            Software = session.GetSoftware(CameraIndex);
            Hardware = session.GetHardware(CameraIndex);
        }

        public override CameraStatus GetStatus()
            => session.CallGetStatus(CameraIndex);
        public override (TemperatureStatus Status, float Temperature) GetCurrentTemperature()
            => session.CallGetCurrentTemperature(CameraIndex);
        public override void SetActive()
            => session.CallSetActive(CameraIndex);
        public override void FanControl(FanMode mode)
            => session.CallFanControl(CameraIndex, mode);
        public override void CoolerControl(Switch mode)
            => session.CallCoolerControl(CameraIndex, mode);
        public override void SetTemperature(int temperature)
            => session.CallSetTemperature(CameraIndex, temperature);
        public override void ShutterControl(
            ShutterMode shutterMode,
            ShutterMode extrn,
            int opTime,
            int clTime,
            TtlShutterSignal type)
            => session.CallShutterControl(
                CameraIndex,
                clTime,
                opTime,
                shutterMode,
                extrn,
                type);

        public override void ShutterControl(ShutterMode inter, ShutterMode extrn)
        {
            throw new NotImplementedException();
        }

        public override void TemperatureMonitor(Switch mode, int timeout = TempCheckTimeOutMs)
            => session.CallTemperatureMonitor(CameraIndex, mode, timeout);

        public override SettingsBase GetAcquisitionSettingsTemplate()
            => new RemoteSettings(session.SessionID, CameraIndex, session.CreateSettings(CameraIndex), session);

        public override async Task StartAcquisitionAsync(CancellationToken cancellationToken)
        {
            // TODO : ReImplement
            //_acquiredImages = new ConcurrentQueue<Image>();

            //string taskID = session.CreateAcquisitionTask(CameraIndex, timeout);

            //try
            //{
            //   await Task.Run(() =>
            //   {
            //       while (!session.IsTaskFinished(taskID))
            //       {
            //           Task.Delay(timeout).Wait();
            //           if (token.IsCancellationRequested)
            //           {
            //               session.RequestCancellation(taskID);
            //               break;
            //           }
            //       }
            //   });
            //}
            //finally
            //{
            //    session.RemoveTask(taskID);
            //}
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
            => session.CallStartAcquisition(CameraIndex);

        protected override void AbortAcquisition()
            => session.CallAbortAcquisition(CameraIndex);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                base.Dispose(disposing);
                session.RemoveCamera(CameraIndex);
                remoteCameras.TryRemove((session.SessionID, CameraIndex), out _);
                session = null;
            }
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

        internal static void NotifyRemotePropertyChanged(int camIndex, string sessionID, string property)
        {
            if (remoteCameras.TryGetValue((sessionID, camIndex), out CameraBase camera))
            {
                (camera as RemoteCamera).changedProperties.AddOrUpdate(property, true, (prop, oldVal) => true);
                (camera as RemoteCamera).OnPropertyChangedRemotely(property);
            }
        }
        internal static void NotifyRemoteTemperatureStatusChecked(
            int camIndex, string sessionID, TemperatureStatusEventArgs args)
        {

            if (remoteCameras.TryGetValue((sessionID, camIndex), out CameraBase camera))
                (camera as RemoteCamera).OnTemperatureStatusChecked(args);
        }
        internal static void NotifyRemoteAcquisitionEventHappened(int camIndex, string sessionID, 
            AcquisitionEventType type, AcquisitionStatusEventArgs args)
        {
            if (remoteCameras.TryGetValue((sessionID, camIndex), out CameraBase camera))
            {
                var remCamera = camera as RemoteCamera;

                switch (type)
                {
                    case AcquisitionEventType.Started:
                        remCamera.OnAcquisitionStarted(args);
                        return;
                    case AcquisitionEventType.Finished:
                        remCamera.OnAcquisitionFinished(args);
                        return;
                    case AcquisitionEventType.StatusChecked:
                        remCamera.OnAcquisitionStatusChecked(args);
                        return;
                    case AcquisitionEventType.ErrorReturned:
                        remCamera.OnAcquisitionErrorReturned(args);
                        return;
                    case AcquisitionEventType.Aborted:
                        remCamera.OnAcquisitionAborted(args);
                        return;
                }
            }
        }
        internal static void NotifyRemoteNewImageReceivedEventHappened(int camIndex, string sessionID, NewImageReceivedEventArgs e)
        {
            // TODO: ReImplement
            //if (remoteCameras.TryGetValue((sessionID, camIndex), out CameraBase camera))
            //{
            //    var cam = camera as RemoteCamera;

            //    var message = cam.session.PullNewImage(cam.CameraIndex);
               
            //    cam.AcquiredImages.Enqueue(new Image(message.Data, message.Width, message.Height, message.TypeCode));

            //    cam.OnNewImageReceived(e);
            //}
        }

        private static string NameofProperty([System.Runtime.CompilerServices.CallerMemberName] string name = "")
            => name;

        public static CameraBase Create(int camIndex = 0, object otherParams = null)
        {
            var commObj = (otherParams
                 ?? throw new ArgumentNullException(
                               nameof(otherParams), 
                               $"{nameof(Create)} requires additional non-null parameter."))
                as DipolClient
                ?? throw new ArgumentException(
                              $"{nameof(Create)} requires additional parameter of type {typeof(DipolClient)}.", 
                              nameof(otherParams));

            return commObj.CreateRemoteCamera(camIndex);
        }

        public static async Task<CameraBase> CreateAsync(int camIndex = 0, object otherParams = null)
        {
            var commObj = (otherParams
                           ?? throw new ArgumentNullException(
                               nameof(otherParams),
                               $"{nameof(Create)} requires additional non-null parameter."))
                          as DipolClient
                          ?? throw new ArgumentException(
                              $"{nameof(Create)} requires additional parameter of type {typeof(DipolClient)}.",
                              nameof(otherParams));

            commObj.RequestCreateRemoteCamera(camIndex);
            var resetEvent = new ManualResetEvent(false);
            if(!DipolClient.CameraCreatedEvents.TryAdd((commObj.SessionID, camIndex), (resetEvent, false)))
                throw new InvalidOperationException($"Cannot add {nameof(ManualResetEvent)} to the listening collection.");

            // TODO: The timeout for remote camera creation should come as parameter or
            // TODO: from settings file.
            var isCreated = await Task.Run(() => resetEvent.WaitOne(TimeSpan.FromMinutes(2)));


            if (!DipolClient.CameraCreatedEvents.TryGetValue((commObj.SessionID, camIndex), out var result))
                result.Success = false;

            DipolClient.CameraCreatedEvents.TryRemove((commObj.SessionID, camIndex), out _);

            if(!result.Success)
                throw new AndorSdkException("Remote instance failed to create a camera.", null);
            if(!isCreated)
                throw new CommunicationException("Response from remote instance was never received.");

            return new RemoteCamera(commObj.Remote, camIndex);
            
        }
    }
}
