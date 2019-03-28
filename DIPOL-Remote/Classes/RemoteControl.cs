﻿//    This file is part of Dipol-3 Camera Manager.

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

//#define NO_ACTUAL_CAMERA

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Threading.Tasks;
using System.Threading;

using System.ServiceModel;
using System.Text;
using DIPOL_Remote.Faults;

using ANDOR_CS.Classes;
using ANDOR_CS.Exceptions;
using ANDOR_CS.DataStructures;
using ANDOR_CS.Enums;
using DIPOL_Remote.Interfaces;
using SettingsManager;
using Newtonsoft.Json;

using CameraDictionary = System.Collections.Concurrent.ConcurrentDictionary<int, ANDOR_CS.Classes.CameraBase>;
// ReSharper disable InheritdocConsiderUsage

namespace DIPOL_Remote.Classes
{
    /// <inheritdoc />
    /// <summary>
    /// Implementation of <see cref="T:DIPOL_Remote.Interfaces.IRemoteControl" /> service interface.
    /// This class should not be utilized directly.
    /// Instances are executed on server (service) side.
    /// </summary>
    [ServiceBehavior(
        ConcurrencyMode = ConcurrencyMode.Multiple, 
        InstanceContextMode = InstanceContextMode.Single,
        //InstanceContextMode = InstanceContextMode.PerSession,
        AutomaticSessionShutdown = true,
        //UseSynchronizationContext = true,
        IncludeExceptionDetailInFaults = true)]
    internal sealed class RemoteControl : IRemoteControl, IDisposable
    {
        /// <summary>
        /// Defines max number of attempts to create unique identifier.
        /// </summary>
        // TODO : Move to settings
        private const int MaxTryAddAttempts = 30;

        private OperationContext _currentContext;
        /// <summary>
        /// A reference to Host instance
        /// </summary>
        private DipolHost _host;

        private readonly CameraDictionary _cameras = new CameraDictionary();
        //private readonly CameraTaskPool _initalizationPool = new CameraTaskPool();

        /// <summary>
        /// Thread-safe collection of all active instances of AcquisitionSettings.
        /// </summary>
        private readonly ConcurrentDictionary<string,  SettingsBase> settings
            = new ConcurrentDictionary<string,  SettingsBase>();

        private readonly ConcurrentDictionary<string, (Task Task, CancellationTokenSource Token, int CameraIndex)> activeTasks
            = new ConcurrentDictionary<string, (Task Task, CancellationTokenSource Token, int CameraIndex)>();

        ///// <summary>
        ///// Thread-safe collection of active remote cameras.
        ///// </summary>
        //private static readonly ConcurrentDictionary<int, (string SessionID, CameraBase Camera)> activeCameras
        //    = new ConcurrentDictionary<int, (string SessionID, CameraBase Camera)>();

        /// <summary>
        /// Unique ID of current session
        /// </summary>
        public string SessionID { [OperationBehavior]
            get; private set;
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
        //public static IReadOnlyDictionary<string, RemoteControl> ActiveConnections
        //    => serviceInstances as IReadOnlyDictionary<string, RemoteControl>;

        /// <summary>
        /// Interface to collectio of all active cameras of all sessions
        /// </summary>
        //public static IReadOnlyDictionary<int, (string SessionID, CameraBase Camera)> ActiveCameras
        //    => activeCameras as IReadOnlyDictionary<int, (string SessionID, CameraBase Camera)>;
       
        /// <summary>
        /// Default constructor
        /// </summary>
        private RemoteControl()
        {
         
        }

        private void SubscribeToCameraEvents(CameraBase camera)
        {
            // Remotely fires event, informing that some property has changed
            camera.PropertyChanged += (sender, e)
                => GetContext()
                ?.GetCallbackChannel<IRemoteCallback>()
                .NotifyRemotePropertyChanged(
                    camera.CameraIndex,
                    SessionID,
                    e.PropertyName);

            // Remotely fires event, informing that temperature status was checked.
            camera.TemperatureStatusChecked += (sender, e)
                => GetContext()
                ?.GetCallbackChannel<IRemoteCallback>()
                .NotifyRemoteTemperatureStatusChecked(
                    camera.CameraIndex,
                    SessionID,
                    e);
            // Remotely fires event, informing that acquisition was started.
            camera.AcquisitionStarted += (snder, e)
                => GetContext()
                ?.GetCallbackChannel<IRemoteCallback>()
                .NotifyRemoteAcquisitionEventHappened(
                    camera.CameraIndex,
                    SessionID,
                    Enums.AcquisitionEventType.Started,
                    e);
            // Remotely fires event, informing that acquisition was finished.
            camera.AcquisitionFinished += (snder, e)
                => GetContext()
                ?.GetCallbackChannel<IRemoteCallback>()
                .NotifyRemoteAcquisitionEventHappened(
                    camera.CameraIndex,
                    SessionID,
                    Enums.AcquisitionEventType.Finished,
                    e);
            // Remotely fires event, informing that acquisition progress was checked.
            camera.AcquisitionStatusChecked += (snder, e)
                => GetContext()
                ?.GetCallbackChannel<IRemoteCallback>()
                .NotifyRemoteAcquisitionEventHappened(
                    camera.CameraIndex,
                    SessionID,
                    Enums.AcquisitionEventType.StatusChecked,
                    e);
            // Remotely fires event, informing that acquisition was aborted.
            camera.AcquisitionAborted += (snder, e)
                => GetContext()
                ?.GetCallbackChannel<IRemoteCallback>()
                .NotifyRemoteAcquisitionEventHappened(
                    camera.CameraIndex,
                    SessionID,
                    Enums.AcquisitionEventType.Aborted,
                    e);
            // Remotely fires event, informing that an error happened during acquisition process.
            camera.AcquisitionErrorReturned += (snder, e)
                => GetContext()
                ?.GetCallbackChannel<IRemoteCallback>()
                .NotifyRemoteAcquisitionEventHappened(
                    camera.CameraIndex,
                    SessionID,
                    Enums.AcquisitionEventType.ErrorReturned,
                    e);
            // Remotely fires event, informing that new image was acquired
            camera.NewImageReceived += (sndr, e)
                => GetContext()
                ?.GetCallbackChannel<IRemoteCallback>()
                .NotifyRemoteNewImageReceivedEventHappened(
                    camera.CameraIndex,
                    SessionID,
                    e);

            camera.TemperatureStatusChecked += (sender, e)
                => _host?.OnEventReceived(sender, $"{e.Status} {e.Temperature}");

            camera.PropertyChanged += (sender, e)
                => _host?.OnEventReceived(sender, e.PropertyName);
        }

        private async Task CreateCameraAsync(int camIndex, params object[] @params)
        {
            CameraBase camera;
            try
            {
                // Tries to create new remote camera
#if DEBUG
                camera = Camera.GetNumberOfCameras() <= 0
                    ? await DebugCamera.CreateAsync(camIndex, @params)
                    : await Camera.CreateAsync(camIndex, @params) as CameraBase;

#else
                camera = await Camera.CreateAsync(camIndex, @params);
#endif
            }
            // Andor-related exception
            catch (AndorSdkException andorEx)
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

            if (!_cameras.TryAdd(camIndex, camera))
            {
                camera?.Dispose();
                throw new FaultException<ServiceException>(
                    new ServiceException()
                    {
                        Message = "Failed to create new remote camera.",
                        MethodName = nameof(Camera)
                    },
                    ServiceException.CameraCommunicationReason);
            }

            SubscribeToCameraEvents(camera);
           
            // TODO: Update Logging
            //camera.AcquisitionStarted += (sender, e)
            //    => host?.OnEventReceived(sender, $"Acq. Started      {e.Status} {(e.IsAsync ? "Async" : "Serial")}");
            //camera.AcquisitionFinished += (sender, e)
            //    => host?.OnEventReceived(sender, $"Acq. Finished     {e.Status} {(e.IsAsync ? "Async" : "Serial")}");
            //camera.AcquisitionStatusChecked += (sender, e)
            //    => host?.OnEventReceived(sender, $"Acq. Stat. Check. {e.Status} {(e.IsAsync ? "Async" : "Serial")}");
            //camera.AcquisitionAborted += (sender, e)
            //    => host?.OnEventReceived(sender, $"Acq. Aborted      {e.Status} {(e.IsAsync ? "Async" : "Serial")}");
            //camera.AcquisitionErrorReturned += (sender, e)
            //    => host?.OnEventReceived(sender, $"Acq. Err. Ret.    {e.Status} {(e.IsAsync ? "Async" : "Serial")}");
            //camera.NewImageReceived += (sender, e)
            //    => host?.OnEventReceived(sender, $"New image:  ({e.First}, {e.Last})");

            _host?.OnEventReceived(camera, "Camera was created remotely");

        }

        /// <summary>
        /// Entry point of any connection.
        /// </summary>
        [OperationBehavior]
        public void Connect()
        {
            _host = OperationContext.Current.Host as DipolHost ??
                    throw new InvalidOperationException("Unsupported host type.");
            _currentContext = OperationContext.Current;

            var uriHash = _host.BaseAddresses.Select(x => x.ToString().GetHashCode()).DefaultIfEmpty(0).FirstOrDefault();

            SessionID = BitConverter.GetBytes(uriHash).Concat(BitConverter.GetBytes(DateTimeOffset.UtcNow.UtcTicks))
                                    .Aggregate(new StringBuilder(2 * (sizeof(long) + sizeof(int))), 
                                        (old, @new) => old.Append(@new.ToString("X2"))).ToString();
                            

            _host?.OnEventReceived("Host", $"Session {SessionID} established.");

        }
        /// <summary>
        /// Exit point for any connection. Frees resources.
        /// </summary>
        [OperationBehavior]
        public void Disconnect()
        {
           Dispose();
           _host?.OnEventReceived("Host", $"Session {SessionID} closed.");
        }
 

        /// <summary>
        /// Implementation of <see cref="IDisposable"/> interface. Frees resources.
        /// </summary>
        public void Dispose()
        {
            try
            {
                //TODO : Dispose of all cameras
                // Select all cameras that are created from this session
                //foreach (var key in
                //    from item
                //    in activeCameras
                //    where item.Value.SessionID == SessionID
                //    select item.Key)
                //    // Remove these cameras from te collection
                //    if (activeCameras.TryRemove(key, out (string SessionID, CameraBase Camera) camInfo))
                //        // If successful, dispose camera instance
                //        camInfo.Camera.Dispose();

                foreach (var key in settings.Keys)
                    RemoveSettings(key);

                foreach (var key in activeTasks.Keys)
                    RemoveTask(key);

            }
            catch (Exception e)
            {
                throw new FaultException<ServiceException>(
                    new ServiceException()
                    {
                        Message = "An exception was thrown while disposing session resources.",
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
#if DEBUG
                var nCams = 0;
                try
                {
                    nCams = Camera.GetNumberOfCameras();
                }
                catch (Exception)
                {
                }
                return nCams == 0 ? 3 : nCams;
#else
                // Tries to retrieve the number of available cameras
                return Camera.GetNumberOfCameras();
#endif

            }
            // If method fails and Andor-related exception is thrown
            catch (AndorSdkException andorEx)
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
            CreateCameraAsync(camIndex).ConfigureAwait(false).GetAwaiter().GetResult();
        }
        [OperationBehavior]
        public void RemoveCamera(int camIndex)
        {
            try
            {
                foreach (var settsKey in
                    from item
                    in settings
                    where item.Value.Camera.CameraIndex == camIndex
                    select item.Key)
                    RemoveSettings(settsKey);

                foreach (var taskKey in
                    from item
                    in activeTasks
                    where item.Value.CameraIndex == camIndex
                    select item.Key)
                    RemoveTask(taskKey);

                //var removedCamera = GetCameraSafe(SessionID, camIndex);
                if (_cameras.TryRemove(camIndex, out var removedCamera))
                {
                    _host?.OnEventReceived(removedCamera, "Camera was disposed remotely.");
                    removedCamera.Dispose();
                }
                else throw new InvalidOperationException("Camera handle is unavailable.");

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
            CameraBase cam = GetCameraSafe(camIndex);
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

            _host?.OnEventReceived("Host", $"AcqSettings with ID {settingsID} created.");

            return settingsID;
        }
        [OperationBehavior]
        public void RemoveSettings(string settingsID)
        {
            settings.TryRemove(settingsID, out SettingsBase setts);
            setts?.Dispose();

            _host?.OnEventReceived("Host", $"AcqSettings with ID {settingsID} removed.");
        }
        [OperationBehavior]
        public string CreateAcquisitionTask(int camIndex, int delay)
        {
            throw new NotImplementedException();
            //var cam = GetCameraSafe(sessionID, camIndex);
            //string taskID = Guid.NewGuid().ToString("N");

            //int counter = 0;
            //var source = new CancellationTokenSource();
            //var task = cam.StartAcquisitionAsync(source, delay);

            //while (counter <= MaxTryAddAttempts && !activeTasks.TryAdd(taskID, (Task: task, Token: source, CameraIndex: camIndex)))
            //    taskID = Guid.NewGuid().ToString("N");

            //if (counter >= MaxTryAddAttempts)
            //{
            //    source.Cancel();
            //    task.Wait();
            //    task.Dispose();
            //    source.Dispose();

            //    throw new Exception();
            //}

            //host?.OnEventReceived("Host", $"AcqTask with ID {taskID} created.");

            //return taskID;
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

            _host?.OnEventReceived("Host", $"AcqTask with ID {taskID} removed.");
        }


        [OperationBehavior]
        public int[] GetCamerasInUse()
            => _cameras.Keys.ToArray();
        [OperationBehavior]
        public string GetCameraModel(int camIndex)
            => GetCameraSafe(camIndex).CameraModel;
        [OperationBehavior]
        public bool GetIsActive(int camIndex)
            => GetCameraSafe(camIndex).IsActive;
        [OperationBehavior]
        public string GetSerialNumber(int camIndex)
            => GetCameraSafe(camIndex).SerialNumber;
        [OperationBehavior]
        public CameraProperties GetProperties(int camIndex)
            => GetCameraSafe(camIndex).Properties;
        [OperationBehavior]
        public bool GetIsInitialized(int camIndex)
            => GetCameraSafe(camIndex).IsInitialized;
        [OperationBehavior]
        public FanMode GetFanMode(int camIndex)
            => GetCameraSafe(camIndex).FanMode;
        [OperationBehavior]
        public Switch GetCoolerMode(int camIndex)
            => GetCameraSafe(camIndex).CoolerMode;
        [OperationBehavior]
        public DeviceCapabilities GetCapabilities(int camIndex)
           => GetCameraSafe(camIndex).Capabilities;
        [OperationBehavior]
        public bool GetIsAcquiring(int camIndex)
            => GetCameraSafe(camIndex).IsAcquiring;
        //[OperationBehavior]
        //public bool GetIsAsyncAcquisition(int camIndex)
        //    => GetCameraSafe(sessionID, camIndex).IsAsyncAcquisition;
        [OperationBehavior]
        public (
           ShutterMode Internal,
           ShutterMode? External,
           TtlShutterSignal Type,
           int OpenTime,
           int CloseTime) GetShutter(int camIndex)
            => GetCameraSafe(camIndex).Shutter;
        [OperationBehavior]
        public bool GetIsTemperatureMonitored(int camIndex)
            => GetCameraSafe(camIndex).IsTemperatureMonitored;
        [OperationBehavior]
        public (Version EPROM, Version COFFile, Version Driver, Version Dll) GetSoftware(int camIndex)
            => GetCameraSafe(camIndex).Software;
        [OperationBehavior]
        public (Version PCB, Version Decode, Version CameraFirmware) GetHardware(int camIndex)
            => GetCameraSafe(camIndex).Hardware;

        [OperationBehavior]
        public CameraStatus CallGetStatus(int camIndex)
           => GetCameraSafe(camIndex).GetStatus();
        [OperationBehavior]
        public (TemperatureStatus Status, float Temperature) CallGetCurrentTemperature(int camIndex)
            => GetCameraSafe(camIndex).GetCurrentTemperature();

        [OperationBehavior]
        public void CallFanControl(int camIndex, FanMode mode)
            => GetCameraSafe(camIndex).FanControl(mode);
        [OperationBehavior]
        public void CallCoolerControl(int camIndex, Switch mode)
            => GetCameraSafe(camIndex).CoolerControl(mode);
        [OperationBehavior]
        public void CallSetTemperature(int camIndex, int temperature)
            => GetCameraSafe(camIndex).SetTemperature(temperature);
        // TODO: Take a look at the parameter order
        [OperationBehavior]
        public void CallShutterControl(
            int camIndex,
            int clTime,
            int opTime,
            ShutterMode inter,
            ShutterMode @extern = ShutterMode.FullyAuto,
            TtlShutterSignal type = TtlShutterSignal.Low)
            => GetCameraSafe(camIndex).ShutterControl(
                inter,
                @extern,
                clTime,
                opTime,
                type);
        [OperationBehavior]
        public void CallTemperatureMonitor(int camIndex, Switch mode, int timeout)
            => GetCameraSafe(camIndex).TemperatureMonitor(mode, timeout);

        [OperationBehavior]
        public void CallStartAcquisition(int camIndex)
            => throw new NotImplementedException();

        [OperationBehavior]
        public void CallAbortAcquisition(int camIndex)
            => throw new NotImplementedException();

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
            throw new NotImplementedException();
            //// Safely retrieves local copy of AcquisitionSettings.
            //var setts = GetSettingsSafe(settingsID);

            //// Creates MemoryStream from byte array and uses it for deserialization.
            //using (var memStr = new System.IO.MemoryStream(data))
            //    setts.Deserialize(memStr);

            //// Applies settings on a local copy of settings.
            //var result = setts.ApplySettings(out (float, float, float, int) timing);

            //// Returns results.
            //return (Result: result.ToArray(), Timing: timing);
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

        private CameraBase GetCameraSafe(int camIndex)
        {
            if (_cameras.TryGetValue(
                camIndex,
                out var cam))
                return cam;

            throw new FaultException<ServiceException>(
                new ServiceException()
                {
                    Message = "Specified camera cannot be found among active devices.",
                    Details = "Camera is not found in pool of active cameras. It might have been already disposed.",
                    MethodName = nameof(GetCameraSafe)
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

        private OperationContext GetContext()
        {
            return _currentContext?.Channel.State == CommunicationState.Opened ? _currentContext : null;
        }

        public void RequestCreateCamera(int camIndex)
        {
            Task.Run(() =>
            {
                var result = true;
                try
                {
                    CreateCamera(camIndex);
                }
                catch
                {
                    result = false;
                }


                var isAccepted = GetContext()
                    ?.GetCallbackChannel<IRemoteCallback>()
                    ?.NotifyCameraCreatedAsynchronously(camIndex, SessionID, result);

                if (!isAccepted ?? true)
                    RemoveCamera(camIndex);
            });

        }

        //static RemoteControl()
        //{
        //    LoadSettings();
        //}

        //private static void LoadSettings(in string path = "dipolconfig.json")
        //{
        //    JsonSettings settings = new JsonSettings();
        //    if (File.Exists(Path.Combine(Environment.CurrentDirectory, path)))
        //        using (var str = new StreamReader(Path.Combine(Environment.CurrentDirectory, path)))
        //        {
        //            try
        //            {
        //                settings = new JsonSettings(str.ReadToEnd());
        //            }
        //            catch (JsonReaderException jre)
        //            {
        //                settings = new JsonSettings();
        //            }
        //            catch
        //            {
        //                //TODO: Add logging system to this level and report other IO errors.
        //            }
        //        }

        //    _config = settings;
        //}

        // Async pattern
        [OperationBehavior]
        public IAsyncResult BeginCreateCameraAsync(int camIndex, AsyncCallback callback, object state)
        {
            //return Task.Factory.StartNew(async _ => await CreateCameraAsync(camIndex), state);
            return new AsyncCameraResult(CreateCameraAsync(camIndex), state);
        }

        //[OperationBehavior]
        public bool EndCreateCameraAsync(IAsyncResult result)
        {
            if (result is AsyncCameraResult camResult)
            {
                if(!camResult.IsCompleted)
                    camResult.Task.GetAwaiter().GetResult();
                return camResult.Task.Status == TaskStatus.RanToCompletion;
            }
            return false;
        }

    }
}
