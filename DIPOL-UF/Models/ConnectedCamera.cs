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
        private CameraBase camera = null;
        private float targetTemperature = 0.0f;
        private string targetTemperatureText = "0";
        private bool canControlTemperature = false;
        private ObservableConcurrentDictionary<string, bool> enabledControls =
            new ObservableConcurrentDictionary<string, bool>(
                new KeyValuePair<string, bool>[]
                {
                    
                }
                );
        
        private DelegateCommand controlCoolerCommand = null;
        private DelegateCommand verifyTextInputCommand = null;
        private DelegateCommand setUpAcquisitionCommand = null;

        public float TargetTemperature
        {
            get => targetTemperature;
            set
            {
                if (Math.Abs(value - targetTemperature) > float.Epsilon)
                {
                    targetTemperature = value;
                    targetTemperatureText = value.ToString("F1");
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(TargetTemperatureText));
                }
            }
        }
        public string TargetTemperatureText
        {
            get => targetTemperatureText;
            set
            {
                if (value != targetTemperatureText)
                {
                    targetTemperatureText = value;
                    targetTemperature = float.Parse(value,
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.NumberFormatInfo.InvariantInfo);
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(TargetTemperature));

                }
            }
        }
        public bool CanControlTemperature
        {
            get => canControlTemperature;
            set
            {
                if (camera.Capabilities.SetFunctions.HasFlag(SetFunction.Temperature) &&
                    value != canControlTemperature)
                {
                    canControlTemperature = value;
                    RaisePropertyChanged();
                }
            }
        }
        public FanMode FanMode
        {
            get => camera.FanMode;
            set
            {
                if (camera.Capabilities.Features.HasFlag(SDKFeatures.FanControl) &&
                    value != FanMode)
                {
                    if (value == FanMode.LowSpeed &&
                        !camera.Capabilities.Features.HasFlag(SDKFeatures.LowFanMode))
                        camera.FanControl(FanMode.LowSpeed);
                    else
                        camera.FanControl(value);

                    RaisePropertyChanged();
                }
            }
        }
        public ShutterMode InternalShutterState
        {
            get => camera.Shutter.Internal;
            set
            {
                if (camera.Capabilities.Features.HasFlag(SDKFeatures.Shutter) &&
                    value != camera.Shutter.Internal)
                {
                    camera.ShutterControl(
                       Settings.GetValueOrNullSafe("ShutterOpenTimeMS", 27),
                       Settings.GetValueOrNullSafe("ShutterCloseTimeMS", 27),
                       value,
                       camera.Shutter.External ?? ShutterMode.PermanentlyOpen,
                       (TTLShutterSignal)Settings.GetValueOrNullSafe("TTLShutterSignal", 1));
                    RaisePropertyChanged();
                }
            }
        }
        public ShutterMode? ExternalShutterState
        {
            get => camera.Shutter.External;
            set
            {
                if (camera.Capabilities.Features.HasFlag(SDKFeatures.ShutterEx) &&
                    value != camera.Shutter.Internal)
                {
                    camera.ShutterControl(
                        Settings.GetValueOrNullSafe("ShutterOpenTimeMS", 27),
                        Settings.GetValueOrNullSafe("ShutterCloseTimeMS", 27), 
                        camera.Shutter.Internal, 
                        value ?? ShutterMode.PermanentlyOpen, 
                        (TTLShutterSignal)Settings.GetValueOrNullSafe("TTLShutterSignal", 1));
                    RaisePropertyChanged();
                }
            }
        }
        public CameraBase Camera
        {
            get => camera;
            private set
            {
                if (value != camera)
                {
                    camera = value;
                    RaisePropertyChanged();
                }
            }
        }
        public DelegateCommand VerifyTextInputCommand
        {
            get => verifyTextInputCommand;
            set
            {
                if (value != verifyTextInputCommand)
                {
                    verifyTextInputCommand = value;
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
            VerifyTextInputCommand = new DelegateCommand(
                VerifyTextInputCommandExecute,
                DelegateCommand.CanExecuteAlways);

            ControlCoolerCommand = new DelegateCommand(
                ControlCoolerCommandExecute,
                (par) => camera.Capabilities.SetFunctions.HasFlag(SetFunction.Temperature)
                );

            SetUpAcquisitionCommand = new DelegateCommand(
                SetUpAcquisitionCommandExecute,
                DelegateCommand.CanExecuteAlways
                );
        }

        private void VerifyTextInputCommandExecute(object parameter)
        {
            if (parameter is CommandEventArgs<TextCompositionEventArgs> txtCompEA)
            {
                txtCompEA.EventArgs.Handled = !Regex.IsMatch(txtCompEA.EventArgs.Text, @"[-.\d]");
            }
            else if (parameter is CommandEventArgs<TextChangedEventArgs> txtChEA)
            {
                if (txtChEA.Sender is TextBox box && !Regex.IsMatch(box.Text, @"-?\d+\.?\d*"))
                {
                    box.Text = "0.0";
                }
            }
        }
        private void ControlCoolerCommandExecute(object parameter)
        {
            if (camera.CoolerMode == Switch.Disabled)
            {
                CanControlTemperature = false;
                camera.SetTemperature((int)targetTemperature);
                camera.CoolerControl(Switch.Enabled);
            }
            else
            {
                camera.CoolerControl(Switch.Disabled);
                CanControlTemperature = true;
            }
        }
        private void SetUpAcquisitionCommandExecute(object parameter)
        {
            var settings = Camera.GetAcquisitionSettingsTemplate();

            var viewModel = new ViewModels.AcquisitionSettingsViewModel(settings, Camera);

            var result = new Views.AcquisitionSettingsView(viewModel).ShowDialog();
        }
    }
}
