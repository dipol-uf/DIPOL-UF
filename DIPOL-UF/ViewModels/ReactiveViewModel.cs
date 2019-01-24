using System;
using System.ComponentModel;
using System.Linq.Expressions;
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
                     .Select(x => (x.Type, x.Message)),
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
                                .ToPropertyEx(@this, targetProperty);

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
                 .ToPropertyEx(@this, targetProperty);

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
                 source.Select(converter)
                 .ToPropertyEx(@this, targetProperty);
                 if(!(validationSource is null))
                     @this.CreateValidator(
                         validationSource,
                         targetProperty.Body.GetMemberInfo().Name);
        }
    }
}
