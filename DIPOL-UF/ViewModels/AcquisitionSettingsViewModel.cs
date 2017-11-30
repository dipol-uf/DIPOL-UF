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

        public CameraBase Camera => camera;
        public Dictionary<string, bool> SupportedSettings => supportedSettings;
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

        public int VSSpeedIndex
        {
            get => model.VSSpeed?.Index ?? 0;
            set
            {
                model.SetVSSpeed(value);
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
                {nameof(model.VSAmplitude), camera.Capabilities.SetFunctions.HasFlag(SetFunction.VerticalClockVoltage) }
            };
        }

        private void InitializeAllowedSettings()
        {
            AllowedSettings = new ObservableConcurrentDictionary<string, bool>(
                new KeyValuePair<string, bool>[]
                {
                    new KeyValuePair<string, bool>(nameof(model.VSSpeed), true),
                    new KeyValuePair<string, bool>(nameof(model.VSAmplitude), true)
                }
                );
        }
    }
}
