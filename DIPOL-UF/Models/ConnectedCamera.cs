using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;


using ANDOR_CS.Classes;
using ANDOR_CS.Enums;

using DIPOL_UF.Commands;

using static DIPOL_UF.DIPOL_UF_App;
using System.Windows.Threading;

namespace DIPOL_UF.Models
{
    class ConnectedCamera : ObservableObject
    {
        private CameraBase _camera = null;
        private Task _acqTask = null;
        private CancellationTokenSource _acqTaskCancel = null;
        private float _targetTemperature = 0.0f;
        private string __targetTemperatureText = "0";
        private bool _canControlTemperature = false;
        private Tuple<float, float, float, int> _timing;
        private DelegateCommand _controlCoolerCommand = null;
        //private DelegateCommand verifyTextInputCommand = null;
        private DelegateCommand _setUpAcquisitionCommand = null;
        private DelegateCommand _controlAcquisitionCommand = null;
        private int _currentImageIndex = 0;

        public DispatcherTimer ProgBarTimer
        {
            get;
        } = new DispatcherTimer()
        {
            Interval = TimeSpan.FromMilliseconds(100),
            IsEnabled = false
        };
        public int CurrentImageIndex
        {
            get => _currentImageIndex;
            private set
            {
                if (value != _currentImageIndex)
                {
                    _currentImageIndex = value;
                    RaisePropertyChanged();
                }
            }
        }
        public float TargetTemperature
        {
            get => _targetTemperature;
            set
            {
                if (Math.Abs(value - _targetTemperature) > float.Epsilon)
                {
                    _targetTemperature = value;
                    __targetTemperatureText = value.ToString("F1");
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(TargetTemperatureText));
                }
            }
        }
        public string TargetTemperatureText
        {
            get => __targetTemperatureText;
            set
            {
                if (value != __targetTemperatureText)
                {
                    __targetTemperatureText = value;
                    _targetTemperature = float.Parse(value,
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.NumberFormatInfo.InvariantInfo);
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(TargetTemperature));

                }
            }
        }
        public bool CanControlTemperature
        {
            get => _canControlTemperature;
            set
            {
                if (_camera.Capabilities.SetFunctions.HasFlag(SetFunction.Temperature) &&
                    value != _canControlTemperature)
                {
                    _canControlTemperature = value;
                    RaisePropertyChanged();
                }
            }
        }
        public FanMode FanMode
        {
            get => _camera.FanMode;
            set
            {
                if (_camera.Capabilities.Features.HasFlag(SdkFeatures.FanControl) &&
                    value != FanMode)
                {
                    if (value == FanMode.LowSpeed &&
                        !_camera.Capabilities.Features.HasFlag(SdkFeatures.LowFanMode))
                        _camera.FanControl(FanMode.LowSpeed);
                    else
                        _camera.FanControl(value);

                    RaisePropertyChanged();
                }
            }
        }
        public ShutterMode InternalShutterState
        {
            get => _camera.Shutter.Internal;
            set
            {
                if (_camera.Capabilities.Features.HasFlag(SdkFeatures.Shutter) &&
                    value != _camera.Shutter.Internal)
                {
                    _camera.ShutterControl(
                       Settings.GetValueOrNullSafe("ShutterOpenTimeMS", 27),
                       Settings.GetValueOrNullSafe("ShutterCloseTimeMS", 27),
                       value,
                       _camera.Shutter.External ?? ShutterMode.PermanentlyOpen,
                       (TtlShutterSignal)Settings.GetValueOrNullSafe("TTLShutterSignal", 1));
                    RaisePropertyChanged();
                }
            }
        }
        public ShutterMode? ExternalShutterState
        {
            get => _camera.Shutter.External;
            set
            {
                if (_camera.Capabilities.Features.HasFlag(SdkFeatures.ShutterEx) &&
                    value != _camera.Shutter.Internal)
                {
                    _camera.ShutterControl(
                        Settings.GetValueOrNullSafe("ShutterOpenTimeMS", 27),
                        Settings.GetValueOrNullSafe("ShutterCloseTimeMS", 27), 
                        _camera.Shutter.Internal, 
                        value ?? ShutterMode.PermanentlyOpen, 
                        (TtlShutterSignal)Settings.GetValueOrNullSafe("TTLShutterSignal", 1));
                    RaisePropertyChanged();
                }
            }
        }
        public CameraBase Camera
        {
            get => _camera;
            private set
            {
                if (value != _camera)
                {
                    _camera = value;
                    RaisePropertyChanged();
                }
            }
        }
        public Tuple<float, float, float, int> Timing
        {
            get => _timing;
            set
            {
                if (!value.Equals(_timing))
                {
                    _timing = value;
                    RaisePropertyChanged();
                }

            }
        }
        public bool AreSettingsApplied => Camera.CurrentSettings != null;
        public DelegateCommand ControlCoolerCommand
        {
            get => _controlCoolerCommand;
            set
            {
                if (value != _controlCoolerCommand)
                {
                    _controlCoolerCommand = value;
                    RaisePropertyChanged();
                }
            }
        }
        public DelegateCommand SetUpAcquisitionCommand
        {
            get => _setUpAcquisitionCommand;
            set
            {
                if (value != _setUpAcquisitionCommand)
                {
                    _setUpAcquisitionCommand = value;
                    RaisePropertyChanged();                    
                }
            }

        }
        public DelegateCommand ControlAcquisitionCommand
        {
            get => _controlAcquisitionCommand;
            set
            {
                if (value != _controlAcquisitionCommand)
                {
                    _controlAcquisitionCommand = value;
                    RaisePropertyChanged();
                }
            }

        }

        public DipolImagePresenter ImagePresenterModel { get; } = new DipolImagePresenter();

        public ConnectedCamera(CameraBase camera)
        {
            Camera = camera;
            CanControlTemperature = camera.Capabilities.SetFunctions.HasFlag(SetFunction.Temperature);
            InitializeCommands();
            Camera.PropertyChanged += Camera_PropertyCahnged;
            Camera.NewImageReceived += Camera_NewImageReceived;
        }

        private void InitializeCommands()
        {
            ControlCoolerCommand = new DelegateCommand(
                ControlCoolerCommandExecute,
                (par) => Camera.Capabilities.SetFunctions.HasFlag(SetFunction.Temperature) &&
                    !Camera.IsAcquiring
                );

            SetUpAcquisitionCommand = new DelegateCommand(
                SetUpAcquisitionCommandExecute,
                (par) => !Camera.IsAcquiring
                );

            ControlAcquisitionCommand = new DelegateCommand(
                ControlAcquisitionCommandExecute,
                (par) => Camera.CurrentSettings != null);
        }
        private void Camera_PropertyCahnged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Camera.IsAcquiring))
            {
                Helper.ExecuteOnUI(SetUpAcquisitionCommand.OnCanExecuteChanged);
                Helper.ExecuteOnUI(ControlCoolerCommand.OnCanExecuteChanged);
                if (Camera.IsAcquiring)
                {
                    ProgBarTimer.Tag = DateTime.Now;
                    ProgBarTimer.Start();
                }
                else
                    ProgBarTimer.Stop();
            }
        }
        private void ControlCoolerCommandExecute(object parameter)
        {
            if (_camera.CoolerMode == Switch.Disabled)
            {
                CanControlTemperature = false;
                _camera.SetTemperature((int)_targetTemperature);
                _camera.CoolerControl(Switch.Enabled);
            }
            else
            {
                _camera.CoolerControl(Switch.Disabled);
                CanControlTemperature = true;
            }
        }
        private void SetUpAcquisitionCommandExecute(object parameter)
        {
            var settings = Camera.CurrentSettings ?? Camera.GetAcquisitionSettingsTemplate();
            
            var viewModel = new ViewModels.AcquisitionSettingsViewModel(settings, Camera);

            if (new Views.AcquisitionSettingsView(viewModel).ShowDialog() == true && 
                !viewModel.EstimatedTiming.Equals(default((float, float, float, int))))
            {
                Timing = viewModel.EstimatedTiming.ToTuple();
                RaisePropertyChanged(nameof(AreSettingsApplied));
                ControlAcquisitionCommand.OnCanExecuteChanged();
            }
                        
        }
        private void ControlAcquisitionCommandExecute(object parameter)
        {
            if (!Camera.IsAcquiring)
            {
                if (Camera.CurrentSettings == null)
                {
                    MessageBox.Show("Please apply acquisition settings first.", "No settings applied",
                          MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
                    return;
                }

                CurrentImageIndex = 0;
                _acqTaskCancel = new CancellationTokenSource();
                _acqTask = Camera.StartAcquistionAsync(_acqTaskCancel,
                    Math.Max((int)(Camera.CurrentSettings.ExposureTime / 50), 50));
               
            }
            else
            {
                if (!_acqTask.IsCompleted)
                    _acqTaskCancel.Cancel();
            }
        }
        private void Camera_NewImageReceived(object sender, ANDOR_CS.Events.NewImageReceivedEventArgs e)
        {
            CurrentImageIndex = e.First;
            if(Camera.AcquiredImages.TryDequeue(out var im))
                ImagePresenterModel.LoadImage(im);
        }
    }
}
