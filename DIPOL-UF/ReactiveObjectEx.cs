//    This file is part of Dipol-3 Camera Manager.

//     MIT License
//     
//     Copyright(c) 2018-2019 Ilia Kosenkov
//     
//     Permission is hereby granted, free of charge, to any person obtaining a copy
//     of this software and associated documentation files (the "Software"), to deal
//     in the Software without restriction, including without limitation the rights
//     to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//     copies of the Software, and to permit persons to whom the Software is
//     furnished to do so, subject to the following conditions:
//     
//     The above copyright notice and this permission notice shall be included in all
//     copies or substantial portions of the Software.
//     
//     THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//     IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//     FITNESS FOR A PARTICULAR PURPOSE AND NONINFINGEMENT. IN NO EVENT SHALL THE
//     AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//     LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//     OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//     SOFTWARE.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ValidationErrorsCache = DynamicData.SourceCache<(string Property, string Type, string Message), (string Property, string Type)>;

namespace DIPOL_UF
{
    public abstract class ReactiveObjectEx : ReactiveObject, INotifyDataErrorInfo, IDisposable
    {
        private readonly ValidationErrorsCache _validationErrors =
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
        public bool HasErrors => _validationErrors?.Items.Any(x => !(x.Message is null)) ?? false;
        public IObservable<bool> ObserveHasErrors { get; private set; }


        protected ReactiveObjectEx()
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
                                 .Select(x => new DataErrorsChangedEventArgs(x.Property));

            WhenErrorsChanged
                .Subscribe(_ => this.RaisePropertyChanged(nameof(HasErrors)))
                .DisposeWith(Subscriptions);

            WhenErrorsChanged
                .Subscribe(OnErrorsChanged)
                .DisposeWith(Subscriptions);

            ObserveHasErrors = WhenErrorsChanged.Select(_ => HasErrors);
       
#if DEBUG
            Helper.WriteLog($"{GetType()}: Created");
#endif
        }
        
        protected void UpdateErrors(string propertyName, string validatorName, string error)
        {
            this.RaisePropertyChanging(nameof(HasErrors));
            
            _validationErrors.Edit(context => context.AddOrUpdate((propertyName, validatorName, null)));
            if(!(error is null))
                _validationErrors.Edit(context => context.AddOrUpdate((propertyName, validatorName, error)));

        }

        protected void BatchUpdateErrors(IEnumerable<(string Property, string Type, string Message)> updates)
        {
            this.RaisePropertyChanging(nameof(HasErrors));
            _validationErrors.Edit(context => context.AddOrUpdate(updates.Select(x => (x.Property, x.Type, (string)null))));
            _validationErrors.Edit(context => context.AddOrUpdate(updates.Where( x=> !(x.Message is null))));
        }
        protected void BatchUpdateErrors(params (string Property, string Type, string Message)[] updates)
        {
            this.RaisePropertyChanging(nameof(HasErrors));
            _validationErrors.Edit(context => context.AddOrUpdate(updates.Select(x => (x.Property, x.Type, (string)null))));
            _validationErrors.Edit(context => context.AddOrUpdate(updates.Where(x => !(x.Message is null))));
        }

        protected void CreateValidator(IObservable<(string Type, string Message)> validationSource, string propertyName)
        {
            validationSource
                .Subscribe(x => UpdateErrors(propertyName, x.Type, x.Message))
                .DisposeWith(Subscriptions);
        }

        protected virtual void OnErrorsChanged(DataErrorsChangedEventArgs e) =>
            ErrorsChanged?.Invoke(this, e);

        protected virtual void HookValidators()
        {
        }

        protected virtual void RemoveAllErrors(string propertyName)
        {
            _validationErrors.Edit(context =>
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

                    _validationErrors.Dispose();

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
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        
    }
}
