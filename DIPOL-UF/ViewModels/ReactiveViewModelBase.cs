using System.Reactive.Disposables;
using ReactiveUI;

namespace DIPOL_UF.ViewModels
{
    internal abstract class ReactiveViewModelBase : ReactiveObjectEx
    {
        public abstract ReactiveObjectEx ReactiveModel { get; }

        public static ReactiveCommand<ReactiveViewModelBase, ReactiveObjectEx> 
            DisposeFromViewCallbackCommand(CompositeDisposable disposesWith)
        {
            var cmd = ReactiveCommand.Create<ReactiveViewModelBase, ReactiveObjectEx>(
                x =>
                {
                    var mdl = x.ReactiveModel;
                    x.Dispose();
                    return mdl;
                });

            return disposesWith is null ? cmd : cmd.DisposeWith(disposesWith);
        }
    }
}
