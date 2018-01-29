using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;


using ANDOR_CS.Classes;
using ANDOR_CS.Enums;

using DIPOL_UF.Commands;

using static DIPOL_UF.DIPOL_UF_App;

namespace DIPOL_UF.Models
{
    class ConnectedCamera : ObservableObject
    {
        private CameraBase _camera = null;
        private float _targetTemperature = 0.0f;
        private string __targetTemperatureText = "0";
        private bool _canControlTemperature = false;
        private (float, float, float, int) _timing;
        private ObservableConcurrentDictionary<string, bool> enabledControls =
            new ObservableConcurrentDictionary<string, bool>(
                new KeyValuePair<string, bool>[]
                {
                    
                }
                );
        
        private DelegateCommand controlCoolerCommand = null;
        //private DelegateCommand verifyTextInputCommand = null;
        private DelegateCommand setUpAcquisitionCommand = null;

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
        public DelegateCommand ControlCoolerCommand
        {
            get => controlCoolerCommand;
            set
            {
                if (value != controlCoolerCommand)
                {
                    controlCoolerCommand = value;
                    RaisePropertyChanged();
                }
            }
        }
        public DelegateCommand SetUpAcquisitionCommand
        {
            get => setUpAcquisitionCommand;
            set
            {
                if (value != setUpAcquisitionCommand)
                {
                    setUpAcquisitionCommand = value;
                    RaisePropertyChanged();                    
                }
            }

        }

        public ObservableConcurrentDictionary<string, bool> EnabledControls
        {
            get => enabledControls;
            set
            {
                if (value != enabledControls)
                {
                    enabledControls = value;
                    RaisePropertyChanged();
                }
            }
        }

        public ConnectedCamera(CameraBase camera)
        {
            Camera = camera;
            CanControlTemperature = camera.Capabilities.SetFunctions.HasFlag(SetFunction.Temperature);
            InitializeCommands();
        }

        private void InitializeCommands()
        {
            ControlCoolerCommand = new DelegateCommand(
                ControlCoolerCommandExecute,
                (par) => _camera.Capabilities.SetFunctions.HasFlag(SetFunction.Temperature)
                );

            SetUpAcquisitionCommand = new DelegateCommand(
                SetUpAcquisitionCommandExecute,
                DelegateCommand.CanExecuteAlways
                );
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

            if (new Views.AcquisitionSettingsView(viewModel).ShowDialog() == true)
            {
                
            }

            
        }
    }
}
