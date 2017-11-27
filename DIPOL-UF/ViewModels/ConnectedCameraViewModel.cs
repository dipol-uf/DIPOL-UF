using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DIPOL_UF.Models;
using DIPOL_UF.Commands;

using ANDOR_CS.Classes;
using ANDOR_CS.Enums;

namespace DIPOL_UF.ViewModels
{
    class ConnectedCameraViewModel : ViewModel<ConnectedCamera>
    {
        public CameraBase Camera => model.Camera;

        public float MinimumAllowedTemperature => model.Camera.Properties.AllowedTemperatures.Minimum;
        public float MaximumAllowedTemperature => model.Camera.Properties.AllowedTemperatures.Maximum;
        public bool CanQueryTemperature => model.Camera.Capabilities.GetFunctions.HasFlag(GetFunction.Temperature);
        public bool CanControlCooler => model.Camera.Capabilities.SetFunctions.HasFlag(SetFunction.Temperature);

        public float TargetTemperature
        {
            get => model.TargetTemperature;
            set
            {
                if (value != model.TargetTemperature)
                    model.TargetTemperature = value;
            }
        }

        public bool IsCoolerEnabled => model.Camera.CoolerMode == Switch.Enabled;

        public DelegateCommand VerifyTextInputCommand => model.VerifyTextInputCommand;
        public DelegateCommand ControlCoolerCommand => model.ControlCoolerCommand;


        public ConnectedCameraViewModel(ConnectedCamera model) : base(model)
        {
            model.Camera.PropertyChanged += (sender, e) =>
            {
                    if (e.PropertyName == nameof(model.Camera.CoolerMode))
                        RaisePropertyChanged(nameof(IsCoolerEnabled));
            };
        }
    }
}
