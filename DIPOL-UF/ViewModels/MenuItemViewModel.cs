using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows;

namespace DIPOL_UF.ViewModels
{
    class MenuItemViewModel : ViewModel<Models.MenuItemModel>
    {

        public MenuItemViewModel(Models.MenuItemModel model)
            :base(model)
        {
        }

        public string Header => model.Header;
        public ICommand Command => model.Command as ICommand;
        public ObservableCollection<MenuItemViewModel> MenuItems => model.MenuItems;
                
    }
}
