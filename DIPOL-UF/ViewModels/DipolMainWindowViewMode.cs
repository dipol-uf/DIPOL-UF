using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows;

namespace DIPOL_UF.ViewModels
{
    class DipolMainWindowViewMode : ObservableObject
    {
        private Models.DipolMainWindow model;

        public ICommand ConnectButtonCommand => model.ConnectButtonCommand as ICommand;
        public ICommand DisconnectButtonCommand => model.DisconnectButtonCommand as ICommand;

        public ObservableCollection<MenuItemViewModel> MenuBarItems 
            => model.MenuBarItems;


        public DipolMainWindowViewMode(Models.DipolMainWindow model)
        {
            this.model = model ?? throw new ArgumentNullException();

            model.PropertyChanged += ModelPropertyChanged;
        }

        protected virtual void ModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            RaisePropertyChanged(e.PropertyName);
        }
    }
}
