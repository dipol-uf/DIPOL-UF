using System;
using System.Windows.Input;
using DIPOL_UF.Models;

using ANDOR_CS.Classes;
using DIPOL_UF.Converters;

namespace DIPOL_UF.ViewModels
{
    class AvailableCamerasViewModel : ViewModel<AvailableCamerasModel>
    {
        //public ObservableConcurrentDictionary<string, string> FoundCamerasEx =>
        //    model.FoundCameras.PropagateCollectionChanges(ConverterImplementations.CameraToStringAliasConversion);

        // ReSharper disable UnusedMember.Global
        //public ICommand SelectionChangedCommand => model.SelectionChangedCommand;
        public ICommand WindowClosingCommand => model.WindowClosingCommand;
        public ICommand CancelButtonCommand => model.CancelButtonCommand;
        public ICommand ConnectButtonCommand => model.ConnectButtonCommand;
        public ICommand ConnectAllButtonCommand => model.ConnectAllButtonCommand;
        public ICommand WindowShownCommand => model.WindowShownCommand;
        // ReSharper restore UnusedMember.Global

        public AvailableCamerasViewModel(AvailableCamerasModel model) 
            : base(model)
        {
          
        }

        
    }
}
