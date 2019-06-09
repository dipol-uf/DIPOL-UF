using System;
using System.Linq.Expressions;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace DIPOL_UF.ViewModels
{
    internal abstract class ReactiveViewModel<TModel> : ReactiveViewModelBase
        where TModel : ReactiveObjectEx
    {
        public override ReactiveObjectEx ReactiveModel => Model;
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
            bool withErrors = false) where TTarget : ReactiveViewModel<TModel>
        {
            @this.Model.WhenPropertyChanged(sourceProperty)
                 .Select(x => x.Value)
                 .ObserveOnUi()
                 .ToPropertyEx(@this, targetProperty)
                 .DisposeWith(@this.Subscriptions);

            if (withErrors)
            {
                var sourceName = sourceProperty?.Body.GetMemberInfo().Name;
                var targetName = targetProperty?.Body.GetMemberInfo().Name;
                @this.PropagateErrors(sourceName, targetName);
            }

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
                     .DisposeWith(@this.Subscriptions);

                 if(!(validationSource is null))
                     @this.CreateValidator(
                         validationSource
                             .ObserveOnUi(),
                         targetProperty.Body.GetMemberInfo().Name);
        }

    }
}
