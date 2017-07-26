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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel;

using ANDOR_CS.Enums;
using ANDOR_CS.DataStructures;
using ANDOR_CS.Interfaces;

using CallerMemberNameAttribute = System.Runtime.CompilerServices.CallerMemberNameAttribute;

namespace ANDOR_CS.Classes
{
    public class DebugCamera : CameraBase
    {
        //private string _SerialNumber = "XYZ-1234";
        //private DeviceCapabilities _Capabilities = default(DeviceCapabilities);
        //private CameraProperties _Properties = default(CameraProperties);
        //private bool _IsActive = true;
        //private bool _IsInitialized = true;
        //private string _CameraModel = "DEBUG-CAMERA-INTERFACE";
        //private FanMode _FanMode = FanMode.Off;
        //private Switch _CoolerMode = Switch.Disabled;
        //private int _CameraIndex = -1;

        //public event PropertyChangedEventHandler PropertyChanged;


        //public DeviceCapabilities Capabilities
        //{
        //    get => _Capabilities;
        //    private set
        //    {
        //        if (!(value as ValueType).Equals(_Capabilities))
        //        {
        //            _Capabilities = value;
        //            OnPropertyChanged();
        //        }
        //    }
        //}

        //public CameraProperties Properties
        //{
        //    get => _Properties;
        //    private set
        //    {
        //        if (!(value as ValueType).Equals(_Properties))
        //        {
        //            _Properties = value;
        //            OnPropertyChanged();
        //        }
        //    }
        //}

        //public bool IsActive
        //{
        //    get => _IsActive;
        //    private set
        //    {
        //        if (value != _IsActive)
        //        {
        //            _IsActive = value;
        //            OnPropertyChanged();
        //        }
        //    }
        //}

        //public bool IsInitialized
        //{
        //    get => _IsInitialized;
        //    private set
        //    {
        //        if (value != _IsInitialized)
        //        {
        //            _IsInitialized = value;
        //            OnPropertyChanged();
        //        }
        //    }
        //}

        //public string SerialNumber
        //{
        //    get => _SerialNumber;
        //    private set
        //    {
        //        if (value != _SerialNumber)
        //        {
        //            _SerialNumber = value;
        //            OnPropertyChanged();
        //        }
        //    }
        //}

        //public string CameraModel
        //{
        //    get => _CameraModel;
        //    set
        //    {
        //        if (value != _CameraModel)
        //        {
        //            _CameraModel = value;
        //            OnPropertyChanged();
        //        }
        //    }
        //}

        //public FanMode FanMode
        //{
        //    get => _FanMode;
        //    private set
        //    {
        //        if (value != _FanMode)
        //        {
        //            _FanMode = value;
        //            OnPropertyChanged();
        //        }
        //    }
        //}

        //public Switch CoolerMode
        //{
        //    get => _CoolerMode;
        //    private set
        //    {
        //        if (value != _CoolerMode)
        //        {
        //            _CoolerMode = value;
        //            OnPropertyChanged();
        //        }

        //    }
        //}

        //public int CameraIndex
        //{
        //    get => _CameraIndex;
        //    private set
        //    {
        //        if (value != _CameraIndex)
        //        {
        //            _CameraIndex = value;
        //            OnPropertyChanged();
        //        }
        //    }
        //}


        public override CameraStatus GetStatus() => CameraStatus.Idle;


        public DebugCamera(int camIndex)
        {
            CameraIndex = camIndex;
            Console.WriteLine($"DebugCamera {camIndex} created.");
        }

        public override void Dispose() 
            => Console.WriteLine($"DebugCamera {CameraIndex} disposed");
      

    }
}
