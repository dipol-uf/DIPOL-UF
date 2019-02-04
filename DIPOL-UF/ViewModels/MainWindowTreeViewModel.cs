using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;
using ANDOR_CS.Classes;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI.Fody.Helpers;

namespace DIPOL_UF.ViewModels
{
    internal sealed class MainWindowTreeViewModel : ReactiveObjectEx
    {
        [Reactive]
        // ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
        public string GroupName { get; private set; }
        // ReSharper restore AutoPropertyCanBeMadeGetOnly.Local

        public IObservableCollection<MainWindowTreeItemViewModel> CameraList { get; }
            = new ObservableCollectionExtended<MainWindowTreeItemViewModel>();

        public MainWindowTreeViewModel(
            string name, 
            IConnectableCache<(string Id, CameraBase Camera), string> collection,
            ISourceList<string> selections)
        {
            GroupName = name;
            collection.Connect()
                      .Transform(x => new MainWindowTreeItemViewModel(x.Id, x.Camera, selections))
                      .ObserveOnUi()
                      .Bind(CameraList)
                      .Subscribe()
                      .DisposeWith(_subscriptions);
        }
    }
}
