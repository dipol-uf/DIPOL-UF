using System;
using System.ComponentModel;

using ANDOR_CS.DataStructures;
using ANDOR_CS.Enums;


namespace ANDOR_CS.Classes
{
    public abstract class CameraBase : ANDOR_CS.Interfaces.ICameraControl
    {
        protected bool _IsActive;
        protected bool _IsInitialized;
        protected DeviceCapabilities _Capabilities;
        protected CameraProperties _Properties;
        protected string _SerialNumber;
        protected string _CameraModel;
        protected FanMode _FanMode;
        protected Switch _CoolerMode;
        protected int _CameraIndex;

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


        public event PropertyChangedEventHandler PropertyChanged;

        public abstract CameraStatus GetStatus();

        public abstract void Dispose();


        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string property = "")
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
    }
}
