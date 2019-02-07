﻿using System;
using System.Linq.Expressions;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace DIPOL_UF.ViewModels
{
    public abstract class ReactiveViewModel<TModel> : ReactiveObjectEx where TModel : ReactiveObjectEx
    {
        protected TModel Model { get; }
        protected ReactiveViewModel(TModel model)
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));
        }

        protected void PropagateErrors(string sourceName, string targetName)
        {
            CreateValidator(
                Model.WhenErrorsChangedTyped.Where(x => x.Property == sourceName)
                     .Select(x => (x.Type, x.Message))
                     .ObserveOnUi(),
                targetName);
        }

        protected void PropagateErrors<TSource>(
            TSource source, string sourceName,
            string targetName)
            where TSource : ReactiveObjectEx
        {
            CreateValidator(
                source.WhenErrorsChangedTyped
                      .Where(x => x.Property == sourceName)
                      .Select(x => (x.Type, x.Message))
                      .ObserveOnUi(),
                targetName);
        }


        protected static void PropagateReadOnlyProperty<TTarget, TProperty>(
            TTarget @this,
            Expression<Func<TModel, TProperty>> sourceProperty,
            Expression<Func<TTarget, TProperty>> targetProperty,
            bool withErrors = true) where TTarget : ReactiveViewModel<TModel>
        {
            @this.Model.WhenPropertyChanged(sourceProperty)
                 .Select(x => x.Value)
                 .ObserveOnUi()
                 .ToPropertyEx(@this, targetProperty)
                 .DisposeWith(@this._subscriptions);

            var sourceName = sourceProperty?.Body.GetMemberInfo().Name;
            var targetName = targetProperty?.Body.GetMemberInfo().Name;
            if(withErrors)
                @this.PropagateErrors(sourceName, targetName);

        }

        protected static void PropagateReadOnlyProperty<TTarget, TSource, TProperty>(
            TTarget @this,
            Expression<Func<TModel, TSource>> sourceProperty,
            Expression<Func<TTarget, TProperty>> targetProperty,
            Func<TSource, TProperty> converter,
            bool withErrors = true) where TTarget : ReactiveViewModel<TModel>
        {
            @this.Model.WhenPropertyChanged(sourceProperty)
                 .Select(x => converter(x.Value))
                 .ObserveOnUi()
                 .ToPropertyEx(@this, targetProperty)
                 .DisposeWith(@this._subscriptions);

            var sourceName = sourceProperty?.Body.GetMemberInfo().Name;
            var targetName = targetProperty?.Body.GetMemberInfo().Name;
            if(withErrors)
                @this.PropagateErrors(sourceName, targetName);

        }

        protected static void PropagateReadOnlyProperty<TTarget, TSource, TProperty>(
            TTarget @this,
            IObservable<TSource> source,
            Expression<Func<TTarget, TProperty>> targetProperty,
            Func<TSource, TProperty> converter,
            IObservable<(string Type, string Message)> validationSource = null) where TTarget : ReactiveViewModel<TModel>
        {
                 source
                     .Select(converter)
                     .ObserveOnUi()
                     .ToPropertyEx(@this, targetProperty)
                     .DisposeWith(@this._subscriptions);

                 if(!(validationSource is null))
                     @this.CreateValidator(
                         validationSource
                             .ObserveOnUi(),
                         targetProperty.Body.GetMemberInfo().Name);
        }

        protected static ReactiveCommand<ReactiveViewModel<TSource>, TSource> 
            DisposeFromViewCallbackCommand<TSource>(CompositeDisposable disposesWith)
            where TSource : ReactiveObjectEx
        {
            var cmd = ReactiveCommand.Create<ReactiveViewModel<TSource>, TSource>(
                x =>
                {
                    var mdl = x.Model;
                    x.Dispose();
                    return mdl;
                });

            return disposesWith is null ? cmd : cmd.DisposeWith(disposesWith);
        }
    }
}
