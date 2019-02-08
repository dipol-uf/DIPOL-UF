using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI;

using ValidationErrorsCache = DynamicData.SourceCache<(string Property, string Type, string Message), (string Property, string Type)>;

namespace DIPOL_UF
{
    public abstract class ReactiveObjectEx : ReactiveObject, INotifyDataErrorInfo, IDisposable
    {
        private readonly ValidationErrorsCache _validationErrors =
            new ValidationErrorsCache(x => (x.Property, x.Type));

        protected  readonly  CompositeDisposable _subscriptions = new CompositeDisposable();

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        internal IObservable<(string Property, string Type, string Message)> WhenErrorsChangedTyped
        {
            get;
            private set;
        }

        public IObservable<DataErrorsChangedEventArgs> WhenErrorsChanged { get; private set; }
        public bool IsDisposed { get; private set; }
        public bool HasErrors => _validationErrors?.Items.Any(x => !(x.Message is null)) ?? false;

        private void UpdateErrors(string error, string propertyName, string validatorName)
        {
            this.RaisePropertyChanging(nameof(HasErrors));
            
            _validationErrors.Edit(context =>
            {
                context.AddOrUpdate((propertyName, validatorName, error));
            });

        }
        
        protected void CreateValidator(IObservable<(string Type, string Message)> validationSource, string propertyName)
        {
            validationSource
                .Subscribe(x => UpdateErrors(x.Message, propertyName, x.Type))
                .DisposeWith(_subscriptions);
        }

        protected virtual void OnErrorsChanged(DataErrorsChangedEventArgs e) =>
            ErrorsChanged?.Invoke(this, e);

        protected virtual void HookValidators()
        {

            WhenErrorsChangedTyped =
                _validationErrors.Connect()
                                 .Select(x => Observable.For(x, y => Observable.Return(y.Current)))
                                 .Merge()
                                 .DistinctUntilChanged();

            WhenErrorsChanged =
                _validationErrors.Connect()
                                 .Select(x =>
                                     x.Select(y => (y.Current.Property, y.Current.Message)).ToList())
                                 .Select(x => Observable.For(x, Observable.Return))
                                 .Merge()
                                 .DistinctUntilChanged()
                                 .Select(x => new DataErrorsChangedEventArgs(x.Property));

            WhenErrorsChanged
                .Subscribe(_ => this.RaisePropertyChanged(nameof(HasErrors)))
                .DisposeWith(_subscriptions);

            WhenErrorsChanged
                .Subscribe(OnErrorsChanged)
                .DisposeWith(_subscriptions);
            
        }

        public virtual List<(string Type, string Message)> GetTypedErrors(string propertyName)
        {
            return _validationErrors.Items
                                    .Where(x => x.Property == propertyName)
                                    .Select(x => (x.Type, x.Message))
                                    .ToList();
        }

        public virtual IEnumerable GetErrors(string propertyName)
        {
            return _validationErrors.Items
                                    .Where(x => x.Property == propertyName && !(x.Message is null))
                                    .Select(x => x.Message);
        }

        public virtual bool HasSpecificErrors(string propertyName)
            => _validationErrors?.Items.Any(x => x.Property == propertyName && !(x.Message is null)) ?? false;

        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    if(!_subscriptions.IsDisposed)
                        _subscriptions.Dispose();

                    _validationErrors.Dispose();

#if DEBUG
                    Helper.WriteLog($"{GetType()}: Disposed");
#endif
                }

                IsDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }



#if DEBUG
        internal ReactiveObjectEx()
        {
            Helper.WriteLog($"{GetType()}: Created");

            Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(
                          x => PropertyChanged += x, x => PropertyChanged -= x)
                      .Subscribe(x => Helper.WriteLog($"{GetType()}: {x.EventArgs.PropertyName}"))
                      .DisposeWith(_subscriptions);
        }
#endif
    }
}
