using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows;

namespace DIPOL_UF.Models
{
    class MenuItemModel : ObservableObject
    {
        private Commands.DelegateCommand command;
        private string header;
        private ObservableCollection<ViewModels.MenuItemViewModel> menuItems;

        public string Header
        {
            get => header;
            set
            {
                if (value != header)
                {
                    header = value;
                    RaisePropertyChanged();
                }

            }
        }

        public Commands.DelegateCommand Command
        {
            get => command;
            set
            {
                if (value != command)
                {
                    command = value;
                    RaisePropertyChanged();
                }
            }
        }

        public ObservableCollection<ViewModels.MenuItemViewModel> MenuItems
        {
            get => menuItems;
            set
            {
                if (value != menuItems)
                {
                    menuItems = value;
                    RaisePropertyChanged();
                    menuItems.CollectionChanged += (sender, e) => RaisePropertyChanged(nameof(MenuItems));
                }
            }
        }

        public MenuItemModel()
        {
            //menuItems.CollectionChanged += (sender, e) => RaisePropertyChanged(nameof(MenuItems));
        }

    }
}
