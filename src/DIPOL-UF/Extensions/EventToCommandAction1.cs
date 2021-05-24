#nullable enable

using System.Windows;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;

namespace DIPOL_UF.Extensions
{
    internal class EventToCommandAction1 : TriggerAction<DependencyObject>
    {
        private static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(EventToCommandAction1));
        
        private static readonly DependencyProperty SenderProperty =
            DependencyProperty.Register(nameof(Sender), typeof(object), typeof(EventToCommandAction1));

        public ICommand? Command
        {
            get => GetValue(CommandProperty) as ICommand;
            set => SetValue(CommandProperty, value);
        }

        public object? Sender
        {
            get => GetValue(SenderProperty);
            set => SetValue(SenderProperty, value);
        }

        protected override void Invoke(object? parameter)
        {
            if(Command?.CanExecute(null) ?? false)
                Command.Execute((Sender, parameter));
        }
    }
}
