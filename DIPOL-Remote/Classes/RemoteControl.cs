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

#define NO_ACTUAL_CAMERA

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ServiceModel;
using System.Reflection;

using DIPOL_Remote.Faults;

using ANDOR_CS.Classes;
using ANDOR_CS.Exceptions;

using ICameraControl = ANDOR_CS.Interfaces.ICameraControl;

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
    public class RemoteControl : IRemoteControl, IDisposable
    {
        private static readonly int MaxTryAddAttempts = 10;

        /// <summary>
        /// Unique ID of the current session
        /// </summary>
        private string sessionID = null;
        /// <summary>
        /// Operation context of current connection. Used for callbacks
        /// </summary>
        private OperationContext context;

        /// <summary>
        /// Thread-safe collection of all active <see cref="RemoteControl"/> service instances.
        /// </summary>
        private static ConcurrentDictionary<string, RemoteControl> serviceInstances 
            = new ConcurrentDictionary<string, RemoteControl>();

        /// <summary>
        /// Thread-safe collection of active remote cameras.
        /// </summary>
        private static ConcurrentDictionary<int, (string SessionID, ICameraControl Camera)> activeCameras
            = new ConcurrentDictionary<int, (string SessionID, ICameraControl Camera)>();

        /// <summary>
        /// Default constructor
        /// </summary>
        private RemoteControl()
        {
         
        }
                
        /// <summary>
        /// Unique ID of current session
        /// </summary>
        public string SessionID
        {
            [OperationBehavior]
            get => sessionID;
            
        }

        /// <summary>
        /// Interface to collection of all active <see cref="RemoteControl"/> service instances.
        /// </summary>
        public static IReadOnlyDictionary<string, RemoteControl> ActiveConnections 
            => serviceInstances as IReadOnlyDictionary<string, RemoteControl>;
        
        /// <summary>
        /// Interface to collectio of all active cameras of all sessions
        /// </summary>
        public static IReadOnlyDictionary<int, (string SessionID, ICameraControl Camera)> ActiveCameras
            => activeCameras as IReadOnlyDictionary<int, (string SessionID, ICameraControl Camera)>;
        

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

        }

        /// <summary>
        /// Exit point for any connection. Frees resources.
        /// </summary>
        [OperationBehavior]
        public void Disconnect()
        {
           Dispose();
        }
        
        /// <summary>
        /// Implementation of <see cref="IDisposable"/> interface. Frees resources.
        /// </summary>
        public void Dispose()
        {
            // Select all cameras that are created from this session
            foreach (var key in
                from item
                in activeCameras
                where item.Value.SessionID == SessionID
                select item.Key)
                // Remove these cameras from te collection
                if (activeCameras.TryRemove(key, out (string SessionID, ICameraControl Camera) camInfo))
                    // If successful, dispose camera instance
                    camInfo.Camera.Dispose();

            // Remove this service from collection
            serviceInstances.TryRemove(sessionID, out _);
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
           
            ICameraControl camera = null;

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

#if NO_ACTUAL_CAMERA
            (camera as DebugCamera).PropertyChanged += (sender, e)
                => context.GetCallbackChannel<IRemoteCallback>()
                .NotifyRemotePropertyChanged(
                    camera.CameraIndex,
                    SessionID,
                    e.PropertyName);
#endif

        }

        [OperationBehavior]
        public void RemoveCamera(int camIndex)
        {
            GetCameraSafe(sessionID, camIndex).Dispose();
            activeCameras.TryRemove(camIndex, out _);

            //if (ActiveCameras.TryGetValue(
            //    camIndex,
            //    out (string SessionID, ICameraControl Camera) camInfo))
            //    if (camInfo.SessionID == SessionID)
            //    {
            //        camInfo.Camera.Dispose();
            //        activeCameras.TryRemove(camIndex, out _);
            //    }
            //    else throw ServiceException.IllegalSessionFaultException();

            //else throw new FaultException<ServiceException>(
            //    new ServiceException()
            //    {
            //        Message = "Specified camera cannot be found among active devices.",
            //        Details = "Camera is not found in pool of active cameras. It might have been already disposed.",
            //        MethodName = nameof(ActiveCameras.TryGetValue)
            //    },
            //    ServiceException.GeneralServiceErrorReason);        

                        
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
        

        private ICameraControl GetCameraSafe(string session, int camIndex)
        {
            if (ActiveCameras.TryGetValue(
                camIndex,
                out (string SessionID, ICameraControl Camera) camInfo))

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
    }
}
