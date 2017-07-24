using System;
using System.Collections.Generic;
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


        public DeviceCpabilities Capabilities
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

        public int CameraIndex
        {
            get;
            private set;
        }


        internal RemoteCamera(IRemoteControl sessionInstance, int camIndex)
        {
            session = sessionInstance;
            CameraIndex = camIndex;
        }


        public CameraStatus GetStatus()
            => CameraStatus.Idle;

        public void Dispose()
        {

        }

    }
}
