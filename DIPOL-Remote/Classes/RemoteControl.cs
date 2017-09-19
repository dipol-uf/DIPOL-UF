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

//#define NO_ACTUAL_CAMERA

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

using System.ServiceModel;


using DIPOL_Remote.Faults;

using ANDOR_CS.Classes;
using ANDOR_CS.Exceptions;
using ANDOR_CS.DataStructures;
using ANDOR_CS.Enums;
using ANDOR_CS.Interfaces;

using DIPOL_Remote.Interfaces;

namespace DIPOL_Remote.Classes
{
    /// <summary>
    /// Implementation of <see cref="IRemoteControl"/> service interface.
    /// This class should not be utilized directly.
    /// Instances are executed on server (service) side.
    /// </summary>
    [ServiceBehavior(
        ConcurrencyMode = ConcurrencyMode.Multiple, 
        InstanceContextMode = InstanceContextMode.PerSession,
        UseSynchronizationContext = true,
        IncludeExceptionDetailInFaults = true)]
    public sealed class RemoteControl : IRemoteControl, IDisposable
    {
        /// <summary>
        /// Defines max number of attempts to create unique identifier.
        /// </summary>
        private static readonly int MaxTryAddAttempts = 30;

        /// <summary>
        /// Unique ID of the current session
        /// </summary>
        private string sessionID = null;
        /// <summary>
        /// Operation context of current connection. Used for callbacks
        /// </summary>
        private OperationContext context;
        /// <summary>
        /// A reference to Host instance
        /// </summary>
        private DipolHost host;

        /// <summary>
        /// Thread-safe collection of all active instances of AcquisitionSettings.
        /// </summary>
        private ConcurrentDictionary<string,  SettingsBase> settings
            = new ConcurrentDictionary<string,  SettingsBase>();

        private ConcurrentDictionary<string, (Task Task, CancellationTokenSource Token, int CameraIndex)> activeTasks
            = new ConcurrentDictionary<string, (Task Task, CancellationTokenSource Token, int CameraIndex)>();

        /// <summary>
        /// Thread-safe collection of all active <see cref="RemoteControl"/> service instances.
        /// </summary>
        private static ConcurrentDictionary<string, RemoteControl> serviceInstances 
            = new ConcurrentDictionary<string, RemoteControl>();
        /// <summary>
        /// Thread-safe collection of active remote cameras.
        /// </summary>
        private static ConcurrentDictionary<int, (string SessionID, CameraBase Camera)> activeCameras
            = new ConcurrentDictionary<int, (string SessionID, CameraBase Camera)>();


        /// <summary>
        /// Unique ID of current session
        /// </summary>
        public string SessionID
        {
            [OperationBehavior]
            get => sessionID;

        }
        /// <summary>
        /// Interface to collection of all active <see cref="AcquisitionSettings"/> instances.
        /// </summary>
        public IReadOnlyDictionary<string,  SettingsBase> Settings
           => settings as IReadOnlyDictionary<string, SettingsBase>;
        public IReadOnlyDictionary<string, (Task Task, CancellationTokenSource Token, int CameraIndex)> ActiveTasks
            => activeTasks as IReadOnlyDictionary<string, (Task Task, CancellationTokenSource Token, int CameraIndex)>;
        /// <summary>
        /// Interface to collection of all active <see cref="RemoteControl"/> service instances.
        /// </summary>
        public static IReadOnlyDictionary<string, RemoteControl> ActiveConnections
            => serviceInstances as IReadOnlyDictionary<string, RemoteControl>;
        /// <summary>
        /// Interface to collectio of all active cameras of all sessions
        /// </summary>
        public static IReadOnlyDictionary<int, (string SessionID, CameraBase Camera)> ActiveCameras
            => activeCameras as IReadOnlyDictionary<int, (string SessionID, CameraBase Camera)>;
       
        /// <summary>
        /// Default constructor
        /// </summary>
        private RemoteControl()
        {
         
        }
                   

        /// <summary>
        /// Entry point of any connection.
        /// </summary>
        [OperationBehavior]
        public void Connect()
        {
            // Stores current context
            context = OperationContext.Current;
            // Assigns session ID
            sessionID = Guid.NewGuid().ToString("N");

            
            int count = 0;
            // Stores current instance of service class into collection

            for(;
                !serviceInstances.TryAdd(sessionID, this) & count < MaxTryAddAttempts;
                count++)
                sessionID = Guid.NewGuid().ToString("N");
           

            if (count >= MaxTryAddAttempts)
                throw new FaultException<ServiceException>(
                    new ServiceException()
                    {
                        Message = "Initialization of connection failed.",
                        Details = "Unable to generate unique session ID. " +
                        "Failed to add current session to the pool of active sessions.",
                        MethodName = nameof(serviceInstances.TryAdd)
                    }, 
                    ServiceException.GeneralServiceErrorReason
                    );


            // Looks up for a host with the same endpoint
            host = DipolHost.OpenedHosts.FirstOrDefault(item => item.Key == context.Host.BaseAddresses[0].GetHashCode()).Value;

            host?.OnEventReceived("Host", $"Session {SessionID} established.");

        }
        /// <summary>
        /// Exit point for any connection. Frees resources.
        /// </summary>
        [OperationBehavior]
        public void Disconnect()
        {
           Dispose();
           host?.OnEventReceived("Host", $"Session {SessionID} closed.");
        }
 

        /// <summary>
        /// Implementation of <see cref="IDisposable"/> interface. Frees resources.
        /// </summary>
        public void Dispose()
        {
            try
            {
                // Select all cameras that are created from this session
                foreach (var key in
                    from item
                    in activeCameras
                    where item.Value.SessionID == SessionID
                    select item.Key)
                    // Remove these cameras from te collection
                    if (activeCameras.TryRemove(key, out (string SessionID, CameraBase Camera) camInfo))
                        // If successful, dispose camera instance
                        camInfo.Camera.Dispose();

                foreach (var key in settings.Keys)
                    RemoveSettings(key);

                foreach (var key in activeTasks.Keys)
                    RemoveTask(key);

                // Remove this session from collection
                serviceInstances.TryRemove(sessionID, out _);
            }
            catch (Exception e)
            {
                throw new FaultException<ServiceException>(
                    new ServiceException()
                    {
                        Message = "An exception was thrown while dsiposing session resources.",
                        Details = e.Message
                    },
                    ServiceException.GeneralServiceErrorReason);
            }
        }


        /// <summary>
        /// Returns number of available cameras.
        /// </summary>
        /// <exception cref="FaultException{AndorSDKException}"/>
        /// <exception cref="FaultException{ServiceException}"/>
        /// <returns>Number of available remote cameras</returns>
        [OperationBehavior]
        public int GetNumberOfCameras()
        {
            try
            {
#if NO_ACTUAL_CAMERA
                return 3;
#else
                // Trys to retrieve the number of available cameras
                return Camera.GetNumberOfCameras();
#endif

            }
            // If method fails and Andor-related exception is thrown
            catch (AndorSDKException andorEx)
            {
                // rethrow it, wrapped in FaultException<>, to the client side
                throw AndorSDKServiceException.WrapAndorSDKException(andorEx, nameof(Camera.GetNumberOfCameras));
          
            }
            // If failure is not realted to Andor API
            catch (Exception ex)
            {
                // rethrow it, wrapped in FaultException<>, to the client side
                throw new FaultException<ServiceException>(
                    new ServiceException()
                    {
                        Message = "Failed retrieving number of available cameras.",
                        Details = ex.Message,
                        MethodName = nameof(Camera.GetNumberOfCameras)
                    },
                    ServiceException.CameraCommunicationReason);

            }
            
        }
        [OperationBehavior]
        public void CreateCamera(int camIndex = 0)
        {

            CameraBase camera;
            try
            {
                // Tries to create new remote camera
#if NO_ACTUAL_CAMERA
                camera = Camera.GetDebugInterface(camIndex);

#else
                camera = new Camera(camIndex);
#endif
            }
            // Andor-related exception
            catch (AndorSDKException andorEx)
            {
                throw AndorSDKServiceException.WrapAndorSDKException(andorEx, nameof(Camera));
            }
            // Other possible exceptions
            catch (Exception ex)
            {
                throw new FaultException<ServiceException>(
                    new ServiceException()
                    {
                        Message = "Failed to create new remote camera.",
                        Details = ex.Message,
                        MethodName = nameof(Camera)
                    },
                    ServiceException.CameraCommunicationReason);
            }

         

            if(!activeCameras.TryAdd(camera.CameraIndex, (sessionID, camera)))
            {
                // Clean & and throw exception
                camera.Dispose();
                throw new FaultException<ServiceException>(
                    new ServiceException()
                    {
                        Message = "Failed to add new remote camera to the dictionary.",
                        Details = "Camera with this index may already exist.",
                        MethodName = nameof(activeCameras.TryAdd)
                    },
                    ServiceException.GeneralServiceErrorReason);
            }

            

            // Remotely fires event, informing that some property has changed
            camera.PropertyChanged += (sender, e)
                => context.GetCallbackChannel<IRemoteCallback>()
                .NotifyRemotePropertyChanged(
                    camera.CameraIndex,
                    SessionID,
                    e.PropertyName);

            // Remotely fires event, informing that temperature status was checked.
            camera.TemperatureStatusChecked += (sender, e)
                => context.GetCallbackChannel<IRemoteCallback>()
                .NotifyRemoteTemperatureStatusChecked(
                    camera.CameraIndex,
                    SessionID,
                    e);
            // Remotely fires event, informing that acquisition was started.
            camera.AcquisitionStarted += (snder, e)
                => context.GetCallbackChannel<IRemoteCallback>()
                .NotifyRemoteAcquisitionEventHappened(
                    camera.CameraIndex,
                    SessionID,
                    Enums.AcquisitionEventType.Started,
                    e);
            // Remotely fires event, informing that acquisition was finished.
            camera.AcquisitionFinished += (snder, e)
                => context.GetCallbackChannel<IRemoteCallback>()
                .NotifyRemoteAcquisitionEventHappened(
                    camera.CameraIndex,
                    SessionID,
                    Enums.AcquisitionEventType.Finished,
                    e);
            // Remotely fires event, informing that acquisition progress was checked.
            camera.AcquisitionStatusChecked += (snder, e)
                => context.GetCallbackChannel<IRemoteCallback>()
                .NotifyRemoteAcquisitionEventHappened(
                    camera.CameraIndex,
                    SessionID,
                    Enums.AcquisitionEventType.StatusChecked,
                    e);
            // Remotely fires event, informing that acquisition was aborted.
            camera.AcquisitionAborted += (snder, e)
                => context.GetCallbackChannel<IRemoteCallback>()
                .NotifyRemoteAcquisitionEventHappened(
                    camera.CameraIndex,
                    SessionID,
                    Enums.AcquisitionEventType.Aborted,
                    e);
            // Remotely fires event, informing that an error happened during acquisition process.
            camera.AcquisitionErrorReturned += (snder, e)
                => context.GetCallbackChannel<IRemoteCallback>()
                .NotifyRemoteAcquisitionEventHappened(
                    camera.CameraIndex,
                    SessionID,
                    Enums.AcquisitionEventType.ErrorReturned,
                    e);
            // Remotely fires event, informing that new image was acquired
            camera.NewImageReceived += (sndr, e)
                => context.GetCallbackChannel<IRemoteCallback>()
                .NotifyRemoteNewImageReceivedEventHappened(
                    camera.CameraIndex,
                    SessionID,
                    e);

            camera.TemperatureStatusChecked += (sender, e)
                => host?.OnEventReceived(sender, $"{e.Status} {e.Temperature}");

            camera.PropertyChanged += (sender, e)
                => host?.OnEventReceived(sender, e.PropertyName);

            camera.AcquisitionStarted += (sender, e)
                => host?.OnEventReceived(sender, $"Acq. Started      {e.Status} {(e.IsAsync ? "Async" : "Serial")}");
            camera.AcquisitionFinished += (sender, e)
                => host?.OnEventReceived(sender, $"Acq. Finished     {e.Status} {(e.IsAsync ? "Async" : "Serial")}");
            camera.AcquisitionStatusChecked += (sender, e)
                => host?.OnEventReceived(sender, $"Acq. Stat. Check. {e.Status} {(e.IsAsync ? "Async" : "Serial")}");
            camera.AcquisitionAborted += (sender, e)
                => host?.OnEventReceived(sender, $"Acq. Aborted      {e.Status} {(e.IsAsync ? "Async" : "Serial")}");
            camera.AcquisitionErrorReturned += (sender, e)
                => host?.OnEventReceived(sender, $"Acq. Err. Ret.    {e.Status} {(e.IsAsync ? "Async" : "Serial")}");
            camera.NewImageReceived += (sender, e)
                => host?.OnEventReceived(sender, $"New image:  ({e.First}, {e.Last})");

            host?.OnEventReceived(camera, "Camera was created remotely");
        }
        [OperationBehavior]
        public void RemoveCamera(int camIndex)
        {
            try
            {
                foreach (var settsKey in
                    from item
                    in settings
                    where item.Value.CameraIndex == camIndex
                    select item.Key)
                    RemoveSettings(settsKey);

                foreach (var taskKey in
                    from item
                    in activeTasks
                    where item.Value.CameraIndex == camIndex
                    select item.Key)
                    RemoveTask(taskKey);

                var removedCamera = GetCameraSafe(sessionID, camIndex);
                host?.OnEventReceived(removedCamera, "Camera was disposed remotely.");
                activeCameras.TryRemove(camIndex, out _);

                removedCamera.Dispose();
            }
            catch (Exception e)
            {
                throw new FaultException<ServiceException>(
                    new ServiceException()
                    {
                        Message = "An exception was thrown while disposing Camera resource",
                        Details = e.Message
                    },
                    ServiceException.GeneralServiceErrorReason);
            }
        }
        [OperationBehavior]
        public string CreateSettings(int camIndex)
        {
            CameraBase cam = GetCameraSafe(SessionID, camIndex);
            SettingsBase setts = cam.GetAcquisitionSettingsTemplate();

            string settingsID = Guid.NewGuid().ToString("N");
            int counter = 0;
            while ((counter <= MaxTryAddAttempts) && !settings.TryAdd(settingsID, setts))
                counter++;

            if (counter >= MaxTryAddAttempts)
                throw new FaultException<ServiceException>(
                    new ServiceException()
                    {
                        Message = "Failed to create unique ID for the settings instance.",
                        Details = $"After {MaxTryAddAttempts} sessionID was not generated",
                        MethodName = nameof(settings.TryAdd)
                    },
                    ServiceException.GeneralServiceErrorReason);

            host?.OnEventReceived("Host", $"AcqSettings with ID {settingsID} created.");

            return settingsID;
        }
        [OperationBehavior]
        public void RemoveSettings(string settingsID)
        {
            settings.TryRemove(settingsID, out SettingsBase setts);
            setts?.Dispose();

            host?.OnEventReceived("Host", $"AcqSettings with ID {settingsID} removed.");
        }
        [OperationBehavior]
        public string CreateAcquisitionTask(int camIndex, int delay)
        {
            var cam = GetCameraSafe(sessionID, camIndex);
            string taskID = Guid.NewGuid().ToString("N");

            int counter = 0;
            var source = new CancellationTokenSource();
            var task = cam.StartAcquistionAsync(source, delay);

            while (counter <= MaxTryAddAttempts && !activeTasks.TryAdd(taskID, (Task: task, Token: source, CameraIndex: camIndex)))
                taskID = Guid.NewGuid().ToString("N");

            if (counter >= MaxTryAddAttempts)
            {
                source.Cancel();
                task.Wait();
                task.Dispose();
                source.Dispose();

                throw new Exception();
            }

            host?.OnEventReceived("Host", $"AcqTask with ID {taskID} created.");

            return taskID;
        }
        [OperationBehavior]
        public void RemoveTask(string taskID)
        {
            if (activeTasks.TryRemove(taskID, out (Task Task, CancellationTokenSource Token, int CamIndex) taskInfo))
            {
                taskInfo.Token.Cancel();
                taskInfo.Task.Wait();

                taskInfo.Task.Dispose();
                taskInfo.Token.Dispose();
            }

            host?.OnEventReceived("Host", $"AcqTask with ID {taskID} removed.");
        }


        [OperationBehavior]
        public int[] GetCamerasInUse()
            => activeCameras.Keys.ToArray();
        [OperationBehavior]
        public string GetCameraModel(int camIndex)
            => GetCameraSafe(sessionID, camIndex).CameraModel;
        [OperationBehavior]
        public bool GetIsActive(int camIndex)
            => GetCameraSafe(sessionID, camIndex).IsActive;
        [OperationBehavior]
        public string GetSerialNumber(int camIndex)
            => GetCameraSafe(sessionID, camIndex).SerialNumber;
        [OperationBehavior]
        public CameraProperties GetProperties(int camIndex)
            => GetCameraSafe(sessionID, camIndex).Properties;
        [OperationBehavior]
        public bool GetIsInitialized(int camIndex)
            => GetCameraSafe(sessionID, camIndex).IsInitialized;
        [OperationBehavior]
        public FanMode GetFanMode(int camIndex)
            => GetCameraSafe(sessionID, camIndex).FanMode;
        [OperationBehavior]
        public Switch GetCoolerMode(int camIndex)
            => GetCameraSafe(sessionID, camIndex).CoolerMode;
        [OperationBehavior]
        public DeviceCapabilities GetCapabilities(int camIndex)
           => GetCameraSafe(sessionID, camIndex).Capabilities;
        [OperationBehavior]
        public bool GetIsAcquiring(int camIndex)
            => GetCameraSafe(sessionID, camIndex).IsAcquiring;
        [OperationBehavior]
        public bool GetIsAsyncAcquisition(int camIndex)
            => GetCameraSafe(sessionID, camIndex).IsAsyncAcquisition;
        [OperationBehavior]
        public (
           ShutterMode Internal,
           ShutterMode? External,
           TTLShutterSignal Type,
           int OpenTime,
           int CloseTime) GetShutter(int camIndex)
            => GetCameraSafe(sessionID, camIndex).Shutter;
        [OperationBehavior]
        public (Version EPROM, Version COFFile, Version Driver, Version Dll) GetSoftware(int camIndex)
            => GetCameraSafe(sessionID, camIndex).Software;
        [OperationBehavior]
        public (Version PCB, Version Decode, Version CameraFirmware) GetHardware(int camIndex)
            => GetCameraSafe(sessionID, camIndex).Hardware;

        [OperationBehavior]
        public byte[] PullNewImage(int camIndex)
        {
            if (GetCameraSafe(sessionID, camIndex).AcquiredImages.TryDequeue(out ImageDisplayLib.Image im))
                return im.GetBytes();
            else
                throw new Exception();

            ////GetCameraSafe(sessionID, camIndex).AcquiredImages.TryDequeue(out ImageDisplayLib.Image im);
            //byte[] arr = new byte[10];
            //(new Random()).NextBytes(arr);
            //return arr; // im.GetBytes();
        }




        [OperationBehavior]
        public CameraStatus CallGetStatus(int camIndex)
           => GetCameraSafe(sessionID, camIndex).GetStatus();
        [OperationBehavior]
        public (TemperatureStatus Status, float Temperature) CallGetCurrentTemperature(int camIndex)
            => GetCameraSafe(sessionID, camIndex).GetCurrentTemperature();
        [OperationBehavior]
        public void CallSetActive(int camIndex)
            => GetCameraSafe(sessionID, camIndex).SetActive();
        [OperationBehavior]
        public void CallFanControl(int camIndex, FanMode mode)
            => GetCameraSafe(sessionID, camIndex).FanControl(mode);
        [OperationBehavior]
        public void CallCoolerControl(int camIndex, Switch mode)
            => GetCameraSafe(sessionID, camIndex).CoolerControl(mode);
        [OperationBehavior]
        public void CallSetTemperature(int camIndex, int temperature)
            => GetCameraSafe(sessionID, camIndex).SetTemperature(temperature);
        [OperationBehavior]
        public void CallShutterControl(
            int camIndex,
            int clTime,
            int opTime,
            ShutterMode inter,
            ShutterMode exter = ShutterMode.FullyAuto,
            TTLShutterSignal type = TTLShutterSignal.Low)
            => GetCameraSafe(sessionID, camIndex).ShutterControl(
                clTime,
                opTime,
                inter,
                exter,
                type);
        [OperationBehavior]
        public void CallTemperatureMonitor(int camIndex, Switch mode, int timeout)
            => GetCameraSafe(sessionID, camIndex).TemperatureMonitor(mode, timeout);
        [OperationBehavior]
        public void CallStartAcquisition(int camIndex)
            => GetCameraSafe(sessionID, camIndex).StartAcquisition();
        [OperationBehavior]
        public void CallAbortAcquisition(int camIndex)
            => GetCameraSafe(sessionID, camIndex).AbortAcquisition();


        [OperationBehavior]
        public (int Index, float Speed)[] GetAvailableHSSpeeds(
            string settingsID,
            int ADConverterIndex,
            int amplifier)
        => GetSettingsSafe(settingsID).GetAvailableHSSpeeds(ADConverterIndex, amplifier).ToArray();

        [OperationBehavior]
        public (int Index, string Name)[] GetAvailablePreAmpGain(
            string settingsID,
            int ADConverterIndex,
            int amplifier,
            int HSSpeed)
        => GetSettingsSafe(settingsID).GetAvailablePreAmpGain(
            ADConverterIndex, amplifier, HSSpeed).ToArray();

        [OperationBehavior]
        public (bool IsSupported, float Speed) CallIsHSSpeedSupported(
            string settingsID, 
            int ADConverter,
            int amplifier,
            int speedIndex)
            => (
            IsSupported: GetSettingsSafe(settingsID)
                .IsHSSpeedSupported(speedIndex, ADConverter, amplifier, out float speed),
            Speed: speed);

        /// <summary>
        /// Applies <see cref="RemoteSettings"/> to local instance of <see cref="AcquisitionSettings"/>.
        /// Remote settings are packed into <see cref="byte"/> array and transmitted over network.
        /// </summary>
        /// <param name="settingsID">Unique settings identifier.</param>
        /// <param name="data">Packed into <see cref="byte"/> array <see cref="RemoteSettings"/>.</param>
        /// <returns>
        /// A complex object <see cref="ValueTuple{T1, T2}"/> containing results of settings application (an array) 
        /// and tuple of timing/frame information that is calculated for this specific settings.
        /// </returns>
        [OperationBehavior]
        public
        // Output of AcquisitionSettings.ApplySettings()
        ((string Option, bool Success, uint ReturnCode)[] Result,
        // Out-result of the same method
         (float ExposureTime, float AccumulationCycleTime, float KineticCycleTime, int BufferSize) Timing)
         CallApplySettings(string settingsID, byte[] data)
        {
            // Safely retrieves local copy of AcquisitionSettings.
            var setts = GetSettingsSafe(settingsID);

            // Creates MemoryStream from byte array and uses it for deserialization.
            using (var memStr = new System.IO.MemoryStream(data))
                setts.Deserialize(memStr);

            // Applies settings on a local copy of settings.
            var result = setts.ApplySettings(out (float, float, float, int) timing);

            // Returns results.
            return (Result: result.ToArray(), Timing: timing);
        }

        [OperationBehavior]
        public bool IsTaskFinished(string taskID)
            => GetTaskSafe(taskID).Task.IsCompleted;

        [OperationBehavior]
        public void RequestCancellation(string taskID)
        {
            if (ActiveTasks.TryGetValue(taskID, out (Task Task, CancellationTokenSource Token, int CameraIndex) taskInfo))
            {
                taskInfo.Token.Cancel();
                RemoveTask(taskID);
            }
            else throw new Exception();
        }

        private CameraBase GetCameraSafe(string session, int camIndex)
        {
            if (ActiveCameras.TryGetValue(
                camIndex,
                out (string SessionID, CameraBase Camera) camInfo))

                if (camInfo.SessionID == SessionID)
                    return camInfo.Camera;
                else throw ServiceException.IllegalSessionFaultException();

            else throw new FaultException<ServiceException>(
                new ServiceException()
                {
                    Message = "Specified camera cannot be found among active devices.",
                    Details = "Camera is not found in pool of active cameras. It might have been already disposed.",
                    MethodName = nameof(ActiveCameras.TryGetValue)
                },
                ServiceException.GeneralServiceErrorReason);
        }
        private SettingsBase GetSettingsSafe(string settingsID)
        {
            if (Settings.TryGetValue(settingsID, out SettingsBase sets))
                return sets;
            else throw new Exception();
        }
        private (Task Task, CancellationTokenSource Token, int CameraIndex) GetTaskSafe(string taskID)
        {
            if (ActiveTasks.TryGetValue(taskID, out (Task Task, CancellationTokenSource Token, int CameraIndex) taskInfo))
                return taskInfo;
            else
                throw new Exception();
        }
    }
}
