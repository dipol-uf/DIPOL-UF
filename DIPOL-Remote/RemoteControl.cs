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
using System.Text;
using System.Threading.Tasks;

using System.ServiceModel;

using DIPOL_Remote.Faults;

using ANDOR_CS.Classes;
using ANDOR_CS.Exceptions;

namespace DIPOL_Remote
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
        private static ConcurrentDictionary<int, (string SessionID, Camera Camera)> activeCameras
            = new ConcurrentDictionary<int, (string SessionID, Camera Camera)>();
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
        {
            get => serviceInstances as IReadOnlyDictionary<string, RemoteControl>;
        }

        public static IReadOnlyDictionary<int, (string SessionID, Camera Camera)> ActiveCameras
        {
            get => activeCameras as IReadOnlyDictionary<int, (string SessionID, Camera Camera)>;
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
               // Trys to retrieve the number of available cameras
                return Camera.GetNumberOfCameras();
            }
            // If method fails and Andor-related exception is thrown
            catch (AndorSDKException andorEx)
            {
                // rethrow it, wrapped in FaultException<>, to the client side
                throw new FaultException<AndorSDKServiceException>(
                    new AndorSDKServiceException()
                    {
                        Message = "Failed retrieving number of available cameras.",
                        Details = andorEx.Message,
                        ErrorCode = andorEx.ErrorCode,
                        MethodName = nameof(Camera.GetNumberOfCameras)
                    },
                    ServiceException.CameraCommunicationReason);
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
           
            Camera camera = null;

            try
            {
                // Tries to create new remote camera
#if NO_ACTUAL_CAMERA
                camera = Camera.GetDebugInterface();
                
#else
                camera = new Camera(camIndex);
#endif
            }
            // Andor-related exception
            catch (AndorSDKException andorEx)
            {
                throw new FaultException<AndorSDKServiceException>(
                    new AndorSDKServiceException()
                    {
                        Message = "Failed to create new remote camera.",
                        Details = andorEx.Message,
                        ErrorCode = andorEx.ErrorCode,
                        MethodName = nameof(Camera)
                    },
                    ServiceException.CameraCommunicationReason);
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

            // Tries to add created camera to te dictionary of
            // active cameras.
            int count = 0;
            for (; 
                !activeCameras.TryAdd(
                    activeCameras.Count + 1, 
                    (SessionID: SessionID, Camera: camera))
                & count < MaxTryAddAttempts; 
                count++)
                continue;

            // If number of attempts exceeds limit
            if (count >= MaxTryAddAttempts)
            {
                // Clena & and throw exception
                camera.Dispose();
                throw new  FaultException<ServiceException>(
                    new ServiceException()
                    {
                        Message = "Failed to add new remote camera to the dictionary.",
                        Details = "Several unsuccessful attemps were made to add new camera to the list of exising ones.",
                        MethodName = nameof(activeCameras.TryAdd)
                    },
                    ServiceException.GeneralServiceErrorReason);
            }

        }

        public void SendToClient()
        {
            Console.WriteLine(context == null);
            context.GetCallbackChannel<IRemoteCallback>().SendToClient("Hello from service");
        }

    }
}
