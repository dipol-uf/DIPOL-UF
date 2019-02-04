using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using System.Windows.Markup;
using ANDOR_CS.Classes;
using ANDOR_CS.Enums;
using ANDOR_CS.Events;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
// ReSharper disable UnassignedGetOnlyAutoProperty

namespace DIPOL_UF.ViewModels
{
    internal sealed class MainWindowTreeItemViewModel : ReactiveObjectEx
    {
        private readonly CameraBase _model;

        [Reactive]
        public string Id { get; private set; }

        [Reactive]
        public string Name { get; private set; }

        public float Temperature { [ObservableAsProperty] get; }
        public TemperatureStatus TempStatus { [ObservableAsProperty] get; }
        public bool IsSelected { [ObservableAsProperty] get; }

        public ICommand SelectCommand { get; }

        public MainWindowTreeItemViewModel(string id, CameraBase cam,
            IObservableList<string> selections,
            ICommand selectCommand)
        {
            SelectCommand = selectCommand;
            _model = cam;
            Id = id;
            Name = Converters.ConverterImplementations.CameraToStringAliasConversion(cam);

            HookEvents();


            selections.Connect()
                      .Filter(x => x == Id).AsObservableList()
                      .CountChanged
                      .Select(x => x != 0)
                      .DistinctUntilChanged()
                      .ObserveOnUi()
                      .ToPropertyEx(this, x => x.IsSelected)
                      .DisposeWith(_subscriptions);

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
