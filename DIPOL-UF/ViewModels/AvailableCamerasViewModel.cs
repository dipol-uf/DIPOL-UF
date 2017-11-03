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
    class AvailableCamerasViewModel : ViewModel<Models.AvailableCamerasModel>
    {
        public AvailableCamerasViewModel(AvailableCamerasModel model) 
            : base(model)
        {
        }
    }
}
