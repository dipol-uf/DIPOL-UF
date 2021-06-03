#nullable enable
using System.Reactive.Disposables;
using DIPOL_UF.Annotations;
using DIPOL_UF.Properties;
using DIPOL_UF.UserNotifications;
using ReactiveUI;
using Serilog;

namespace DIPOL_UF.ViewModels
{
    internal abstract class ReactiveViewModelBase : ReactiveObjectEx
    {
        protected IUserNotifier? Notifier { get; }
        protected ILogger? Logger { get; }
        public abstract ReactiveObjectEx ReactiveModel { get; }

        protected ReactiveViewModelBase(IUserNotifier? notifier = null, ILogger? logger = null) =>
            (Notifier, Logger) = (
                notifier ?? Injector.LocateOrDefault<IUserNotifier>(),
                logger ?? Injector.LocateOrDefault<ILogger>()
            );
            
        
        public static ReactiveCommand<ReactiveViewModelBase, ReactiveObjectEx> 
            DisposeFromViewCallbackCommand(CompositeDisposable? disposesWith)
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
