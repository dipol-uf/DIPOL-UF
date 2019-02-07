using System;
using System.Reactive;
using System.Windows;
using System.Windows.Input;
using DIPOL_UF.ViewModels;
using Microsoft.Xaml.Behaviors;

namespace DIPOL_UF.Extensions
{
    internal class ShowViewAction : TriggerAction<DependencyObject>
    {
        private static readonly  DependencyProperty TypeProperty =
            DependencyProperty.Register(nameof(Type), 
                typeof(Type), typeof(ShowViewAction));

        private static readonly DependencyProperty OwnerProperty =
            DependencyProperty.Register(nameof(Owner), 
                typeof(Window), typeof(ShowViewAction));

        private static readonly DependencyProperty IsDialogProperty =
            DependencyProperty.Register(nameof(IsDialog), 
                typeof(bool), typeof(ShowViewAction),
                new PropertyMetadata(false));

        private static readonly DependencyProperty StartupLocationProperty =
            DependencyProperty.Register(nameof(StartupLocation), 
                typeof(WindowStartupLocation), typeof(ShowViewAction),
                new PropertyMetadata(WindowStartupLocation.CenterScreen));
        
        private static readonly DependencyProperty CallbackCommandProperty =
            DependencyProperty.Register(nameof(CallbackCommand),
                typeof(ICommand), typeof(ShowViewAction));

        private static readonly DependencyProperty ProxyProperty =
            DependencyProperty.Register(nameof(Proxy),
                typeof(DescendantProxy), typeof(ShowViewAction));

        public Type Type
        {
            get => GetValue(TypeProperty) as Type;
            set => SetValue(TypeProperty, value);
        }
        public Window Owner
        {
            get => GetValue(OwnerProperty) as Window;
            set => SetValue(OwnerProperty, value);
        }
        public bool IsDialog
        {
            get => (bool)GetValue(IsDialogProperty);
            set => SetValue(IsDialogProperty, value); }
        public WindowStartupLocation StartupLocation
        {
            get => (WindowStartupLocation)GetValue(StartupLocationProperty);
            set => SetValue(StartupLocationProperty, value);
        }

        public ICommand CallbackCommand
        {
            get => GetValue(CallbackCommandProperty) as ICommand;
            set => SetValue(CallbackCommandProperty, value);
        }

        public DescendantProxy Proxy
        {
            get => GetValue(ProxyProperty) as DescendantProxy;
            set => SetValue(ProxyProperty, value);
        }


        protected override void Invoke(object parameter)
        {
            if (parameter is PropagatingEventArgs args)
            {
                if(Type is null)
                    throw new NullReferenceException(nameof(Type));

                if (Type.IsClass && Type.IsSubclassOf(typeof(Window)))
                {
                    var view = (Activator.CreateInstance(Type) as Window)
                        .WithDataContext(args.Content);

                    view.WindowStartupLocation = StartupLocation;
                    view.Owner = Owner;

                    if (!(CallbackCommand is null))
                        view.Closed += (sender, e) =>
                        {
                            if (CallbackCommand.CanExecute(args.Content))
                                CallbackCommand.Execute(args.Content);
                        };
                    else if (!(Proxy is null))
                    {
                        view.Closed += (sender, e) =>
                        {
                            if (Proxy.ViewFinished?.CanExecute(null) ?? false)
                                Proxy.ViewFinished.Execute(args.Content);
                        };

                        view.ContentRendered += (sender, e) =>
                        {
                            if (Proxy.WindowShown?.CanExecute(null) ?? false)
                                Proxy.WindowShown.Execute(Unit.Default);
                        };

                        Proxy.ClosingRequested += (sender, e) =>
                            view.Close();
                    }


                    if (IsDialog)
                        view.ShowDialog();
                    else
                        view.Show();
                }
            }
        }
    }
}
