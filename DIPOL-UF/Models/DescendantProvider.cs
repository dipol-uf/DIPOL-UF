using System;
using System.Drawing.Design;
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
            ViewRequested = requestView.DisposeWith(_subscriptions);
            ClosingRequested = requestClosing?.DisposeWith(_subscriptions); ;
            ViewFinished = finalize?.DisposeWith(_subscriptions);
            WindowShown = windowShown?.DisposeWith(_subscriptions);
        }
    }
}
