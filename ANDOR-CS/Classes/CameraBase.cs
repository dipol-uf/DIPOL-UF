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
using System.ComponentModel;

using ANDOR_CS.DataStructures;
using ANDOR_CS.Enums;
using ANDOR_CS.Events;


namespace ANDOR_CS.Classes
{
    /// <summary>
    /// Base abstract class to all ANDOR-device classes
    /// </summary>
    public abstract class CameraBase : IDisposable, INotifyPropertyChanged//ANDOR_CS.Interfaces.ICameraControl
    {
        protected const int AmpDescriptorMaxLength = 21;
        protected const int PreAmpGainDescriptorMaxLength = 30;
        protected const int StatusCheckTimeOutMS = 100;
        protected const int TempCheckTimeOutMS = 5000;

        private bool _IsActive = false;
        private bool _IsInitialized = false;
        private DeviceCapabilities _Capabilities = default(DeviceCapabilities);
        private CameraProperties _Properties = default(CameraProperties);
        private string _SerialNumber = "";
        private string _CameraModel = "";
        private FanMode _FanMode = FanMode.Off;
        private Switch _CoolerMode = Switch.Disabled;
        private int _CameraIndex = -1;
        private (
           ShutterMode Internal,
           ShutterMode? External,
           TTLShutterSignal Type,
           int OpenTime,
           int CloseTime) _Shutter 
            = (ShutterMode.FullyAuto, null, TTLShutterSignal.Low, 0, 0);
        private (Version EPROM, Version COFFile, Version Driver, Version Dll) _Software
            = default((Version EPROM, Version COFFile, Version Driver, Version Dll));
        private (Version PCB, Version Decode, Version CameraFirmware) _Hardware
            = default((Version PCB, Version Decode, Version CameraFirmware));
        private volatile bool _IsAcquiring = false;
        private volatile bool _IsAsyncAcquisition = false;

      
        public virtual DeviceCapabilities Capabilities
        {
            get => _Capabilities;
            protected set
            {
                if (!(value as ValueType).Equals(_Capabilities))
                {
                    _Capabilities = value;
                    OnPropertyChanged();
                }
            }
        }
        public virtual CameraProperties Properties
        {
            get => _Properties;
            protected set
            {
                if (!(value as ValueType).Equals(_Properties))
                {
                    _Properties = value;
                    OnPropertyChanged();
                }
            }
        }
        public virtual bool IsActive
        {
            get => _IsActive;
            protected set
            {
                if (value != _IsActive)
                {
                    _IsActive = value;
                    OnPropertyChanged();
                }
            }
        }
        public virtual bool IsInitialized
        {
            get => _IsInitialized;
            protected set
            {
                if (value != _IsInitialized)
                {
                    _IsInitialized = value;
                    OnPropertyChanged();
                }
            }
        }
        public virtual string SerialNumber
        {
            get => _SerialNumber;
            protected set
            {
                if (value != _SerialNumber)
                {
                    _SerialNumber = value;
                    OnPropertyChanged();
                }
            }
        }
        public virtual string CameraModel
        {
            get => _CameraModel;
            set
            {
                if (value != _CameraModel)
                {
                    _CameraModel = value;
                    OnPropertyChanged();
                }
            }
        }
        public virtual FanMode FanMode
        {
            get => _FanMode;
            protected set
            {
                if (value != _FanMode)
                {
                    _FanMode = value;
                    OnPropertyChanged();
                }
            }
        }
        public virtual Switch CoolerMode
        {
            get => _CoolerMode;
            protected set
            {
                if (value != _CoolerMode)
                {
                    _CoolerMode = value;
                    OnPropertyChanged();
                }

            }
        }
        /// <summary>
        /// Andor SDK unique index of camera; passed to constructor
        /// </summary>
        public virtual int CameraIndex
        {
            get => _CameraIndex;
            protected set
            {
                if (value != _CameraIndex)
                {
                    _CameraIndex = value;
                    OnPropertyChanged();
                }
            }
        }
        /// <summary>
        /// Indicates if camera is in process of image acquisition.
        /// </summary>
        public virtual bool IsAcquiring
        {
            get => _IsAcquiring;
            protected set
            {
                if (value != _IsAcquiring)
                {
                    _IsAcquiring = value;
                    OnPropertyChanged();
                }
            }
        }
        /// <summary>
        /// Indicates if acquisition is launched from async method and
        /// camera is able to properly fire all events.
        /// </summary>
        public virtual bool IsAsyncAcquisition
        {
            get => _IsAsyncAcquisition;
            protected set
            {
                if (value != _IsAsyncAcquisition)
                {
                    _IsAsyncAcquisition = value;
                    OnPropertyChanged();
                }
            }
        }
        public virtual (
           ShutterMode Internal,
           ShutterMode? External,
           TTLShutterSignal Type,
           int OpenTime,
           int CloseTime) Shutter
        {
            get => _Shutter;
            protected set
            {
                if (!value.Equals(_Shutter))
                {
                    _Shutter = value;
                    OnPropertyChanged();
                }
            }
        }
        public virtual (Version EPROM, Version COFFile, Version Driver, Version Dll)
            Software
        {
            get => _Software;
            protected set
            {
                if (!value.Equals(_Software))
                {
                    _Software = value;
                    OnPropertyChanged();
                }
            }
        }
        public virtual (Version PCB, Version Decode, Version CameraFirmware)
            Hardware
        {
            get => _Hardware;
            protected set
            {
                if (!value.Equals(_Hardware))
                {
                    _Hardware = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Fires when one of the properties was changed
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        /// <summary>
        /// Fires when acquisition is started.
        /// </summary>
        public event AcquisitionStatusEventHandler AcquisitionStarted;
        /// <summary>
        /// Fires when acquisition is finished.
        /// </summary>
        public event AcquisitionStatusEventHandler AcquisitionFinished;
        /// <summary>
        /// Fires when acquisition status is asynchronously checked
        /// </summary>
        public event AcquisitionStatusEventHandler AcquisitionStatusChecked;
        /// <summary>
        /// Fires when an exception is thrown in a background asynchronous task
        /// </summary>
        public event AcquisitionStatusEventHandler AcquisitionErrorReturned;
        /// <summary>
        /// Fires when acquisition is aborted manually
        /// </summary>
        public event AcquisitionStatusEventHandler AcquisitionAborted;
        /// <summary>
        /// Fires when backround task acsynchronously checks temperature
        /// </summary>
        public event TemperatureStatusEventHandler TemperatureStatusChecked;



        /// <summary>
        /// When overriden in a derived class, returns current status of the camera
        /// </summary>
        /// <returns>Status of a camera</returns>
        public abstract CameraStatus GetStatus();
        /// <summary>
        /// When overriden in derived class, disposes camera instance and frees all resources.
        /// </summary>
        public abstract void Dispose();
        /// <summary>
        /// When overriden in derived class, returns current camera temperature and temperature status
        /// </summary>
        /// <returns>Temperature status and temperature in degrees</returns>
        public abstract (TemperatureStatus Status, float Temperature) GetCurrentTemperature();
        public abstract void SetActive();
        public abstract void FanControl(FanMode mode);
        public abstract void CoolerControl(Switch mode);
        public abstract void SetTemperature(int temperature);
        public abstract void ShutterControl(
           int clTime,
           int opTime,
           ShutterMode inter,
           ShutterMode exter = ShutterMode.FullyAuto,
           TTLShutterSignal type = TTLShutterSignal.Low);
        public abstract void TemperatureMonitor(Switch mode, int timeout);

        /// <summary>
        /// Fires <see cref="PropertyChanged"/> event
        /// </summary>
        /// <param name="property">Compiler-filled name of the property that fires event.</param>
        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string property = "")
        {
            //Console.WriteLine($"CameraBase.OnPropertyChanged({property})");
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
        /// <summary>
        /// Fires <see cref="AcquisitionStarted"/> event.
        /// </summary>
        /// <param name="e">Status of camera at the beginning of acquisition</param>
        protected virtual void OnAcquisitionStarted(AcquisitionStatusEventArgs e) 
            => AcquisitionStarted?.Invoke(this, e);
        /// <summary>
        /// Fires <see cref="AcquisitionStatusChecked"/> event.
        /// </summary>
        /// <param name="e">Status of camera during acquisition</param>
        protected virtual void OnAcquisitionStatusChecked(AcquisitionStatusEventArgs e) 
            => AcquisitionStatusChecked?.Invoke(this, e);
        /// <summary>
        /// Fires <see cref="AcquisitionFinished"/> event.
        /// </summary>
        /// <param name="e">Status of camera at the end of acquisition</param>
        protected virtual void OnAcquisitionFinished(AcquisitionStatusEventArgs e) 
            => AcquisitionFinished?.Invoke(this, e);
        /// <summary>
        /// Fires <see cref="AcquisitionErrorReturned"/> event.
        /// </summary>
        /// <param name="e">Status of camera when exception was thrown</param>
        protected virtual void OnAcquisitionErrorReturned(AcquisitionStatusEventArgs e) 
            => AcquisitionErrorReturned?.Invoke(this, e);
        /// <summary>
        /// Fires <see cref="AcquisitionAborted"/> event.
        /// </summary>
        /// <param name="e">Status of camera when abortion happeed</param>
        protected virtual void OnAcquisitionAborted(AcquisitionStatusEventArgs e) 
            => AcquisitionAborted?.Invoke(this, e);
        /// <summary>
        /// Fires <see cref="TemperatureStatusChecked"/> event
        /// </summary>
        /// <param name="e">Status of the camera when temperature was checked.</param>
        protected virtual void OnTemperatureStatusChecked(TemperatureStatusEventArgs e) 
            => TemperatureStatusChecked?.Invoke(this, e);
    }

    
}
