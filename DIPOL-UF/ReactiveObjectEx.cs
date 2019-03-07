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
using System.Net.Sockets;
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
        //private readonly ValidationErrorsCache _validationErrors =
        //    new ValidationErrorsCache(x => (x.Property, x.Type));

        protected  readonly  CompositeDisposable Subscriptions = new CompositeDisposable();

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        internal IObservable<(string Property, string Type, string Message)> WhenErrorsChangedTyped
        {
            get;
            private set;
        }

        public IObservable<DataErrorsChangedEventArgs> WhenErrorsChanged { get; private set; }
        public bool IsDisposed { get; private set; }
        //public bool HasErrors => _validationErrors?.Items.Any(x => !(x.Message is null)) ?? false;
        public IObservable<bool> ObserveHasErrors { get; private set; }


        protected ReactiveObjectEx()
        {
             //WhenErrorsChangedTyped =
             //   _validationErrors.Connect()
             //                    .Flatten()
             //                    .Select(x => x.Current)
             //                    .DistinctUntilChanged();

             WhenErrorsChangedTyped =
                 _errorCollection.Connect()
                                 .Transform(x => x.Connect()
                                                  .Flatten()
                                                  .Select(y => y.Current))
                                 .Flatten()
                                 .Select(x => x.Current)
                                 .Merge();

            //WhenErrorsChanged =
            //    _validationErrors.Connect()
            //                     .Select(x => Observable.For<string, string>(x.Select(y => y.Current.Property).Distinct(), Observable.Return))
            //                     .Merge()
            //                     //.Select(x => x)
            //                     //.Flatten()
            //                     //.Select(x => (x.Current.Property, x.Current.Type))
            //                     //.DistinctUntilChanged()
            //                     .Select(x => new DataErrorsChangedEventArgs(x));

            WhenErrorsChanged
                = _errorCollection.Connect()
                                  .Transform(x => x.Connect()
                                                   .Select(_ => x.Items.First().Property)
                                                   .DistinctUntilChanged())
                                  .Flatten()
                                  .Select(x => x.Current)
                                  .Merge()
                                  .Select(x => new DataErrorsChangedEventArgs(x));

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

            //_validationErrors.Edit(context => context.AddOrUpdate((propertyName, validatorName, null)));
            //if (!(error is null))
            //    _validationErrors.Edit(context => context.AddOrUpdate((propertyName, validatorName, error)));

            _errorCollection.Edit(context =>
            {
                var lookup = context.Lookup(propertyName);
                var payload = lookup.HasValue
                    ? lookup.Value
                    : new SourceCache<(string Property, string Type, string Message), string>(x => x.Type);
                payload.AddOrUpdate((propertyName, validatorName, error));
                context.AddOrUpdate(payload);
            });
        }

        protected void BatchUpdateErrors(IEnumerable<(string Property, string Type, string Message)> updates)
        {
            var solidUpdates = updates.ToList();
            this.RaisePropertyChanging(nameof(HasErrors));
            //_validationErrors.Edit(context => context.AddOrUpdate(solidUpdates.Select(x => (x.Property, x.Type, (string)null))));
            //_validationErrors.Edit(context => context.AddOrUpdate(solidUpdates.Where( x=> !(x.Message is null))));

           _errorCollection.Edit(context =>
           {
               foreach (var group in solidUpdates.GroupBy(x => x.Property))
               {
                   var lookup = context.Lookup(group.Key);
                   var payload = lookup.HasValue
                       ? lookup.Value
                       : new SourceCache<(string Property, string Type, string Message), string>(x => x.Type);
                   payload.AddOrUpdate(group);

                   context.AddOrUpdate(payload);
               }
           });
        }
        protected void BatchUpdateErrors(params (string Property, string Type, string Message)[] updates)
        {
            this.RaisePropertyChanging(nameof(HasErrors));
            //_validationErrors.Edit(context => context.AddOrUpdate(updates.Select(x => (x.Property, x.Type, (string)null))));
            //_validationErrors.Edit(context => context.AddOrUpdate(updates.Where(x => !(x.Message is null))));

            _errorCollection.Edit(context =>
            {
                foreach (var group in updates.GroupBy(x => x.Property))
                {
                    var lookup = context.Lookup(group.Key);
                    var payload = lookup.HasValue
                        ? lookup.Value
                        : new SourceCache<(string Property, string Type, string Message), string>(x => x.Type);
                    payload.AddOrUpdate(group);

                    context.AddOrUpdate(payload);
                }
            });
        }

        protected void CreateValidator(IObservable<(string Type, string Message)> validationSource, string propertyName)
        {
            validationSource
                .Subscribe(x => UpdateErrors(propertyName, x.Type, x.Message))
                .DisposeWith(Subscriptions);
        }

        protected virtual void OnErrorsChanged(DataErrorsChangedEventArgs e)
            => ErrorsChanged?.Invoke(this, e);

        protected virtual void HookValidators()
        {
        }

        protected virtual void RemoveAllErrors(string propertyName)
        {
            //_validationErrors.Edit(context =>
            //{
            //    var items = context.Items.Where(x => x.Property == propertyName)
            //                       .Select(x => (x.Property, x.Type, Message: (string)null))
            //                       .ToList();
            //    context.AddOrUpdate(items);
            //});
            _errorCollection.Edit(context => context.Remove(propertyName));
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

                    //_validationErrors.Dispose();
                    foreach (var item in _errorCollection.Items)
                        item.Dispose();
                    _errorCollection.Dispose();
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

            //return _validationErrors.Items
            //                        .Where(x => x.Property == propertyName)
            //                        .Select(x => (x.Type, x.Message))
            //                        .ToList();
            => _errorCollection.Lookup(propertyName) is var lookup
               && lookup.HasValue
                ? lookup.Value.Items.Select(x => (x.Type, x.Message)).ToList()
                : new List<(string Type, string Message)>(0);
        

        //public virtual IEnumerable GetErrors(string propertyName)
        //{
        //    return _validationErrors.Items
        //                            .Where(x => x.Property == propertyName && !(x.Message is null))
        //                            .Select(x => x.Message);
        //}

        public virtual bool HasSpecificErrors(string propertyName)
            //=> _validationErrors?.Items.Any(x => x.Property == propertyName && !(x.Message is null)) ?? false;
            => _errorCollection.Lookup(propertyName) is var lookup
               && lookup.HasValue && lookup.Value.Items.Any(x => !(x.Message is null));
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #region v2

        //private readonly Dictionary<string, SourceCache<(string Type, string Message), string>> _errorCollection
        //    = new Dictionary<string, SourceCache<(string Type, string Message), string>>();

        private readonly SourceCache<SourceCache<(string Property, string Type, string Message), string>, string>
            _errorCollection =
                new SourceCache<SourceCache<(string Property, string Type, string Message), string>, string>(x =>
                    x.Items.First().Property);

        public bool HasErrors => _errorCollection.Items.Any(x => x.Items.Any(y => !(y.Message is null)));

        public virtual IEnumerable GetErrors(string propertyName)
            => _errorCollection.Lookup(propertyName) is var lookup
               && lookup.HasValue
                ? lookup.Value.Items.Select(x => x.Message).Where(x => !(x is null))
                : Enumerable.Empty<string>();


        #endregion
    }
}
