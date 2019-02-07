using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Windows.Input;
using DIPOL_UF.Models;
using ReactiveUI;

namespace DIPOL_UF.ViewModels
{


    internal class DescendantProxy : ReactiveObjectEx
    {
        public ICommand ViewFinished { get; }
        public ICommand WindowShown { get; }
        public event EventHandler ViewRequested;
        public event EventHandler ClosingRequested;

        public DescendantProxy(
            IObservable<ReactiveObjectEx> modelSource,
            IObservable<object> closingSource)
        {
            modelSource.ObserveOnUi()
                       .Subscribe(x => ViewRequested?.Invoke(this, new PropagatingEventArgs(x)))
                       .DisposeWith(_subscriptions);

            closingSource.ObserveOnUi()
                         .Subscribe(x => ClosingRequested?.Invoke(this, new EventArgs()))
                         .DisposeWith(_subscriptions);


            ViewFinished = ReactiveViewModelBase.DisposeFromViewCallbackCommand(_subscriptions);
        }

        public DescendantProxy(DescendantProvider provider,
            Func<ReactiveObjectEx, ReactiveViewModelBase> constructor)
        {
            if (provider is null)
                throw new ArgumentNullException(nameof(provider));

            if (constructor is null)
                throw new ArgumentNullException(nameof(constructor));

            provider.ViewRequested
                    .ObserveOnUi()
                    .Subscribe(x => 
                        ViewRequested?.Invoke(this, new PropagatingEventArgs(constructor(x))))
                    .DisposeWith(_subscriptions);

            provider.ClosingRequested
                    ?.ObserveOnUi()
                    .Subscribe(x => ClosingRequested?.Invoke(this, EventArgs.Empty))
                    .DisposeWith(_subscriptions);

            var shownCmd = ReactiveCommand.Create<Unit>(_ => { })
                                          .DisposeWith(_subscriptions);

            WindowShown = shownCmd;
                

            var finishedCmd = ReactiveViewModelBase
                .DisposeFromViewCallbackCommand(_subscriptions);

            ViewFinished = finishedCmd;

            if (!(provider.ViewFinished is null))
                finishedCmd.InvokeCommand(provider.ViewFinished).DisposeWith(_subscriptions);

            if (!(provider.WindowShown is null))
                shownCmd.InvokeCommand(provider.WindowShown).DisposeWith(_subscriptions);
        }
    }

}