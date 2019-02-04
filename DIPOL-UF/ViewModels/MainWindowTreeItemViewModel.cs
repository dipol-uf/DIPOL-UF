using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Markup;
using ANDOR_CS.Classes;
using ANDOR_CS.Enums;
using ANDOR_CS.Events;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI.Fody.Helpers;

namespace DIPOL_UF.ViewModels
{
    internal sealed class MainWindowTreeItemViewModel : ReactiveObjectEx
    {
        private readonly CameraBase _model;
        [Reactive]
        // ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
        public string Id { get; private set; }

        [Reactive]
        public string Name { get; private set; }
        // ReSharper restore AutoPropertyCanBeMadeGetOnly.Local

        // ReSharper disable UnassignedGetOnlyAutoProperty
        public float Temperature { [ObservableAsProperty] get; }
        public TemperatureStatus TempStatus { [ObservableAsProperty] get; }
        // ReSharper restore UnassignedGetOnlyAutoProperty

        public MainWindowTreeItemViewModel(string id, CameraBase cam,
            ISourceList<string> selections)
        {
            _model = cam;
            Id = id;
            Name = Converters.ConverterImplementations.CameraToStringAliasConversion(cam);

            HookEvents();

            
        }

       

        private void HookEvents()
        {
            var tempObs =
                Observable.FromEventPattern<TemperatureStatusEventHandler, TemperatureStatusEventArgs>(
                              x => _model.TemperatureStatusChecked += x,
                              x => _model.TemperatureStatusChecked -= x)
                          .ObserveOnUi();

            tempObs.Select(x => x.EventArgs.Temperature)
                   .ToPropertyEx(this, x => x.Temperature)
                   .DisposeWith(_subscriptions);

            tempObs.Select(x => x.EventArgs.Status)
                   .ToPropertyEx(this, 
                       x => x.TempStatus,
                       TemperatureStatus.Off)
                   .DisposeWith(_subscriptions);

        }
    }
}
