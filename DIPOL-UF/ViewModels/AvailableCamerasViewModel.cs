using System;
using System.Collections;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using DIPOL_UF.Models;
using DIPOL_UF.Converters;
using DynamicData;
using DynamicData.Alias;
using DynamicData.Binding;
using ReactiveUI;

namespace DIPOL_UF.ViewModels
{
    internal sealed class AvailableCamerasViewModel : ReactiveViewModel<AvailableCamerasModel>
    {
        public IObservableCollection<Tuple<string, string, string>> ListedCameras { get; }
            = new ObservableCollectionExtended<Tuple<string, string, string>>();


        public ICommand CancelButtonCommand => Model.CancelButtonCommand;
        public ICommand ConnectButtonCommand => Model.ConnectButtonCommand;
        public ICommand ConnectAllButtonCommand => Model.ConnectAllButtonCommand;
        public ICommand WindowContentRenderedCommand => Model.WindowContentRenderedCommand;
        public ICommand CloseCrossCommand => Model.CloseCrossCommand;
        public ICommand ClickCommand => Model.ClickCommand;

        public ICommand SelectionChangedCommand { get; private set; }

        public AvailableCamerasViewModel(AvailableCamerasModel model)
            : base(model)
        {

            HookValidators();
            HookCommands();
            HookObservables();
        }

        private void HookObservables()
        {
            var observer = Model.FoundCameras.Connect();
            observer.Select(x => new Tuple<string, string, string>(
                        ConverterImplementations.CameraKeyToHostConversion(x.Id),
                        ConverterImplementations.CameraToStringAliasConversion(x.Camera),
                        x.Id))
                    .Sort(SortExpressionComparer<Tuple<string, string, string>>
                          .Ascending(x => x.Item1).ThenByAscending(x => x.Item2))
                    .ObserveOnUi()
                    .Bind(ListedCameras)
                    .DisposeMany()
                    .Subscribe()
                    .DisposeWith(_subscriptions);


            (SelectionChangedCommand as ReactiveCommand<IList, IList>)
                ?.ObserveOnUi()
                .Select(x => x.Cast<Tuple<string, string, string>>().Select(y => y.Item3).ToList())
                .Subscribe(x =>
                {
                    Model.SelectedIds.Edit(context =>
                    {
                        context.Clear();
                        context.AddRange(x);
                    });
                })
                .DisposeWith(_subscriptions);
        }

        private void HookCommands()
        {
            SelectionChangedCommand =
                ReactiveCommand.Create<IList, IList>(x => x)
                               .DisposeWith(_subscriptions);
        }

    }
}
