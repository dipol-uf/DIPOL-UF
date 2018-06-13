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
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.XPath;
using CameraBase = ANDOR_CS.Classes.CameraBase;
using IRemoteControl = DIPOL_Remote.Interfaces.IRemoteControl;
using AcquisitionEventType = DIPOL_Remote.Enums.AcquisitionEventType;

using ANDOR_CS.Enums;
using ANDOR_CS.DataStructures;
using ANDOR_CS.Events;
using ANDOR_CS.Classes;
using DipolImage;

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
        public override bool IsAsyncAcquisition
        {
            get
            {
                if (changedProperties.TryGetValue(NameofProperty(), out bool hasChanged) && hasChanged)
                {
                    IsAsyncAcquisition = session.GetIsAsyncAcquisition(CameraIndex);
                    changedProperties.TryUpdate(NameofProperty(), false, true);
                }

                return base.IsAsyncAcquisition;
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
            IsAsyncAcquisition = session.GetIsAsyncAcquisition(CameraIndex);
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
            int clTime,
            int opTime,
            ShutterMode inter,
            ShutterMode exter = ShutterMode.FullyAuto,
            TtlShutterSignal type = TtlShutterSignal.Low)
            => session.CallShutterControl(
                CameraIndex,
                clTime,
                opTime,
                inter,
                exter,
                type);
        public override void TemperatureMonitor(Switch mode, int timeout = TempCheckTimeOutMs)
            => session.CallTemperatureMonitor(CameraIndex, mode, timeout);

        public override SettingsBase GetAcquisitionSettingsTemplate()
            => new RemoteSettings(session.SessionID, CameraIndex, session.CreateSettings(CameraIndex), session);

        public override async Task StartAcquistionAsync(CancellationTokenSource token, int timeout)
        {
            _acquiredImages = new ConcurrentQueue<Image>();

            string taskID = session.CreateAcquisitionTask(CameraIndex, timeout);

            try
            {
               await Task.Run(() =>
               {
                   while (!session.IsTaskFinished(taskID))
                   {
                       Task.Delay(timeout).Wait();
                       if (token.IsCancellationRequested)
                       {
                           session.RequestCancellation(taskID);
                           break;
                       }
                   }
               });
            }
            finally
            {
                session.RemoveTask(taskID);
            }
        }

        public override void StartAcquisition()
            => session.CallStartAcquisition(CameraIndex);

        public override void AbortAcquisition()
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

        protected void OnPropertyChangedRemotely(
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
            if (remoteCameras.TryGetValue((sessionID, camIndex), out CameraBase camera))
            {
                var cam = camera as RemoteCamera;

                var message = cam.session.PullNewImage(cam.CameraIndex);
               
                cam.AcquiredImages.Enqueue(new Image(message.Data, message.Width, message.Height, message.TypeCode));

                cam.OnNewImageReceived(e);
            }
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

            var isCreated = await Task.Run(() => resetEvent.WaitOne(TimeSpan.FromSeconds(10)));


            if (!DipolClient.CameraCreatedEvents.TryGetValue((commObj.SessionID, camIndex), out var result))
                result.Success = false;

            DipolClient.CameraCreatedEvents.TryRemove((commObj.SessionID, camIndex), out _);

            if (isCreated && result.Success)
                return new RemoteCamera(commObj.Remote, camIndex);
            else
                throw new CommunicationException("Remote response was never received.");
            
        }
    }
}
