﻿//    This file is part of Dipol-3 Camera Manager.

//     MIT License
//     
//     Copyright(c) 2018 Ilia Kosenkov
//     
//     Permission is hereby granted, free of charge, to any person obtaining a copy
//     of this software and associated documentation files (the "Software"), to deal
//     in the Software without restriction, including without limitation the rights
//     to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//     copies of the Software, and to permit persons to whom the Software is
//     furnished to do so, subject to the following conditions:
//     
//     The above copyright notice and this permission notice shall be included in all
//     copies or substantial portions of the Software.
//     
//     THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//     IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//     FITNESS FOR A PARTICULAR PURPOSE AND NONINFINGEMENT. IN NO EVENT SHALL THE
//     AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//     LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//     OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//     SOFTWARE.

using ANDOR_CS.DataStructures;
using ANDOR_CS.Enums;
using ANDOR_CS.Events;
using ANDOR_CS.Exceptions;
using DipolImage;
using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;

#pragma warning disable 1591
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
        protected const int MaxImagesPerCall = 32;

        protected string _imgDir = SettingsProvider.Settings.Get<string>("ImageDir");

        private bool _isDisposed;
        private bool _isDisposing;

        private Timer _temperatureMonitorTimer;
        private string _filePattern;
        private bool _isActive;
        private bool _isInitialized;
        private DeviceCapabilities _capabilities;
        private CameraProperties _properties;
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

        private (Version EPROM, Version COFFile, Version Driver, Version Dll) _software;
        private (Version PCB, Version Decode, Version CameraFirmware) _hardware;
        private bool _isTemperatureMonitored;
        private volatile bool _isAcquiring;
        private volatile bool _isAsyncAcquisition;

        // ReSharper disable once InconsistentNaming
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

        public bool IsDisposed
        {
            get => _isDisposed;
            protected set
            {
                if (value != _isDisposed)
                {
                    _isDisposed = value;
                    OnPropertyChanged();
                }
            }
        }
        public bool IsDisposing
        {
            get => _isDisposing;
            protected set
            {
                if (value != _isDisposing)
                {
                    _isDisposing = value;
                    OnPropertyChanged();
                }
            }
        }

        public virtual string FilePattern
        {
            get => _filePattern;
            protected set
            {
                if (value != _filePattern)
                {
                    _filePattern = value;
                    OnPropertyChanged();
                }
            }
        }

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
        public virtual SettingsBase CurrentSettings { get; internal set; } = null;


        /// <inheritdoc />
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
        /// Fires when background task asynchronously checks temperature
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
            ShutterMode extrn = ShutterMode.FullyAuto,
            TtlShutterSignal type = TtlShutterSignal.Low);



        protected virtual void TemperatureMonitorWorker(object sender, ElapsedEventArgs e)
        {
            if (sender is Timer timer && timer.Enabled && !IsDisposed)
            {
                var (status, temp) = GetCurrentTemperature();
                OnTemperatureStatusChecked(new TemperatureStatusEventArgs(status, temp));
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    IsDisposing = true;
                    if (!(_temperatureMonitorTimer is null))
                    {
                        _temperatureMonitorTimer.Stop();
                        _temperatureMonitorTimer.Elapsed -= TemperatureMonitorWorker;
                        _temperatureMonitorTimer.Dispose();
                    }
                }

                IsDisposing = false;
                IsDisposed = true;
            }
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
        /// <param name="e">Status of camera when abortion happened</param>
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

        public virtual void TemperatureMonitor(Switch mode, int timeout = TempCheckTimeOutMs)
        {
            CheckIsDisposed();

            // Throws if temperature monitoring is not supported
            if (!Capabilities.GetFunctions.HasFlag(GetFunction.Temperature))
                throw new NotSupportedException("Camera dose not support temperature queries.");

            if (mode == Switch.Enabled)
            {
                if (_temperatureMonitorTimer is null)
                {
                    _temperatureMonitorTimer = new Timer()
                    {
                        Interval = timeout,
                        AutoReset = true,
                        Enabled = false
                    };
                    _temperatureMonitorTimer.Elapsed += TemperatureMonitorWorker;
                    _temperatureMonitorTimer.Start();
                    IsTemperatureMonitored = true;
                }
                else
                {
                    _temperatureMonitorTimer.Stop();
                    _temperatureMonitorTimer.Interval = timeout;
                    _temperatureMonitorTimer.Start();
                }

            }
            else if (mode == Switch.Disabled)
            {
                _temperatureMonitorTimer?.Stop();
                IsTemperatureMonitored = false;
            }

        }

        public abstract SettingsBase GetAcquisitionSettingsTemplate();

        public abstract void EnableAutosave(in string pattern);

        public abstract void StartAcquisition();

        /// <summary>
        /// A synchronous way to manually abort acquisition.
        /// NOTE: if called while async acquisition is in progress, throws
        /// <see cref="TaskCanceledException"/>. To cancel async acquisition, use 
        /// <see cref="CancellationToken"/>.
        /// </summary>
        /// <exception cref="AndorSdkException"/>
        /// <exception cref="TaskCanceledException"/>
        public abstract void AbortAcquisition();

        public abstract Task StartAcquisitionAsync(CancellationTokenSource token, int timeout = StatusCheckTimeOutMs);
        

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
            if (_isDisposing || _isDisposed)
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
