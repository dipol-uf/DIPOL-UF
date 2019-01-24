using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Subjects;
using DIPOL_UF.Models;
using DIPOL_UF.ViewModels;
using DynamicData.Alias;
using ReactiveUI;

using PropertyErrorCache = DynamicData.SourceCache<(string ErrorType, string Message), string>;
using GlobalErrorCache = DynamicData.SourceCache<(string Property, DynamicData.SourceCache<(string ErrorType, string Message), string> Errors), string>;

namespace DIPOL_UF
{
    public abstract class ReactiveObjectEx : ReactiveObject, INotifyDataErrorInfo, IDisposable
    {
        private readonly GlobalErrorCache _observableErrors =
            new GlobalErrorCache(x => x.Property);
        protected readonly List<IDisposable> _subscriptions = new List<IDisposable>();

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        public bool IsDisposed { get; private set; }
        public bool HasErrors => _observableErrors.KeyValues.Any(x => x.Value.Errors.KeyValues.Any());

        private void UpdateErrors(string error, string propertyName, string validatorName)
        {
            this.RaisePropertyChanging(nameof(HasErrors));

            _observableErrors.Edit(global =>
            {
                var globalCollection = global.Lookup(propertyName);

                // TODO: Localize exception
                if(!globalCollection.HasValue)
                    throw new InvalidOperationException("Poorly attached validator");

                var value = globalCollection.Value;

                value.Errors.Edit(local =>
                {
                    if (error is null)
                        local.Remove(validatorName);
                    else
                        local.AddOrUpdate((validatorName, error));
                });
                global.AddOrUpdate(value);

            });
        }
        
        protected void CreateValidator(IObservable<string> validationSource, string propertyName, string validatorName)
        {
            if (!_observableErrors.Keys.Contains(propertyName))
            {
                var value = (Property: propertyName, Errors: new PropertyErrorCache(x => x.ErrorType));
               _observableErrors.Edit(global =>
               {
                   global.AddOrUpdate(value);
               });
            }
            validationSource
                .Subscribe(x => UpdateErrors(x, propertyName, validatorName))
                .AddTo(_subscriptions);
        }

        protected virtual void OnErrorsChanged(DataErrorsChangedEventArgs e) =>
            ErrorsChanged?.Invoke(this, e);

        protected virtual void HookValidators()
        {
            _observableErrors.Connect()
                             .Subscribe(_ => this.RaisePropertyChanged(nameof(HasErrors)))
                             .AddTo(_subscriptions);
        }

        public virtual List<(string ErrorType, string Message)> GetTypedErrors(string propertyName)
        {
            var result = _observableErrors.Lookup(propertyName);
            return result.HasValue ? result.Value.Errors.Items.ToList() : null;
        }

        public virtual IEnumerable GetErrors(string propertyName) {
            var result = _observableErrors.Lookup(propertyName);
            return result.HasValue ? result.Value.Errors.Items.Select(x => x.Message).ToList() : null;
        }
        
        public virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    foreach (var sub in _subscriptions)
                        sub.Dispose();
                    foreach(var (_, error) in _observableErrors.Items)
                        error.Dispose();
                    _observableErrors.Dispose();
                }

                IsDisposed = true;
            }
        }

        public void Dispose()
            => Dispose(true);
    }
}
