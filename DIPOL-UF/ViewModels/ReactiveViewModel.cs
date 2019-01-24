using System;
using System.ComponentModel;
using System.Reactive.Linq;

namespace DIPOL_UF.ViewModels
{
    public abstract class ReactiveViewModel<TModel> : ReactiveObjectEx where TModel : ReactiveObjectEx
    {
        protected TModel Model { get; }
        protected ReactiveViewModel(TModel model)
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));
        }

        protected override void HookValidators()
        {
            //Observable.FromEventPattern<DataErrorsChangedEventArgs>(
            //              x => Model.ErrorsChanged += x,
            //              x => Model.ErrorsChanged -= x)
            //          .Subscribe(errorInfo =>
            //          {
            //              foreach (var (errorType, message) in Model.GetTypedErrors(errorInfo.EventArgs.PropertyName))
            //                  UpdateErrors(message, errorInfo.EventArgs.PropertyName, errorType);
            //          })
            //          .AddTo(_subscriptions);

            //base.HookValidators();
        }
    }
}
