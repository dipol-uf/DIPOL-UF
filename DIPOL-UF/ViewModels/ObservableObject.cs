﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace DIPOL_UF.ViewModels
{
    class ObservableObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
            => PropertyChanged?.Invoke(sender, e);

        protected virtual void RaisePropertyChanged(
            [System.Runtime.CompilerServices.CallerMemberName]  string propertyName = "")
            => OnPropertyChanged(this, new PropertyChangedEventArgs(propertyName));

    }
}
