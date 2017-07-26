using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.ComponentModel;
using System.Threading.Tasks;

using CameraBase = ANDOR_CS.Classes.CameraBase;
using IRemoteControl = DIPOL_Remote.Interfaces.IRemoteControl;

using ANDOR_CS.Enums;
using ANDOR_CS.DataStructures;

namespace DIPOL_Remote.Classes
{
    public class RemoteCamera : CameraBase
    {
        private static ConcurrentDictionary<(string,int), CameraBase> remoteCameras
            = new ConcurrentDictionary<(string, int), CameraBase>();


        private ConcurrentDictionary<string, bool> changedProperties
            = new ConcurrentDictionary<string, bool>();

        private IRemoteControl session;

        public override string CameraModel
        {
            get
            {
                if (changedProperties.TryGetValue(NameofProperty(), out bool hasChanged) && hasChanged)
                {
                    _CameraModel = session.GetCameraModel(CameraIndex);
                    changedProperties.TryUpdate(NameofProperty(), false, true);
                }

                return _CameraModel;
            }
            set => throw new NotSupportedException();
        }

        public override string SerialNumber
        {
            get
            {
                if (changedProperties.TryGetValue(NameofProperty(), out bool hasChanged) && hasChanged)
                {
                    _SerialNumber = session.GetSerialNumber(CameraIndex);
                    changedProperties.TryUpdate(NameofProperty(), false, true);
                }

                return _SerialNumber;
            }
            protected set => throw new NotSupportedException();
        }

        public override bool IsActive
        {
            get
            {
                if (changedProperties.TryGetValue(NameofProperty(), out bool hasChanged) && hasChanged)
                {
                    _IsActive = session.GetIsActive(CameraIndex);
                    changedProperties.TryUpdate(NameofProperty(), false, true);
                }

                return _IsActive;
            }
            protected set => throw new NotSupportedException();
        }



        internal RemoteCamera(IRemoteControl sessionInstance, int camIndex)
        {
            session = sessionInstance ?? throw new ArgumentNullException("Session cannot be null.");
            _CameraIndex = camIndex;

            remoteCameras.TryAdd((session.SessionID, camIndex), this);

            _CameraModel = session.GetCameraModel(CameraIndex);
            
        }


        public override CameraStatus GetStatus()
            => CameraStatus.Idle;

        public override void Dispose()
        {
            session.RemoveCamera(CameraIndex);
            remoteCameras.TryRemove((session.SessionID, CameraIndex), out _);
        }
       

        //protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string property = "")
        //    => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));

        public static void NotifyRemotePropertyChanged(int camIndex, string sessionID, string property)
        {
            
            if (remoteCameras.TryGetValue((sessionID, camIndex), out CameraBase camera))
            {
                (camera as RemoteCamera).changedProperties.AddOrUpdate(property, true, (prop, oldVal) => true);
                (camera as RemoteCamera).OnPropertyChanged(property);
            }
        }

        private static string NameofProperty([System.Runtime.CompilerServices.CallerMemberName] string name = "")
            => name;

        
    }
}
