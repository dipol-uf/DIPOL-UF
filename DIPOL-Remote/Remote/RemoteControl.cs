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

//#define NO_ACTUAL_CAMERA

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ANDOR_CS.Classes;
using ANDOR_CS.DataStructures;
using ANDOR_CS.Enums;
using ANDOR_CS.Exceptions;
using DIPOL_Remote.Callback;
using DIPOL_Remote.Faults;
using CameraDictionary = System.Collections.Concurrent.ConcurrentDictionary<int, ANDOR_CS.Classes.CameraBase>;
// ReSharper disable InheritdocConsiderUsage

namespace DIPOL_Remote.Remote
{
    /// <summary>
    /// Implementation of <see cref="T:DIPOL_Remote.Interfaces.IRemoteControl" /> service interface.
    /// This class should not be utilized directly.
    /// Instances are executed on server (service) side.
    /// </summary>
    [ServiceBehavior(
        ConcurrencyMode = ConcurrencyMode.Multiple,
        InstanceContextMode = InstanceContextMode.PerSession,
        AutomaticSessionShutdown = true,
        UseSynchronizationContext = true,
        IncludeExceptionDetailInFaults = true)]
    internal sealed class RemoteControl : IRemoteControl, IDisposable
    {
        /// <summary>
        /// Defines max number of attempts to create unique identifier.
        /// </summary>
        // TODO : Move to settings
        private const int MaxTryAddAttempts = 30;

        /// <summary>
        /// A reference to Host instance
        /// </summary>
        private DipolHost _host;

        private readonly CameraDictionary _cameras = new CameraDictionary();

        /// <summary>
        /// Thread-safe collection of all active instances of AcquisitionSettings.
        /// </summary>
        private readonly ConcurrentDictionary<string,  SettingsBase> _settings
            = new ConcurrentDictionary<string,  SettingsBase>();

        private readonly ConcurrentDictionary<string, (Task Task, CancellationTokenSource Token, int CameraIndex)> _activeTasks
            = new ConcurrentDictionary<string, (Task Task, CancellationTokenSource Token, int CameraIndex)>();

        private OperationContext Context { get; set; }

        private IRemoteCallback Callback => Context?.GetCallbackChannel<IRemoteCallback>()
            ?? throw new NullReferenceException("Either service context or callback channel are null");

        /// <summary>
        /// Unique ID of current session
        /// </summary>
        public string SessionID {
            [OperationBehavior]
            get;
            private set;
        }

        /// <summary>
        /// Interface to collection of all active <see cref="AcquisitionSettings"/> instances.
        /// </summary>
        public IReadOnlyDictionary<string,  SettingsBase> Settings
           => _settings;

        public IReadOnlyDictionary<string, (Task Task, CancellationTokenSource Token, int CameraIndex)> ActiveTasks
            => _activeTasks;

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
                => Callback
                .NotifyRemotePropertyChanged(
                    camera.CameraIndex,
                    e.PropertyName);

            // Remotely fires event, informing that temperature status was checked.
            camera.TemperatureStatusChecked += (sender, e)
                => Callback
                .NotifyRemoteTemperatureStatusChecked(
                    camera.CameraIndex,
                    e);
            // Remotely fires event, informing that acquisition was started.
            camera.AcquisitionStarted += (snder, e)
                => Callback
                .NotifyRemoteAcquisitionEventHappened(
                    camera.CameraIndex,
                    Enums.AcquisitionEventType.Started,
                    e);
            // Remotely fires event, informing that acquisition was finished.
            camera.AcquisitionFinished += (snder, e)
                => Callback
                .NotifyRemoteAcquisitionEventHappened(
                    camera.CameraIndex,
                    Enums.AcquisitionEventType.Finished,
                    e);
            // Remotely fires event, informing that acquisition progress was checked.
            camera.AcquisitionStatusChecked += (snder, e)
                => Callback
                .NotifyRemoteAcquisitionEventHappened(
                    camera.CameraIndex,
                    Enums.AcquisitionEventType.StatusChecked,
                    e);
            // Remotely fires event, informing that acquisition was aborted.
            camera.AcquisitionAborted += (snder, e)
                => Callback
                .NotifyRemoteAcquisitionEventHappened(
                    camera.CameraIndex,
                    Enums.AcquisitionEventType.Aborted,
                    e);
            // Remotely fires event, informing that an error happened during acquisition process.
            camera.AcquisitionErrorReturned += (snder, e)
                => Callback
                .NotifyRemoteAcquisitionEventHappened(
                    camera.CameraIndex,
                    Enums.AcquisitionEventType.ErrorReturned,
                    e);
            // Remotely fires event, informing that new image was acquired
            camera.NewImageReceived += (sndr, e)
                => Callback
                .NotifyRemoteNewImageReceivedEventHappened(
                    camera.CameraIndex,
                    e);

            camera.TemperatureStatusChecked += (sender, e)
                => _host?.OnEventReceived(sender, $"{e.Status} {e.Temperature}");

            camera.PropertyChanged += (sender, e)
                => _host?.OnEventReceived(sender, e.PropertyName);


            camera.AcquisitionStarted += (sender, e)
                => _host?.OnEventReceived(sender, $"Acq. Started      {e.Status}");
            camera.AcquisitionFinished += (sender, e)
                => _host?.OnEventReceived(sender, $"Acq. Finished     {e.Status}");
            camera.AcquisitionStatusChecked += (sender, e)
                => _host?.OnEventReceived(sender, $"Acq. Stat. Check. {e.Status}");
            camera.AcquisitionAborted += (sender, e)
                => _host?.OnEventReceived(sender, $"Acq. Aborted      {e.Status}");
            camera.AcquisitionErrorReturned += (sender, e)
                => _host?.OnEventReceived(sender, $"Acq. Err. Ret.    {e.Status}");
            camera.NewImageReceived += (sender, e)
                => _host?.OnEventReceived(sender, $"New image [{e.Index}] at {e.EventTime:HH:mm:ss.fff} (sender time)");
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
                throw AndorSdkFault.WrapFault(andorEx, nameof(Camera));
            }
            // Other possible exceptions
            catch (Exception ex)
            {
                throw new FaultException<ServiceFault>(
                    new ServiceFault()
                    {
                        Message = "Failed to create new remote camera.",
                        Details = ex.Message,
                        MethodName = nameof(Camera)
                    },
                    ServiceFault.CameraCommunicationReason);
            }

            if (!_cameras.TryAdd(camIndex, camera))
            {
                camera?.Dispose();
                throw new FaultException<ServiceFault>(
                    new ServiceFault()
                    {
                        Message = "Failed to create new remote camera.",
                        MethodName = nameof(Camera)
                    },
                    ServiceFault.CameraCommunicationReason);
            }

            SubscribeToCameraEvents(camera);

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
            Context = OperationContext.Current;

            var uriHash = _host.BaseAddresses.Select(x => x.ToString().GetHashCode()).DefaultIfEmpty(0).FirstOrDefault();

            SessionID = BitConverter.GetBytes(uriHash).Concat(BitConverter.GetBytes(DateTimeOffset.UtcNow.UtcTicks))
                                    .Aggregate(new StringBuilder(2 * (sizeof(long) + sizeof(int))), 
                                        (old, @new) => old.Append(@new.ToString("X2"))).ToString();
                            

            _host.OnEventReceived("Host", $"Session {SessionID} established.");

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
                foreach(var cam in _cameras)
                    cam.Value?.Dispose();

                _cameras.Clear();


                foreach (var key in _settings.Keys)
                    RemoveSettings(key);

                foreach (var key in _activeTasks.Keys)
                    RemoveTask(key);

            }
            catch (Exception e)
            {
                throw new FaultException<ServiceFault>(
                    new ServiceFault()
                    {
                        Message = "An exception was thrown while disposing session resources.",
                        Details = e.Message
                    },
                    ServiceFault.GeneralServiceErrorReason);
            }
        }


        /// <summary>
        /// Returns number of available cameras.
        /// </summary>
        /// <exception cref="FaultException{AndorSDKException}"/>
        /// <exception cref="FaultException{ServiceFault}"/>
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
                    // Ignored for debug purposes
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
                throw AndorSdkFault.WrapFault(andorEx, nameof(Camera.GetNumberOfCameras));
          
            }
            // If failure is not realted to Andor API
            catch (Exception ex)
            {
                // rethrow it, wrapped in FaultException<>, to the client side
                throw new FaultException<ServiceFault>(
                    new ServiceFault()
                    {
                        Message = "Failed retrieving number of available cameras.",
                        Details = ex.Message,
                        MethodName = nameof(Camera.GetNumberOfCameras)
                    },
                    ServiceFault.CameraCommunicationReason);

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
                    in _settings
                    where item.Value.Camera.CameraIndex == camIndex
                    select item.Key)
                    RemoveSettings(settsKey);

                foreach (var taskKey in
                    from item
                    in _activeTasks
                    where item.Value.CameraIndex == camIndex
                    select item.Key)
                    RemoveTask(taskKey);

                if (_cameras.TryRemove(camIndex, out var removedCamera))
                {
                    _host?.OnEventReceived(removedCamera, "Camera was disposed remotely.");
                    removedCamera.Dispose();
                }
                else throw new InvalidOperationException("Camera handle is unavailable.");

            }
            catch (Exception e)
            {
                throw new FaultException<ServiceFault>(
                    new ServiceFault()
                    {
                        Message = "An exception was thrown while disposing Camera resource",
                        Details = e.Message
                    },
                    ServiceFault.GeneralServiceErrorReason);
            }
        }
        [OperationBehavior]
        public string CreateSettings(int camIndex)
        {
            CameraBase cam = GetCameraSafe(camIndex);
            SettingsBase setts = cam.GetAcquisitionSettingsTemplate();

            string settingsID = Guid.NewGuid().ToString("N");
            int counter = 0;
            while ((counter <= MaxTryAddAttempts) && !_settings.TryAdd(settingsID, setts))
                counter++;

            if (counter >= MaxTryAddAttempts)
                throw new FaultException<ServiceFault>(
                    new ServiceFault()
                    {
                        Message = "Failed to create unique ID for the settings instance.",
                        Details = $"After {MaxTryAddAttempts} sessionID was not generated",
                        MethodName = nameof(_settings.TryAdd)
                    },
                    ServiceFault.GeneralServiceErrorReason);

            _host?.OnEventReceived("Host", $"AcqSettings with ID {settingsID} created.");

            return settingsID;
        }
        [OperationBehavior]
        public void RemoveSettings(string settingsID)
        {
            _settings.TryRemove(settingsID, out SettingsBase setts);
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
            if (_activeTasks.TryRemove(taskID, out (Task Task, CancellationTokenSource Token, int CamIndex) taskInfo))
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
            int adConverterIndex,
            int amplifier)
        => GetSettingsSafe(settingsID).GetAvailableHSSpeeds(adConverterIndex, amplifier).ToArray();

        [OperationBehavior]
        public (int Index, string Name)[] GetAvailablePreAmpGain(
            string settingsID,
            int adConverterIndex,
            int amplifier,
            int hsSpeed)
        => GetSettingsSafe(settingsID).GetAvailablePreAmpGain(
            adConverterIndex, amplifier, hsSpeed).ToArray();

        [OperationBehavior]
        public (bool IsSupported, float Speed) CallIsHSSpeedSupported(
            string settingsID, 
            int adConverter,
            int amplifier,
            int speedIndex)
            => (
            IsSupported: GetSettingsSafe(settingsID)
                .IsHSSpeedSupported(speedIndex, adConverter, amplifier, out float speed),
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

            throw new FaultException<ServiceFault>(
                new ServiceFault()
                {
                    Message = "Specified camera cannot be found among active devices.",
                    Details = "Camera is not found in pool of active cameras. It might have been already disposed.",
                    MethodName = nameof(GetCameraSafe)
                },
                ServiceFault.GeneralServiceErrorReason);
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


        #region Async methods
        [OperationBehavior]
        public IAsyncResult BeginCreateCameraAsync(int camIndex, AsyncCallback callback, object state)
        {
            return new AsyncVoidResult(CreateCameraAsync(camIndex), callback, state);
        }

        public bool EndCreateCameraAsync(IAsyncResult result)
        {
            if (result is AsyncVoidResult res)
            {
                try
                {
                    res.Task.GetAwaiter().GetResult();
                    return true;
                }
                finally
                {
                    res.Dispose();
                }
            }
            throw new InvalidOperationException($"Incompatible object of type [{typeof(IAsyncResult)}] received.");
        }
        #endregion
    }
}
