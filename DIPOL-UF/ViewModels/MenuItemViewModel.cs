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
    class MenuItemViewModel : ObservableObject
    {
        private Models.MenuItemModel model;

        public MenuItemViewModel(Models.MenuItemModel model)
        {
            this.model = model ?? throw new ArgumentNullException();
        }

        public string Header => model.Header;
        public ICommand Command => model.Command as ICommand;
        public ObservableCollection<MenuItemViewModel> MenuItems => model.MenuItems;

        protected virtual void ModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            RaisePropertyChanged(e.PropertyName);
        }
    }
}
