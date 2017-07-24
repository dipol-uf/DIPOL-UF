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
                    new FaultReason("Connection was not properly initialized.")
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
                return Camera.GetNumberOfCameras();
            }
            catch (AndorSDKException andorEx)
            {
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
            catch (Exception e)
            {
                throw new FaultException<ServiceException>(
                    new ServiceException()
                    {
                        Message = "Failed retrieving number of available cameras.",
                        Details = e.Message,
                        MethodName = nameof(Camera.GetNumberOfCameras)
                    },
                    ServiceException.CameraCommunicationReason);

            }
            
        }

        [OperationBehavior]
        public void CreateCamera(int camIndex = 0)
        {
            // Prevents from creating the same camera
            //if (ActiveCameras.Count(item => item.Value.Camera.CameraIndex == camIndex) != 0)
            //    throw new Exception();

            Camera camera = null;

            try
            {
                camera = new Camera(camIndex);
            }
            catch (Exception e)
            {

            }


            int count = 0;
            for (; 
                !activeCameras.TryAdd(
                    activeCameras.Count + 1, 
                    (SessionID: SessionID, Camera: camera))
                & count < MaxTryAddAttempts; 
                count++)
                continue;

            if (count >= MaxTryAddAttempts)
                throw new Exception();

        }

        public void SendToClient()
        {
            Console.WriteLine(context == null);
            context.GetCallbackChannel<IRemoteCallback>().SendToClient("Hello from service");
        }

    }
}
