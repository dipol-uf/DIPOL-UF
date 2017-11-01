using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Markup;
using System.Reflection;

namespace DIPOL_UF.Commands
{
    class EventToCommndExtension : MarkupExtension
    {
        private string commandName = null;

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var valueTarget = (serviceProvider.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget);

            var boundPropertyInfo = valueTarget.TargetProperty as MethodInfo;
            var parameters = boundPropertyInfo.GetParameters();
            var delegateType = parameters[1].ParameterType;
            var delegateParameterTypes = delegateType.GetMethod("Invoke", BindingFlags.Public | BindingFlags.Instance)?.GetParameters().Select(p => p.ParameterType).ToArray();

            var eventHandlerMethodInfo = this.GetType().GetRuntimeMethods().First(mi => mi.Name == "DoAction").MakeGenericMethod(delegateParameterTypes[1]);

            return  Delegate.CreateDelegate(delegateType, this, eventHandlerMethodInfo);
         
        }

        private void DoAction<T>(object sender, T e) where T:RoutedEventArgs
        {
            if (sender is FrameworkElement element)
            {
                var commandArgsType = typeof(EventCommandArgs<>).MakeGenericType(typeof(T));
                var commandArgs = Activator.CreateInstance(commandArgsType, new object[] { sender, e });

                var delegateCommand = element.DataContext.GetType().GetProperty(commandName, BindingFlags.Instance | BindingFlags.Public).GetValue(element.DataContext) as DelegateCommand;

                if (delegateCommand.CanExecute(commandArgs))
                    delegateCommand.Execute(commandArgs);
                
            }
        }

        public EventToCommndExtension(object commandName)
        {

            // Arguments directly come from binding
            if (commandName is string command)
                this.commandName = command;
            else throw new ArgumentException();

            
        }
    }
}
