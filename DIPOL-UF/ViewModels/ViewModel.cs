using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows;

namespace DIPOL_UF.ViewModels
{
    abstract class ViewModel<T> : ObservableObject where T : ObservableObject
    {
        protected static string[] declaredProperties = null;

        protected T model;
        
        protected ViewModel(T model)
        {
            this.model = model ?? throw new ArgumentNullException($"Parameter {nameof(model)} is null.");

            if(declaredProperties == null)
                declaredProperties = ListProperties();

            model.PropertyChanged += OnModelPropertyChanged;
        }

        private string[] ListProperties()
        {
            var t = GetType();
            var properties = t.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(pi => pi.CanRead);
            return properties.Select(pi => pi.Name).ToArray(); 
        }

        protected virtual void OnModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if(declaredProperties?.Contains(e.PropertyName) ?? true)
                RaisePropertyChanged(e.PropertyName);
        }
    } 
}
