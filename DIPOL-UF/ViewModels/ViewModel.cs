using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows;

namespace DIPOL_UF.ViewModels
{
    abstract class ViewModel<T> : ObservableObject, INotifyDataErrorInfo where T : class, INotifyPropertyChanged
    {
        protected enum ErrorPriority : byte
        {
            Low = 0,
            High = 1
        }

        protected Dictionary<string, List<string>> errorCollection
            = new Dictionary<string, List<string>>();
        protected static string[] declaredProperties = null;
        protected T model;


        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        public T Model => model;
        public bool HasErrors => errorCollection.Count > 0;
        public IEnumerable GetErrors(string propertyName)
           => errorCollection.ContainsKey(propertyName)
           ? errorCollection[propertyName]
           : null;

        protected ViewModel(T model)
        {
            this.model = model ?? throw new ArgumentNullException($"Parameter {nameof(model)} is null.");

            if(declaredProperties == null)
                declaredProperties = ListProperties();

            model.PropertyChanged += OnModelPropertyChanged;
        }

        protected string[] ListProperties()
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

        protected virtual void OnErrorChanged(object sender, DataErrorsChangedEventArgs e)
            => ErrorsChanged?.Invoke(sender, e);

        protected virtual void RaiseErrorChanged(
           [System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
           => OnErrorChanged(this, new DataErrorsChangedEventArgs(propertyName));

        protected virtual void AddError(
            string error = "",
            ErrorPriority priority = ErrorPriority.Low,
            [System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
        {
            if (!string.IsNullOrWhiteSpace(propertyName))
            {
                if (!errorCollection.ContainsKey(propertyName))
                    errorCollection[propertyName] = new List<string>();

                if (!errorCollection[propertyName].Contains(error))
                {
                    if (priority == ErrorPriority.Low)
                        errorCollection[propertyName].Add(error);
                    else
                        errorCollection[propertyName].Insert(0, error);
                    RaiseErrorChanged(propertyName);
                }
            }
        }

        protected virtual void RemoveError(
            string error = "",
            [System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
        {
            if (errorCollection.ContainsKey(propertyName) &&
                errorCollection[propertyName].Contains(error))
            {
                errorCollection[propertyName].Remove(error);
                if (errorCollection[propertyName].Count <= 0)
                    errorCollection.Remove(propertyName);

                RaiseErrorChanged(propertyName);
            }
        }

        protected virtual bool IsValid(
            object value,
            [System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
            => true;
    } 
}
