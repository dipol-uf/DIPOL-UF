using System;
using System.Collections.Generic;
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
        /// Index of VS Speed.
        /// </summary>
        public int VSSpeedIndex
        {
            get => model.VSSpeed?.Index ?? 0;
            set
            {
                model.SetVSSpeed(value);
                RaisePropertyChanged();
            }
        }
        /// <summary>
        /// VS Amplitude.
        /// </summary>
        public VSAmplitude VSAmplitudeValue
        {
            get => model.VSAmplitude ?? VSAmplitude.Normal;
            set
            {
                model.SetVSAmplitude(value);
                RaisePropertyChanged();
            }
        }
        /// <summary>
        /// Analog-Digital COnverter index.
        /// </summary>
        public int ADConverterIndex
        {
            get => model.ADConverter?.Index ?? 0;
            set
            {
                model.SetADConverter(value);
                RaisePropertyChanged();
            }
        }
        public int AmplifierIndex
        {
            get => model.Amplifier?.Index ?? 0;
            set
            {
                model.SetOutputAmplifier(camera.Properties.Amplifiers[value].Amplifier);
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
                { nameof(model.Amplifier), true }
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
                    new KeyValuePair<string, bool>(nameof(model.Amplifier), true)
                }
                );
        }
    }
}
