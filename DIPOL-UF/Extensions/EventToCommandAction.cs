using System.Windows;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;

namespace DIPOL_UF.Extensions
{
    internal class EventToCommandAction : TriggerAction<DependencyObject>
    {
        private static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(EventToCommandAction));

        public ICommand Command
        {
            get => GetValue(CommandProperty) as ICommand;
            set => SetValue(CommandProperty, value);
        }

        protected override void Invoke(object parameter)
        {
            if(Command?.CanExecute(null) ?? false)
                Command.Execute(parameter);
        }
    }
}
