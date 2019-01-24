﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using ReactiveUI;

using ValidationErrorsCache = DynamicData.SourceCache<(string Property, string Type, string Message), (string Property, string Type)>;

namespace DIPOL_UF
{
    public abstract class ReactiveObjectEx : ReactiveObject, INotifyDataErrorInfo, IDisposable
    {
        private readonly ValidationErrorsCache _validationErrors =
            new ValidationErrorsCache(x => (x.Property, x.Type));

        protected readonly List<IDisposable> _subscriptions = new List<IDisposable>();

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        internal IObservable<(string Property, string Type, string Message)> WhenErrorsChangedTyped
        {
            get;
            private set;
        }

        public IObservable<DataErrorsChangedEventArgs> WhenErrorsChanged { get; private set; }
        public bool IsDisposed { get; private set; }
        public bool HasErrors => _validationErrors.Items.Any(x => !(x.Message is null));

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
                .AddTo(_subscriptions);
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

            WhenErrorsChanged.Subscribe(_ => this.RaisePropertyChanged(nameof(HasErrors)))
                                     .AddTo(_subscriptions);

            WhenErrorsChanged.Subscribe(OnErrorsChanged).AddTo(_subscriptions);


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
                                    .Where(x => x.Property == propertyName)
                                    .Select(x => x.Message);
        }
        
        public virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    foreach (var sub in _subscriptions)
                        sub.Dispose();

                    _validationErrors.Dispose();
                }

                IsDisposed = true;
            }
        }

        public void Dispose()
            => Dispose(true);
    }
}
