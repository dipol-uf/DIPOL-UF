using System;
using System.Windows;
using Microsoft.Xaml.Behaviors;

namespace DIPOL_UF.Extensions
{
    internal class ShowViewAction : TargetedTriggerAction<object>
    {
        private static readonly DependencyProperty BindingProperty =
            DependencyProperty.Register(nameof(Binding), typeof(object), typeof(ShowViewAction));

        private static readonly  DependencyProperty TypeProperty =
            DependencyProperty.Register(nameof(Type), typeof(object), typeof(ShowViewAction));

        public object Binding
        {
            get => GetValue(BindingProperty);
            set => SetValue(BindingProperty, value);
        }

        public object Type
        {
            get => GetValue(TypeProperty);
            set => SetValue(TypeProperty, value);
        }


        protected override void Invoke(object parameter)
        {
            //throw new NotImplementedException();
        }
    }
}
