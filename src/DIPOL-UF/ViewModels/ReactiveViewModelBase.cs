#nullable enable
using System.Reactive.Disposables;
using DIPOL_UF.UserNotifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace DIPOL_UF.ViewModels
{
    internal abstract class ReactiveViewModelBase : ReactiveObjectEx
    {
        protected IUserNotifier? Notifier { get; }
        protected ILogger? Logger { get; }
        public abstract ReactiveObjectEx ReactiveModel { get; }

        protected ReactiveViewModelBase(IUserNotifier? notifier = null, ILogger? logger = null) =>
            (Notifier, Logger) = (
                notifier ?? Injector.ServiceProvider.GetRequiredService<IUserNotifier>(),
                logger ?? Injector.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(GetType())
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
