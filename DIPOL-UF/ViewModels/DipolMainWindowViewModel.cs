using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows;
using DIPOL_UF.Models;

namespace DIPOL_UF.ViewModels
{
    class DipolMainWindowViewModel : ViewModel<Models.DipolMainWindow>
    {

        public DipolMainWindowViewModel(DipolMainWindow model) : base(model)
        {
        }

        public ICommand ConnectButtonCommand => model.ConnectButtonCommand as ICommand;
        public ICommand DisconnectButtonCommand => model.DisconnectButtonCommand as ICommand;

        public ObservableCollection<MenuItemViewModel> MenuBarItems 
            => model.MenuBarItems;
            
    }
}
