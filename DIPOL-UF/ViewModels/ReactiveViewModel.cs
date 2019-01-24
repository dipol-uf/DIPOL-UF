using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reactive.Linq;
using DynamicData.Binding;
using ReactiveUI;

namespace DIPOL_UF.ViewModels
{
    public abstract class ReactiveViewModel<TModel> : ReactiveObjectEx where TModel : ReactiveObjectEx
    {
        protected TModel Model { get; }
        protected ReactiveViewModel(TModel model)
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));
        }

        protected ObservableAsPropertyHelper<TProperty> PropagateProperty<TProperty>(
            Expression<Func<TModel, TProperty>> sourceProperty,
            string targetProperty)
        {
            var property = Model.WhenPropertyChanged(sourceProperty)
                                .Select(x => x.Value)
                                .ToProperty(this, targetProperty);

            var name = sourceProperty.Body.GetMemberInfo().Name;
            CreateValidator(
                Model.WhenErrorsChangedTyped.Where(x => x.Property == name)
                     .Select(x => (x.Type, x.Message)),
                targetProperty);

            return property;
        }
    }
}
