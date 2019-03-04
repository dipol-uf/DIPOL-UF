using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ValidationErrorsCache = DynamicData.SourceCache<(string Property, string Type, string Message), (string Property, string Type)>;

namespace DIPOL_UF
{
    public abstract class ReactiveObjectEx : ReactiveObject, INotifyDataErrorInfo, IDisposable
    {
        internal readonly ValidationErrorsCache ValidationErrors =
            new ValidationErrorsCache(x => (x.Property, x.Type));

        protected  readonly  CompositeDisposable Subscriptions = new CompositeDisposable();

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        internal IObservable<(string Property, string Type, string Message)> WhenErrorsChangedTyped
        {
            get;
            private set;
        }

        public IObservable<DataErrorsChangedEventArgs> WhenErrorsChanged { get; private set; }
        public bool IsDisposed { get; private set; }
        public bool HasErrors => ValidationErrors?.Items.Any(x => !(x.Message is null)) ?? false;
        public IObservable<bool> ObserveHasErrors { get; private set; }

        internal ReactiveObjectEx()
        {
        
#if DEBUG
            Helper.WriteLog($"{GetType()}: Created");
#endif
        }
        
        protected void UpdateErrors(string error, string propertyName, string validatorName)
        {
            this.RaisePropertyChanging(nameof(HasErrors));
            
            ValidationErrors.Edit(context =>
            {
                context.AddOrUpdate((propertyName, validatorName, error));
            });

        }
        
        protected void CreateValidator(IObservable<(string Type, string Message)> validationSource, string propertyName)
        {
            validationSource
                .Subscribe(x => UpdateErrors(x.Message, propertyName, x.Type))
                .DisposeWith(Subscriptions);
        }

        protected virtual void OnErrorsChanged(DataErrorsChangedEventArgs e) =>
            ErrorsChanged?.Invoke(this, e);

        protected virtual void HookValidators()
        {
            WhenErrorsChangedTyped =
                ValidationErrors.Connect()
                                 .Select(x => Observable.For(x, y => Observable.Return(y.Current)))
                                 .Merge()
                                 .DistinctUntilChanged();

            WhenErrorsChanged =
                ValidationErrors.Connect()
                                 .Select(x =>
                                     x.Select(y => (y.Current.Property, y.Current.Message)).ToList())
                                 .Select(x => Observable.For(x, Observable.Return))
                                 .Merge()
                                 .LogObservable("WHENERRORSCHANGED", Subscriptions)
                                 //.DistinctUntilChanged()
                                 .Select(x => new DataErrorsChangedEventArgs(x.Property));

            WhenErrorsChanged
                .Subscribe(_ => this.RaisePropertyChanged(nameof(HasErrors)))
                .DisposeWith(Subscriptions);

            WhenErrorsChanged
                .Subscribe(OnErrorsChanged)
                .DisposeWith(Subscriptions);

            ObserveHasErrors = WhenErrorsChanged.Select(_ => HasErrors);

        }

        protected virtual void RemoveAllErrors(string propertyName)
        {
            ValidationErrors.Edit(context =>
            {
                var items = context.Items.Where(x => x.Property == propertyName)
                                   .Select(x => (x.Property, x.Type, Message: (string)null))
                                   .ToList();
                context.AddOrUpdate(items);
            });
        }

        protected virtual void BindTo<TSource, TTarget, TProperty>(
            TSource source, Expression<Func<TSource, TProperty>> srcSelector,
            TTarget target, Expression<Func<TTarget, TProperty>> trgtSelector)
            where TSource : ReactiveObjectEx
            where TTarget : ReactiveObjectEx
            => source.WhenPropertyChanged(srcSelector)
                     .Select(x => x.Value)
                     .BindTo(target, trgtSelector)
                     .DisposeWith(Subscriptions);

        protected virtual void ToPropertyEx<TSource, TTarget, TProperty>(
            TSource source, Expression<Func<TSource, TProperty>> srcSelector,
            TTarget target, Expression<Func<TTarget, TProperty>> trgtSelector,
            TProperty initialValue = default)
            where TSource : ReactiveObjectEx
            where TTarget : ReactiveObjectEx
            => source.WhenPropertyChanged(srcSelector)
                     .Select(x => x.Value)
                     .ToPropertyEx(target, trgtSelector, initialValue)
                     .DisposeWith(Subscriptions);

        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    if (!Subscriptions.IsDisposed)
                        Subscriptions.Dispose();

                    ValidationErrors.Dispose();

#if DEBUG
                    Helper.WriteLog($"{GetType()}: Disposed");
#endif
                }

                IsDisposed = true;
            }
        }

        public virtual IObservable<bool> ObserveSpecificErrors(string propertyName)
            => WhenErrorsChangedTyped.Where(x => x.Property == propertyName)
                                     .Select(_ => HasSpecificErrors(propertyName));

        public virtual List<(string Type, string Message)> GetTypedErrors(string propertyName)
        {
            return ValidationErrors.Items
                                    .Where(x => x.Property == propertyName)
                                    .Select(x => (x.Type, x.Message))
                                    .ToList();
        }

        public virtual IEnumerable GetErrors(string propertyName)
        {
            return ValidationErrors.Items
                                    .Where(x => x.Property == propertyName && !(x.Message is null))
                                    .Select(x => x.Message);
        }

        public virtual bool HasSpecificErrors(string propertyName)
            => ValidationErrors?.Items.Any(x => x.Property == propertyName && !(x.Message is null)) ?? false;
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        
    }
}
