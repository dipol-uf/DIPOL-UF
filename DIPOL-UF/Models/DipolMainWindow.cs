using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using ANDOR_CS.Classes;

namespace DIPOL_UF.Models
{
    class DipolMainWindow : ObservableObject
    {

        private ObservableCollection<ViewModels.MenuItemViewModel> menuBarItems
            = new ObservableCollection<ViewModels.MenuItemViewModel>()
        {
            new ViewModels.MenuItemViewModel(new MenuItemModel()
                {
                 Header = "_Camera",
                 MenuItems = new ObservableCollection<ViewModels.MenuItemViewModel>()
                 {
                     new ViewModels.MenuItemViewModel(new MenuItemModel()
                     {
                         Header = "Find Available",
                         Command = new Commands.DelegateCommand(
                             (x) => ListCameras(), 
                             (x) => CamerasAvailable()),

                     })
                 }
            })
        };

        private ConcurrentDictionary<string, CameraBase> connectedCameras;



        public ObservableCollection<ViewModels.MenuItemViewModel> MenuBarItems => menuBarItems;





        private static void ListCameras()
        {

        }

        private static bool CamerasAvailable()
        {
            try
            {
                return Camera.GetNumberOfCameras() > 0;
            }
            catch (ANDOR_CS.Exceptions.AndorSDKException aEx)
            {
                return false;
            }

        }


    }
}
