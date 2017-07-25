using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ICameraControl = ANDOR_CS.Interfaces.ICameraControl;
using IRemoteControl = DIPOL_Remote.Interfaces.IRemoteControl;

using ANDOR_CS.Enums;
using ANDOR_CS.DataStructures;

namespace DIPOL_Remote.Classes
{
    public class RemoteCamera : ICameraControl
    {
        private IRemoteControl session;

        private ConcurrentDictionary<int, ICameraControl> remoteCameras
            = new ConcurrentDictionary<int, ICameraControl>();


        public DeviceCapabilities Capabilities
        {
            get;
            private set;
        }

        public CameraProperties Properties
        {
            get;
            private set;
        }

        public bool IsInitialized
        {
            get;
            private set;
        }

        public string CameraModel
        {
            get;
            private set;
        }

        public Switch CoolerMode
        {
            get;
            private set;
        }

        public bool IsActive
        {
            get;
            private set;
        }

        public string SerialNumber
        {
            get;
            private set;
        }

        public FanMode FanMode
        {
            get;
            private set;
        }

        // Property is client-side
        public int CameraIndex
        {
            get;
            private set;
        }


        internal RemoteCamera(IRemoteControl sessionInstance, int camIndex)
        {
            session = sessionInstance ?? throw new ArgumentNullException("Session cannot be null.");
            CameraIndex = camIndex;

            remoteCameras.TryAdd(camIndex, this);
        }


        public CameraStatus GetStatus()
            => CameraStatus.Idle;

        public void Dispose()
        {
            session.RemoveCamera(CameraIndex);
            remoteCameras.TryRemove(CameraIndex, out _);
        }


        public static void NotifyRemotePropertyChanged(int camIndex, string sessionID, string property)
        {
            Console.WriteLine($"Property {property} of camera {camIndex} changed in session {sessionID}.");
        }

    }
}
