using System;
using System.Reactive.Disposables;
using System.Windows.Input;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI.Fody.Helpers;

namespace DIPOL_UF.ViewModels
{
    internal sealed class MenuItemViewModel : ReactiveObjectEx
    {
        [Reactive]
        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local
        public string Name { get; private set; }

        public IObservableCollection<MenuItemViewModel> SubMenu { get; }
            = new ObservableCollectionExtended<MenuItemViewModel>();

        public ICommand Command { get; }

        public MenuItemViewModel(string name, ICommand command,
            IObservableList<MenuItemViewModel> subMenus = null)
        {
            Name = name;
            Command = command;
            subMenus?.Connect().Bind(SubMenu).Subscribe().DisposeWith(Subscriptions);
        }


    }
}
