using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using DynamicData.Kernel;
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

        public bool IsDisposing { get; private set; } = false;
        public bool IsDisposed { get; private set; } = false;
        public bool HasErrors => _observableErrors.KeyValues.Any(x => x.Value.Errors.KeyValues.Any());

        private void UpdateErrors(string error, string propertyName, string validatorName)
        {
            this.RaisePropertyChanging(nameof(HasErrors));

            _observableErrors.Edit(global =>
            {
                var globalCollection = global.Lookup(propertyName);

                var value = globalCollection.Value;
                if (!globalCollection.HasValue)
                {
                    value = (Property: propertyName, Errors: new PropertyErrorCache(x => x.ErrorType));
                    value.Errors.Connect()
                         .Subscribe(_ => OnErrorsChanged(new DataErrorsChangedEventArgs(propertyName)))
                         .AddTo(_subscriptions);
                }

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


        protected virtual void OnErrorsChanged(DataErrorsChangedEventArgs e) =>
            ErrorsChanged?.Invoke(this, e);

        protected virtual void HookValidators()
        {
            _observableErrors.Connect()
                             .Subscribe(_ => this.RaisePropertyChanged(nameof(HasErrors)))
                             .AddTo(_subscriptions);
        }

        public IEnumerable GetErrors(string propertyName)
        {
            var result = _observableErrors.Lookup(propertyName);
            return result.HasValue ? result.Value.Errors.Items.Select(x => x.Message).ToList() : null;
        }

        public void Dispose()
        {
            try
            {
                IsDisposing = true;
                foreach (var sub in _subscriptions)
                    sub.Dispose();
                _observableErrors?.Dispose();
                IsDisposed = true;
            }
            finally
            {
                IsDisposing = false;
            }
        }

    }
}
