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
using System.CodeDom;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ANDOR_CS.DataStructures;
using ANDOR_CS.Enums;
using ANDOR_CS.Events;
using DipolImage;

namespace ANDOR_CS.Classes
{
    /// <summary>
    /// Base abstract class to all ANDOR-device classes
    /// </summary>
    public abstract class CameraBase : IDisposable, INotifyPropertyChanged
    {
        protected const int AmpDescriptorMaxLength = 21;
        protected const int PreAmpGainDescriptorMaxLength = 30;
        protected const int StatusCheckTimeOutMs = 100;
        protected const int TempCheckTimeOutMs = 5000;

        protected bool _isDisposed;

        private bool _isActive;
        private bool _isInitialized;
        private DeviceCapabilities _capabilities = default(DeviceCapabilities);
        private CameraProperties _properties = default(CameraProperties);
        private string _serialNumber = "";
        private string _cameraModel = "";
        private FanMode _fanMode = FanMode.Off;
        private Switch _coolerMode = Switch.Disabled;
        private int _cameraIndex = -1;
        private (
           ShutterMode Internal,
           ShutterMode? External,
           TtlShutterSignal Type,
           int OpenTime,
           int CloseTime) _shutter
            = (ShutterMode.FullyAuto, null, TtlShutterSignal.Low, 27, 27);
        private (Version EPROM, Version COFFile, Version Driver, Version Dll) _software
            = default((Version EPROM, Version COFFile, Version Driver, Version Dll));
        private (Version PCB, Version Decode, Version CameraFirmware) _hardware
            = default((Version PCB, Version Decode, Version CameraFirmware));
        private bool _isTemperatureMonitored;
        private volatile bool _isAcquiring;
        private volatile bool _isAsyncAcquisition;

        protected ConcurrentQueue<Image> _acquiredImages = new ConcurrentQueue<Image>();


        public virtual bool IsTemperatureMonitored
        {
            get => _isTemperatureMonitored;
            set
            {
                if (value != _isTemperatureMonitored)
                {
                    _isTemperatureMonitored = value;
                    OnPropertyChanged();
                }
            }

        }
        public bool IsDisposed => _isDisposed;

        public virtual DeviceCapabilities Capabilities
        {
            get => _capabilities;
            protected set
            {
                if (!(value as ValueType).Equals(_capabilities))
                {
                    _capabilities = value;
                    OnPropertyChanged();
                }
            }
        }
        public virtual CameraProperties Properties
        {
            get => _properties;
            protected set
            {
                if (!(value as ValueType).Equals(_properties))
                {
                    _properties = value;
                    OnPropertyChanged();
                }
            }
        }
        public virtual bool IsActive
        {
            get => _isActive;
            protected set
            {
                if (value != _isActive)
                {
                    _isActive = value;
                    OnPropertyChanged();
                }
            }
        }
        public virtual bool IsInitialized
        {
            get => _isInitialized;
            protected set
            {
                if (value != _isInitialized)
                {
                    _isInitialized = value;
                    OnPropertyChanged();
                }
            }
        }
        public virtual string SerialNumber
        {
            get => _serialNumber;
            protected set
            {
                if (value != _serialNumber)
                {
                    _serialNumber = value;
                    OnPropertyChanged();
                }
            }
        }
        public virtual string CameraModel
        {
            get => _cameraModel;
            set
            {
                if (value != _cameraModel)
                {
                    _cameraModel = value;
                    OnPropertyChanged();
                }
            }
        }
        public virtual FanMode FanMode
        {
            get => _fanMode;
            protected set
            {
                if (value != _fanMode)
                {
                    _fanMode = value;
                    OnPropertyChanged();
                }
            }
        }
        public virtual Switch CoolerMode
        {
            get => _coolerMode;
            protected set
            {
                if (value != _coolerMode)
                {
                    _coolerMode = value;
                    OnPropertyChanged();
                }

            }
        }
        /// <summary>
        /// Andor SDK unique index of camera; passed to constructor
        /// </summary>
        public virtual int CameraIndex
        {
            get => _cameraIndex;
            protected set
            {
                if (value != _cameraIndex)
                {
                    _cameraIndex = value;
                    OnPropertyChanged();
                }
            }
        }
        /// <summary>
        /// Indicates if camera is in process of image acquisition.
        /// </summary>
        public virtual bool IsAcquiring
        {
            get => _isAcquiring;
            protected set
            {
                if (value != _isAcquiring)
                {
                    _isAcquiring = value;
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
            get => _isAsyncAcquisition;
            protected set
            {
                if (value != _isAsyncAcquisition)
                {
                    _isAsyncAcquisition = value;
                    OnPropertyChanged();
                }
            }
        }
        public virtual (
           ShutterMode Internal,
           ShutterMode? External,
           TtlShutterSignal Type,
           int OpenTime,
           int CloseTime) Shutter
        {
            get => _shutter;
            protected set
            {
                if (!value.Equals(_shutter))
                {
                    _shutter = value;
                    OnPropertyChanged();
                }
            }
        }
        public virtual (Version EPROM, Version COFFile, Version Driver, Version Dll)
            Software
        {
            get => _software;
            protected set
            {
                if (!value.Equals(_software))
                {
                    _software = value;
                    OnPropertyChanged();
                }
            }
        }
        public virtual (Version PCB, Version Decode, Version CameraFirmware)
            Hardware
        {
            get => _hardware;
            protected set
            {
                if (!value.Equals(_hardware))
                {
                    _hardware = value;
                    OnPropertyChanged();
                }
            }
        }
        public virtual ConcurrentQueue<Image> AcquiredImages => _acquiredImages;
        public virtual SettingsBase CurrentSettings
        {
            get;
            internal set;
        } = null;


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
        public event NewImageReceivedHandler NewImageReceived;

        /// <summary>
        /// When overriden in a derived class, returns current status of the camera
        /// </summary>
        /// <returns>Status of a camera</returns>
        public abstract CameraStatus GetStatus();
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
           TtlShutterSignal type = TtlShutterSignal.Low);
        public abstract void TemperatureMonitor(Switch mode, int timeout = 150);
        public abstract SettingsBase GetAcquisitionSettingsTemplate();

        public abstract void StartAcquisition();

        /// <summary>
        /// A synchronous way to manually abort acquisition.
        /// NOTE: if called while async acquisition is in progress, throws
        /// <see cref="TaskCanceledException"/>. To cancel async acquisition, use 
        /// <see cref="CancellationToken"/>.
        /// </summary>
        /// <exception cref="AndorSDKException"/>
        /// <exception cref="TaskCanceledException"/>
        public abstract void AbortAcquisition();

        public abstract Task StartAcquistionAsync(CancellationTokenSource token, int timeout);

        /// <summary>
        /// String representation of the camera instance.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{CameraModel} [{SerialNumber}]";
        }
        public override int GetHashCode()
        {
            var strRep = $"{CameraIndex} {ToString()}";
            int hash = 0;
            foreach (var ch in strRep)
                hash += BitConverter.GetBytes(ch).Aggregate(hash, (current, b) => current + b);
            return hash;
        }
        public override bool Equals(object obj)
        {
            if (obj is CameraBase cam)
                return
                    cam.CameraIndex == CameraIndex &
                    cam.CameraModel == CameraModel &
                    cam.SerialNumber == SerialNumber;
            return false;
        }


        public virtual void CheckIsDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException("Camera instance is already disposed");
        }

        /// <inheritdoc />
        /// <summary>
        /// When overriden in derived class, disposes camera instance and frees all resources.
        /// </summary>
        public virtual void Dispose()
        {
            if (!_isDisposed)
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }
        protected virtual void Dispose(bool disposing)
        {
            _isDisposed = true;
        }

        /// <summary>
        /// Fires <see cref="PropertyChanged"/> event
        /// </summary>
        /// <param name="property">Compiler-filled name of the property that fires event.</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string property = "")
        {
            if (!IsDisposed)
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
        /// <summary>
        /// Fires <see cref="AcquisitionStarted"/> event.
        /// </summary>
        /// <param name="e">Status of camera at the beginning of acquisition</param>
        protected virtual void OnAcquisitionStarted(AcquisitionStatusEventArgs e)
        {
            if (!IsDisposed)
                AcquisitionStarted?.Invoke(this, e);
        }
        /// <summary>
        /// Fires <see cref="AcquisitionStatusChecked"/> event.
        /// </summary>
        /// <param name="e">Status of camera during acquisition</param>
        protected virtual void OnAcquisitionStatusChecked(AcquisitionStatusEventArgs e)
        {
            if (!IsDisposed)
                AcquisitionStatusChecked?.Invoke(this, e);
        }
        /// <summary>
        /// Fires <see cref="AcquisitionFinished"/> event.
        /// </summary>
        /// <param name="e">Status of camera at the end of acquisition</param>
        protected virtual void OnAcquisitionFinished(AcquisitionStatusEventArgs e)
        {
            if (!IsDisposed)
                AcquisitionFinished?.Invoke(this, e);
        }
        /// <summary>
        /// Fires <see cref="AcquisitionErrorReturned"/> event.
        /// </summary>
        /// <param name="e">Status of camera when exception was thrown</param>
        protected virtual void OnAcquisitionErrorReturned(AcquisitionStatusEventArgs e)
        {
            if (!IsDisposed)
                AcquisitionErrorReturned?.Invoke(this, e);
        }
        /// <summary>
        /// Fires <see cref="AcquisitionAborted"/> event.
        /// </summary>
        /// <param name="e">Status of camera when abortion happeed</param>
        protected virtual void OnAcquisitionAborted(AcquisitionStatusEventArgs e)
        {
            if (!IsDisposed)
                AcquisitionAborted?.Invoke(this, e);
        }
        /// <summary>
        /// Fires <see cref="TemperatureStatusChecked"/> event
        /// </summary>
        /// <param name="e">Status of the camera when temperature was checked.</param>
        protected virtual void OnTemperatureStatusChecked(TemperatureStatusEventArgs e)
        {
            if (!IsDisposed)
                TemperatureStatusChecked?.Invoke(this, e);
        }
        protected virtual void OnNewImageReceived(NewImageReceivedEventArgs e)
        {
            if (!IsDisposed)
                NewImageReceived?.Invoke(this, e);
        }


        public static CameraBase Create(int camIndex = 0, object otherParams = null)
            => throw new NotSupportedException($"Cannot create instance of abstract class {nameof(CameraBase)}.");

        public static async Task<CameraBase> CreateAsync(int camIndex = 0, object otherParams = null)
            => await Task.Run(() => Create(camIndex, otherParams));


        ~CameraBase()
        {
            Dispose(false);
        }
    }


}
