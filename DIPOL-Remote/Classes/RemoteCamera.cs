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
        private static ConcurrentDictionary<int, ICameraControl> remoteCameras
            = new ConcurrentDictionary<int, ICameraControl>();


        private ConcurrentDictionary<string, bool> changedProperties
            = new ConcurrentDictionary<string, bool>();

        private IRemoteControl session;

        private string _SerialNumber = "";
        private string _CameraModel = "";
        


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
            get
            {
                if (changedProperties.TryGetValue(NameofProperty(), out bool changed) && changed)
                {
                    _CameraModel = session.GetCameraModel(CameraIndex);
                    changedProperties.AddOrUpdate(NameofProperty(), false, (prop, oldVal) => false);
                }
                
                return _CameraModel;
                
            }
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
            get
            {
                return _SerialNumber;
            }
            
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

            _CameraModel = session.GetCameraModel(CameraIndex);
            
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

            if (remoteCameras.TryGetValue(camIndex, out ICameraControl camera))
                (camera as RemoteCamera).changedProperties.AddOrUpdate(property, true, (prop, oldVal) => true);
        }

        private static string NameofProperty([System.Runtime.CompilerServices.CallerMemberName] string name = "")
            => name;

    }
}
