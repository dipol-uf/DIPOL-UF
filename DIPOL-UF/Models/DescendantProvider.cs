using System;
using System.Reactive;
using System.Reactive.Disposables;
using ReactiveUI;

namespace DIPOL_UF.Models
{
    internal class DescendantProvider : ReactiveObjectEx
    {
        public ReactiveCommand<object, ReactiveObjectEx> ViewRequested { get; }
        public ReactiveCommand<Unit, Unit> ClosingRequested { get; }

        public ReactiveCommand<ReactiveObjectEx, Unit> ViewFinished { get; }
        public ReactiveCommand<Unit, Unit> WindowShown { get; }

        public DescendantProvider(
            ReactiveCommand<object, ReactiveObjectEx> requestView,
            ReactiveCommand<Unit, Unit> windowShown,
            ReactiveCommand<Unit, Unit> requestClosing,
            ReactiveCommand<ReactiveObjectEx, Unit> finalize)
        {
            if(requestView is null)
                throw new ArgumentNullException(nameof(requestView));
            ViewRequested = requestView.DisposeWith(Subscriptions);

            ClosingRequested = (requestClosing ??
                                ReactiveCommand.Create<Unit>(_ => { }))
                .DisposeWith(Subscriptions);

            ViewFinished = (finalize ??
                            ReactiveCommand.Create<ReactiveObjectEx, Unit>(_ => Unit.Default))
                .DisposeWith(Subscriptions);

            WindowShown = (windowShown ?? 
                           ReactiveCommand.Create<Unit>(_ => { }))
                .DisposeWith(Subscriptions);
        }
    }
}
