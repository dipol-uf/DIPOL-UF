using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows;
using DIPOL_UF.Models;

using ANDOR_CS.Classes;

namespace DIPOL_UF.ViewModels
{
    class AvailableCamerasViewModel : ViewModel<AvailableCamerasModel>
    {
        public ObservableConcurrentDictionary<string, CameraBase> FoundCameras => model.FoundCameras;

        public ICommand SelectionChangedCommand => model.SelectionChangedCommand as ICommand;
        public ICommand WindowClosingCommand => model.WindowClosingCommand as ICommand;
        public ICommand CancelButtonCommand => model.CancelButtonCommand as ICommand;
        public ICommand ConnectButtonCommand => model.ConnectButtonCommand as ICommand;
        public ICommand ConnectAllButtonCommand => model.ConnectAllButtonCommand as ICommand;
        public ICommand WindowShownCommand => model.WindowShownCommand as ICommand;

        public AvailableCamerasViewModel(AvailableCamerasModel model) 
            : base(model)
        {
        }
    }
}
