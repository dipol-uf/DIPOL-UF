using System;
using System.Collections;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using DIPOL_UF.Models;

using ANDOR_CS.Classes;
using DIPOL_UF.Converters;
using DynamicData;
using DynamicData.Alias;
using DynamicData.Binding;

namespace DIPOL_UF.ViewModels
{
    internal sealed class AvailableCamerasViewModel : ReactiveViewModel<AvailableCamerasModel>
    {
        public IObservableCollection<string> FoundCameras { get; }
            = new ObservableCollectionExtended<string>();

        public ICommand WindowClosingCommand => Model.WindowClosingCommand;
        public ICommand CancelButtonCommand => Model.CancelButtonCommand;
        public ICommand ConnectButtonCommand => Model.ConnectButtonCommand;
        public ICommand ConnectAllButtonCommand => Model.ConnectAllButtonCommand;
        public ICommand WindowContentRenderedCommand => Model.WindowContentRenderedCommand;
        
        public AvailableCamerasViewModel(AvailableCamerasModel model) 
            : base(model)
        {
          HookValidators();
          HookObservables();
        }

        private void HookObservables()
        {
            Model.FoundCameras.Connect().Select(x => x.Id)
                 .ObserveOnUi()
                 .Bind(FoundCameras)
                 .Subscribe()
                 .DisposeWith(_subscriptions);
        }

        
    }
}
