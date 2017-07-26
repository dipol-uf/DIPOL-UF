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
        private static ConcurrentDictionary<(string SessionID, int CameraIndex), CameraBase> remoteCameras
            = new ConcurrentDictionary<(string SessionID, int CameraIndex), CameraBase>();


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

        public override CameraProperties Properties
        {
            get
            {
                if (changedProperties.TryGetValue(NameofProperty(), out bool hasChanged) && hasChanged)
                {
                    _Properties = session.GetProperties(CameraIndex);
                    changedProperties.TryUpdate(NameofProperty(), false, true);
                }

                return _Properties;
            }
            protected set => throw new NotSupportedException();
        }

        public override bool IsInitialized
        {
            get
            {
                if (changedProperties.TryGetValue(NameofProperty(), out bool hasChanged) && hasChanged)
                {
                    _IsInitialized = session.GetIsInitialized(CameraIndex);
                    changedProperties.TryUpdate(NameofProperty(), false, true);
                }

                return _IsInitialized;
            }
            protected set => throw new NotSupportedException();
        }

        public override FanMode FanMode
        {
            get
            {
                if (changedProperties.TryGetValue(NameofProperty(), out bool hasChanged) && hasChanged)
                {
                    _FanMode = session.GetFanMode(CameraIndex);
                    changedProperties.TryUpdate(NameofProperty(), false, true);
                }

                return _FanMode;
            }
            protected set => throw new NotSupportedException();
        }

        public override Switch CoolerMode
        {
            get
            {
                    if (changedProperties.TryGetValue(NameofProperty(), out bool hasChanged) && hasChanged)
                    {
                        _CoolerMode = session.GetCoolerMode(CameraIndex);
                        changedProperties.TryUpdate(NameofProperty(), false, true);
                    }

                    return _CoolerMode;
                }
            protected set => throw new NotSupportedException();
        }

        public override DeviceCapabilities Capabilities 
        {
            get
            {
                if (changedProperties.TryGetValue(NameofProperty(), out bool hasChanged) && hasChanged)
                {
                    _Capabilities= session.GetCapabilities(CameraIndex);
                    changedProperties.TryUpdate(NameofProperty(), false, true);
                }

                return _Capabilities;
            }
            protected set => throw new NotSupportedException();
        }


        internal RemoteCamera(IRemoteControl sessionInstance, int camIndex)
        {
            session = sessionInstance ?? throw new ArgumentNullException("Session cannot be null.");
            _CameraIndex = camIndex;

            remoteCameras.TryAdd((session.SessionID, camIndex), this);

            _CameraModel = session.GetCameraModel(CameraIndex);
            _SerialNumber = session.GetSerialNumber(CameraIndex);
            _IsActive = session.GetIsActive(CameraIndex);
            _Properties = session.GetProperties(CameraIndex);
            _IsInitialized = session.GetIsInitialized(CameraIndex);
            _FanMode = session.GetFanMode(CameraIndex);
            _CoolerMode = session.GetCoolerMode(CameraIndex);
            _Capabilities = session.GetCapabilities(CameraIndex);
            
        }


        public override CameraStatus GetStatus()
            => session.CallGetStatus(CameraIndex);

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
