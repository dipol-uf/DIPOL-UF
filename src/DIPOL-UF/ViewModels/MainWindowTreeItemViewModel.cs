using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using ANDOR_CS;
using ANDOR_CS.Enums;
using ANDOR_CS.Events;
using DIPOL_UF.Properties;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI.Fody.Helpers;
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
// ReSharper disable UnassignedGetOnlyAutoProperty

namespace DIPOL_UF.ViewModels
{
    internal sealed class MainWindowTreeItemViewModel : ReactiveObjectEx
    {
        private readonly IDevice _model;

        [Reactive]
        public string Id { get; private set; }

        [Reactive]
        public string Name { get; private set; }

        public float Temperature { [ObservableAsProperty] get; }
        public TemperatureStatus TempStatus { [ObservableAsProperty] get; }
        public bool IsSelected { [ObservableAsProperty] get; }
        public FanMode FanMode { [ObservableAsProperty] get; }

        public ICommand SelectCommand { get; }

        public IObservableCollection<MenuItemViewModel> ContextMenu { get; }
            = new ObservableCollectionExtended<MenuItemViewModel>();

        public MainWindowTreeItemViewModel(string id, IDevice cam,
            IObservableList<string> selections,
            ICommand selectCommand,
            ICommand contextMenuCommand)
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
                      .DisposeWith(Subscriptions);

            new[] {new MenuItemViewModel(
                    Localization.Menu_MainWindow_CameraProperties, 
                    contextMenuCommand)}
                .AsObservableChangeSet()
                .DisposeMany()
                .Bind(ContextMenu)
                .Subscribe(new AnonymousObserver<IChangeSet<MenuItemViewModel>>(_ => { }))
                .DisposeWith(Subscriptions);

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
                   .DisposeWith(Subscriptions);

            tempObs.Select(x => x.EventArgs.Status)
                   .ToPropertyEx(this, 
                       x => x.TempStatus,
                       TemperatureStatus.Off)
                   .DisposeWith(Subscriptions);
            
            _model.WhenPropertyChanged(x => x.FanMode).Select(x => x.Value)
                  .ObserveOnUi()
                  .ToPropertyEx(this, 
                      x => x.FanMode, 
                      FanMode.Off)
                  .DisposeWith(Subscriptions);
        }
    }
}
