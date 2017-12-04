using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DIPOL_UF.Models;
using DIPOL_UF.Commands;

using ANDOR_CS.Classes;
using ANDOR_CS.Enums;
using System.ComponentModel;
using System.Collections;
using System.Runtime.CompilerServices;

namespace DIPOL_UF.ViewModels
{
    class ConnectedCameraViewModel : ViewModel<ConnectedCamera>, INotifyDataErrorInfo
    {
        protected Dictionary<string, Func<object, bool>> validators =
            new Dictionary<string, Func<object, bool>>();

        protected Dictionary<string, string> errorMessages = new Dictionary<string, string>()
        {
            { "NotANumber" , "The input contains characters and/or illegal symbols."},
            { "OutOfRange", @"The value is expected to be in [{0}, {1}] range." }
        };

        public CameraBase Camera => model.Camera;
        /// <summary>
        /// Minimum allowed cooling temperature
        /// </summary>
        public float MinimumAllowedTemperature => model.Camera.Properties.AllowedTemperatures.Minimum;
        /// <summary>
        /// Maximum allowed cooling temperature
        /// </summary>
        public float MaximumAllowedTemperature => model.Camera.Properties.AllowedTemperatures.Maximum;
        /// <summary>
        /// Indicates if camera supports temperature queries (gets).
        /// </summary>
        public bool CanQueryTemperature => model.Camera.Capabilities.GetFunctions.HasFlag(GetFunction.Temperature);
        /// <summary>
        /// Indicates if camera supports active cooling (set temperature and cooler control)
        /// </summary>
        public bool CanControlCooler => model.Camera.Capabilities.SetFunctions.HasFlag(SetFunction.Temperature);
        /// <summary>
        /// Indicates if temperature can be controled. False if cooling is on.
        /// </summary>
        public bool CanControlTemperature => model.CanControlTemperature;
        /// <summary>
        /// Indicates if Fan Control is available.
        /// </summary>
        public bool CanControlFan => model.Camera.Capabilities.Features.HasFlag(SDKFeatures.FanControl);
        /// <summary>
        /// The number of allowed regimes: 
        /// Tick spaceing is 2 for On/Off,
        /// or 1 for On/Low/Off.
        /// </summary>
        public int LowFanModeTickStep =>
            model.Camera.Capabilities.Features.HasFlag(SDKFeatures.LowFanMode)
            ? 1
            : 2;
        /// <summary>
        /// A map from <see cref="ANDOR_CS.Enums.FanMode"/> to integer tick position and back.
        /// TwoWay binding, controls fan.
        /// </summary>
        public int FanMode
        {
            get => (int)(2 - (uint)model.FanMode);
            set
            {
                model.FanMode = (FanMode)(2 - value);
            }

        }
        /// <summary>
        /// Target temperature for camera's cooler.
        /// </summary>
        public float TargetTemperature
        {
            get => model.TargetTemperature;
            set
            {
                if (CanControlCooler &&
                    Math.Abs(value - model.TargetTemperature) > float.Epsilon &&
                    value <= MaximumAllowedTemperature &&
                    value >= MinimumAllowedTemperature)
                    model.TargetTemperature = value;
            }
        }
        public string TargetTemperatureText
        {
            get => model.TargetTemperatureText;
            set
            {
                if(IsValid(value))
                {
                    model.TargetTemperatureText = string.IsNullOrWhiteSpace(value) ? "0": value;
                }
            }
        }
        /// <summary>
        /// Indicates if cooler is enabled and therefore temperature cannot be controled.
        /// </summary>
        public bool IsCoolerEnabled => model.Camera.CoolerMode == Switch.Enabled;
        /// <summary>
        /// Indicates if internal shutter can be controlled.
        /// </summary>
        public bool CanControlShutter => model.Camera.Capabilities.Features.HasFlag(SDKFeatures.Shutter);
        /// <summary>
        /// Indicates if internal and external shutters can be controlled separately.
        /// </summary>
        public bool CanControlInternalExternalShutter => model.Camera.Capabilities.Features.HasFlag(SDKFeatures.ShutterEx);

        public ShutterMode InternalShutterState
        {
            get => model.InternalShutterState;
            set => model.InternalShutterState = value;


        }
        public ShutterMode ExternalShutterState
        {
            get => model.ExternalShutterState ?? ShutterMode.PermanentlyOpen;
            set => model.ExternalShutterState = value;

        }

        public ObservableConcurrentDictionary<string, bool> EnabledControls => model.EnabledControls;

        /// <summary>
        /// Command that handles text input and allows only numerics.
        /// </summary>
        public DelegateCommand VerifyTextInputCommand => model.VerifyTextInputCommand;
        /// <summary>
        /// Controls cooler.
        /// </summary>
        public DelegateCommand ControlCoolerCommand => model.ControlCoolerCommand;

        public DelegateCommand SetUpAcquisitionCommand => model.SetUpAcquisitionCommand;

        public ConnectedCameraViewModel(ConnectedCamera model) : base(model)
        {
            InitializeValidators();

            model.Camera.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(model.Camera.CoolerMode))
                    RaisePropertyChanged(nameof(IsCoolerEnabled));
            };
        }

        protected override bool IsValid(object value, [CallerMemberName] string propertyName = "")
        {
            if (validators.ContainsKey(propertyName))
                return validators[propertyName](value);

            return base.IsValid(value, propertyName);
        }

        protected void InitializeValidators()
        {
            validators.Add(nameof(TargetTemperatureText), IsTemperatureTextInRange);
                
        }

        private bool IsTemperatureTextInRange(object value)
        {
            if (value is string s)
            {
                if (string.IsNullOrWhiteSpace(s))
                    return true;
                if (Regex.IsMatch(s, @"^[-+0-9\.]+?$"))
                {
                    RemoveError(
                        new ValidationErrorInstance(
                            "NotANumber", 
                            ""),
                        nameof(TargetTemperatureText));
                    if (float.TryParse(s,
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.NumberFormatInfo.InvariantInfo,
                        out float floatVal) &&
                        (floatVal >= MinimumAllowedTemperature && floatVal <= MaximumAllowedTemperature))
                    {
                        RemoveError(
                            new ValidationErrorInstance(
                                "OutOfRange",                           
                                ""),
                            nameof(TargetTemperatureText));
                        return true;
                    }
                    else
                    {
                        AddError(new ValidationErrorInstance(
                                "OutOfRange",
                                String.Format(errorMessages["OutOfRange"],
                                    MinimumAllowedTemperature,
                                    MaximumAllowedTemperature)),
                                ErrorPriority.High,
                                nameof(TargetTemperatureText));
                        return false;
                    }
                }
                else
                {
                    AddError(
                        new ValidationErrorInstance(
                            "NotANumber",
                            errorMessages["NotANumber"]),
                        ErrorPriority.High,
                        nameof(TargetTemperatureText));
                    return false;
                }
            }
            return true;
        }

    }
}
