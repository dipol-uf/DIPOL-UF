using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using DIPOL_UF.Models;

using ANDOR_CS.Classes;
using DIPOL_UF.Converters;

namespace DIPOL_UF.ViewModels
{
    internal sealed class AvailableCamerasViewModel : ReactiveViewModel<AvailableCamerasModel>
    {
        public ICommand WindowClosingCommand => Model.WindowClosingCommand;
        public ICommand CancelButtonCommand => Model.CancelButtonCommand;
        public ICommand ConnectButtonCommand => Model.ConnectButtonCommand;
        public ICommand ConnectAllButtonCommand => Model.ConnectAllButtonCommand;
        public ICommand WindowContentRenderedCommand => Model.WindowContentRenderedCommand;
        public AvailableCamerasViewModel(AvailableCamerasModel model) 
            : base(model)
        {
          
        }

        public void DebugInvoke(object sender, EventArgs e)
        {
            Helper.WriteLog("Loaded");
        }

        public IEnumerable FoundCamerasEx {
            get
            {
                yield break;
            }
        }

    }
}
