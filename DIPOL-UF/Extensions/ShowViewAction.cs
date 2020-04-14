//    This file is part of Dipol-3 Camera Manager.

//     MIT License
//     
//     Copyright(c) 2018-2019 Ilia Kosenkov
//     
//     Permission is hereby granted, free of charge, to any person obtaining a copy
//     of this software and associated documentation files (the "Software"), to deal
//     in the Software without restriction, including without limitation the rights
//     to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//     copies of the Software, and to permit persons to whom the Software is
//     furnished to do so, subject to the following conditions:
//     
//     The above copyright notice and this permission notice shall be included in all
//     copies or substantial portions of the Software.
//     
//     THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//     IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//     FITNESS FOR A PARTICULAR PURPOSE AND NONINFINGEMENT. IN NO EVENT SHALL THE
//     AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//     LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//     OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//     SOFTWARE.

using System;
using System.Reactive;
using System.Windows;
using DIPOL_UF.ViewModels;
using Microsoft.Xaml.Behaviors;

namespace DIPOL_UF.Extensions
{
    internal class ShowViewAction : TriggerAction<DependencyObject>
    {
        public static readonly  DependencyProperty TypeProperty =
            DependencyProperty.Register(nameof(Type), 
                typeof(Type), typeof(ShowViewAction));

        public static readonly DependencyProperty OwnerProperty =
            DependencyProperty.Register(nameof(Owner), 
                typeof(Window), typeof(ShowViewAction));

        public static readonly DependencyProperty IsDialogProperty =
            DependencyProperty.Register(nameof(IsDialog), 
                typeof(bool), typeof(ShowViewAction),
                new PropertyMetadata(false));

        public static readonly DependencyProperty StartupLocationProperty =
            DependencyProperty.Register(nameof(StartupLocation), 
                typeof(WindowStartupLocation), typeof(ShowViewAction),
                new PropertyMetadata(WindowStartupLocation.CenterScreen));
        
        public static readonly DependencyProperty ProxyProperty =
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
                    throw new ArgumentNullException(nameof(Type));

                if(Proxy is null)
                    throw new ArgumentNullException(nameof(Proxy));

                if (Type.IsClass && Type.IsSubclassOf(typeof(Window)))
                {
                    var view = (Activator.CreateInstance(Type) as Window)
                        .WithDataContext(args.Content);

                    view.WindowStartupLocation = StartupLocation;
                    view.Owner = Owner;
                    
                    view.Closed += (sender, e) =>
                    {
                        if (Proxy.ViewFinished?.CanExecute(null) ?? false)
                        {
                            Proxy.ViewFinished.Execute(args.Content);
                            Owner.Focus();
                        }
                    };

                    view.ContentRendered += (sender, e) =>
                    {
                        if (Proxy.WindowShown?.CanExecute(null) ?? false)
                            Proxy.WindowShown.Execute(Unit.Default);
                    };

                    Proxy.ClosingRequested += (sender, e) =>
                        view.Close();


                    if (IsDialog)
                        view.ShowDialog();
                    else
                        view.Show();
                }
            }
        }
    }
}
