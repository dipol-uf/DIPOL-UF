using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ANDOR_CS.Classes;
using ANDOR_CS.Enums;
using ANDOR_CS.DataStructures;
using ANDOR_CS.Exceptions;

namespace DIPOL_UF.ViewModels
{
    class AcquisitionSettingsViewModel : ObservableObject
    {
        private SettingsBase model;
        private CameraBase camera;

        private Dictionary<string, bool> supportedSettings = null;
        private ObservableConcurrentDictionary<string, bool> allowedSettings = null;

        /// <summary>
        /// Reference to Camera instance.
        /// </summary>
        public CameraBase Camera => camera;
        /// <summary>
        /// Collection of supported by a given Camera settings.
        /// </summary>
        public Dictionary<string, bool> SupportedSettings => supportedSettings;
        /// <summary>
        /// Collection of settings that can be set now.
        /// </summary>
        public ObservableConcurrentDictionary<string, bool> AllowedSettings
        {
            get => allowedSettings;
            set
            {
                if (value != allowedSettings)
                {
                    allowedSettings = value;
                    RaisePropertyChanged();
                }
            }
        }
        /// <summary>
        /// Supported acquisition modes.
        /// </summary>
        public AcquisitionMode[] AllowedAcquisitionModes =>
           Helper.EnumFlagsToArray(Camera.Capabilities.AcquisitionModes)
            .Where(item => item != AcquisitionMode.FrameTransfer)
            .Where(item => ANDOR_CS.Classes.EnumConverter.IsAcquisitionModeSupported(item))            
            .ToArray();

        public (int Index, float Speed)[] AvailableHSSpeeds =>
            model
            .GetAvailableHSSpeeds(ADConverterIndex ?? 0, AmplifierIndex ?? 0)
            .ToArray();
        public (int Index, string Name)[] AvailablePreAmpGains =>
            model
            .GetAvailablePreAmpGain(ADConverterIndex ?? 0, 
                AmplifierIndex ?? 0, HSSpeedIndex?? 0)
            .ToArray();

        /// <summary>
        /// Index of VS Speed.
        /// </summary>
        public int? VSSpeedIndex
        {
            get => model.VSSpeed?.Index;
            set
            {
                model.SetVSSpeed(value ?? 0);
                RaisePropertyChanged();
            }
        }
        /// <summary>
        /// VS Amplitude.
        /// </summary>
        public VSAmplitude? VSAmplitudeValue
        {
            get => model.VSAmplitude;
            set
            {
                model.SetVSAmplitude(value?? VSAmplitude.Normal);
                RaisePropertyChanged();
            }
        }
        /// <summary>
        /// Analog-Digital COnverter index.
        /// </summary>
        public int? ADConverterIndex
        {
            get => model.ADConverter?.Index;
            set
            {
                model.SetADConverter(value ?? 0);
                RaisePropertyChanged();
            }
        }
        /// <summary>
        /// Output Amplifier index.
        /// </summary>
        public int? AmplifierIndex
        {
            get => model.Amplifier?.Index;
            set
            {
                model.SetOutputAmplifier(camera.Properties.Amplifiers[value ?? 0].Amplifier);
                RaisePropertyChanged();
            }
        }
        /// <summary>
        /// HS Speed.
        /// </summary>
        public int? HSSpeedIndex
        {
            get => model.HSSpeed?.Index;
            set
            {
                if (value >= 0)
                {
                    model.SetHSSpeed(value.Value);
                    RaisePropertyChanged();
                }                
            }
        }
        /// <summary>
        /// Index of Pre Amplifier Gain.
        /// </summary>
        public int? PreAmpGainIndex
        {
            get => model.PreAmpGain?.Index;
            set
            {
                if (value >= 0)
                {
                    model.SetPreAmpGain(value.Value);
                    RaisePropertyChanged();
                }
            }
        }

        public AcquisitionMode? AcquisitionModeValue
        {
            get => model.AcquisitionMode;
            set
            {
                model.SetAcquisitionMode(value ?? AcquisitionMode.SingleScan);
                RaisePropertyChanged();
            }
        }

        public AcquisitionSettingsViewModel(SettingsBase model, CameraBase camera) 
        {
            this.model = model;
            this.camera = camera;

            CheckSupportedFeatures();
            InitializeAllowedSettings();

        }


        private void CheckSupportedFeatures()
        {
            supportedSettings = new Dictionary<string, bool>()
            {
                { nameof(model.VSSpeed), camera.Capabilities.SetFunctions.HasFlag(SetFunction.VerticalReadoutSpeed)},
                { nameof(model.VSAmplitude), camera.Capabilities.SetFunctions.HasFlag(SetFunction.VerticalClockVoltage) },
                { nameof(model.ADConverter), true },
                { nameof(model.Amplifier), true },
                { nameof(model.HSSpeed), camera.Capabilities.SetFunctions.HasFlag(SetFunction.HorizontalReadoutSpeed) },
                { nameof(model.PreAmpGain), camera.Capabilities.SetFunctions.HasFlag(SetFunction.PreAmpGain) },
                { nameof(model.AcquisitionMode), true }
            };
        }

        private void InitializeAllowedSettings()
        {
            AllowedSettings = new ObservableConcurrentDictionary<string, bool>(
                new KeyValuePair<string, bool>[]
                {
                    new KeyValuePair<string, bool>(nameof(model.VSSpeed), true),
                    new KeyValuePair<string, bool>(nameof(model.VSAmplitude), true),
                    new KeyValuePair<string, bool>(nameof(model.ADConverter), true),
                    new KeyValuePair<string, bool>(nameof(model.Amplifier), true),
                    new KeyValuePair<string, bool>(nameof(model.HSSpeed), 
                        ADConverterIndex.HasValue 
                        && AmplifierIndex.HasValue),
                    new KeyValuePair<string, bool>(nameof(model.PreAmpGain),
                        ADConverterIndex.HasValue 
                        && AmplifierIndex.HasValue 
                        && HSSpeedIndex.HasValue),
                    new KeyValuePair<string, bool>(nameof(model.AcquisitionMode), true)
                }
                );
        }

        protected override void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(sender, e);

            if ((e.PropertyName == nameof(AmplifierIndex) || 
                 e.PropertyName == nameof(ADConverterIndex)) &&
                (AllowedSettings[nameof(model.HSSpeed)] 
                    = ADConverterIndex.HasValue && 
                      AmplifierIndex.HasValue))
            {
                RaisePropertyChanged(nameof(PreAmpGainIndex));
                RaisePropertyChanged(nameof(HSSpeedIndex));
                RaisePropertyChanged(nameof(AvailableHSSpeeds));
            }
            if ((e.PropertyName == nameof(AmplifierIndex) ||
                 e.PropertyName == nameof(ADConverterIndex) ||
                 e.PropertyName == nameof(HSSpeedIndex)) &&
                (AllowedSettings[nameof(model.PreAmpGain)]
                    = AmplifierIndex.HasValue &&
                      ADConverterIndex.HasValue &&
                      HSSpeedIndex.HasValue))
            {
                RaisePropertyChanged(nameof(PreAmpGainIndex));
                RaisePropertyChanged(nameof(AvailablePreAmpGains));
            }
            //if (e.PropertyName == nameof(AvailableHSSpeeds))
            //{
            //    RaisePropertyChanged(nameof(HSSpeedIndex));
            //    RaisePropertyChanged(nameof(PreAmpGainIndex));
            //}
            //if (e.PropertyName == nameof(AvailablePreAmpGains))
            //    RaisePropertyChanged(nameof(PreAmpGainIndex));
        }

       
    }
}
