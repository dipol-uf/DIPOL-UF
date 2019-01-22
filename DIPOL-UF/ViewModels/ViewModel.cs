using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows;

namespace DIPOL_UF.ViewModels
{
    public abstract class ViewModel<T> : ObservableObject, INotifyDataErrorInfo where T : class
    {
        protected enum ErrorPriority : byte
        {
            Low = 0,
            High = 1
        }

        protected Dictionary<string, List<ValidationErrorInstance>> errorCollection
            = new Dictionary<string, List<ValidationErrorInstance>>();
        // ReSharper disable once StaticMemberInGenericType
        // Stores property names of each specialized type
        // It is NOT intended to be shared among all generics of this type
        protected static string[] declaredProperties = null;
        protected T model;

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        public T Model => model;
        public bool HasErrors => errorCollection.Any(item => item.Value.Any());
        public IEnumerable GetErrors(string propertyName)
        {
            if (String.IsNullOrEmpty(propertyName))
                return errorCollection
                    .Select(item => item.Value)
                    .Aggregate(new List<ValidationErrorInstance>(10),
                        (old, item) => { old.AddRange(item); return old; });

            return errorCollection.ContainsKey(propertyName)
                ? errorCollection[propertyName]
                : null;
        }
        public Dictionary<string, string> LatestErrors => errorCollection
            .Select(item => new KeyValuePair<string, string>(item.Key, item.Value.FirstOrDefault()?.Message))
            .ToDictionary(item => item.Key, item => item.Value);
        public Dictionary<string, bool> HasErrorsOfProperties => errorCollection
            .Select(item => new KeyValuePair<string, bool>(item.Key, item.Value.Any()))
            .ToDictionary(item => item.Key, item => item.Value);

        protected ViewModel(T model)
        {
            this.model = model ?? throw new ArgumentNullException($"Parameter {nameof(model)} is null.");

            if(declaredProperties == null)
                declaredProperties = ListProperties();

            foreach (var propName in declaredProperties)
                errorCollection.Add(propName, new List<ValidationErrorInstance>());

            if(model is INotifyPropertyChanged notifiable)
                notifiable.PropertyChanged += OnModelPropertyChanged;

            ErrorsChanged += (sender, e) =>
            {
                RaisePropertyChanged(nameof(LatestErrors));
                RaisePropertyChanged(nameof(HasErrorsOfProperties));
            };
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
                Helper.ExecuteOnUI(() => RaisePropertyChanged(e.PropertyName));
        }

        protected virtual void OnErrorChanged(object sender, DataErrorsChangedEventArgs e)
            => ErrorsChanged?.Invoke(sender, e);

        protected virtual void RaiseErrorChanged(
           [System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
           => OnErrorChanged(this, new DataErrorsChangedEventArgs(propertyName));

        protected virtual void AddError(
            ValidationErrorInstance error,
            ErrorPriority priority = ErrorPriority.Low,
            [System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
        {
            if (!string.IsNullOrWhiteSpace(propertyName))
            {
                if (!errorCollection.ContainsKey(propertyName))
                    errorCollection[propertyName] = new List<ValidationErrorInstance>();

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
            ValidationErrorInstance error,
            [System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
        {
            if (errorCollection.ContainsKey(propertyName) &&
                errorCollection[propertyName].Contains(error))
            {
                errorCollection[propertyName].Remove(error);
                //if (errorCollection[propertyName].Count <= 0)
                //    errorCollection.Remove(propertyName);

                RaiseErrorChanged(propertyName);
            }
        }

        protected virtual bool IsValid(
            object value,
            [System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
            => true;


    } 
}
