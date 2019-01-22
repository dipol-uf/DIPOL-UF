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
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DIPOL_UF.Commands
{
    static class WindowDragCommandProvider 
    {
        public static void Execute(object parameter)
        {

            if (parameter is CommandEventArgs<MouseButtonEventArgs> commandArgs &&
                commandArgs.Sender is FrameworkElement sender)
            {
                var parent = VisualTreeHelper.GetParent(sender);
                while (!(parent is Window) && !(parent is null))
                    parent = VisualTreeHelper.GetParent(parent);

                if (parent != null)
                {
                    if(commandArgs.EventArgs.LeftButton== MouseButtonState.Pressed)
                        (parent as Window).DragMove();
                }
            }


        }

        public static bool CanExecute(object parameter) => true;


        static WindowDragCommandProvider()
            => Command = new DelegateCommand(Execute, CanExecute);


        public static DelegateCommand Command { get; }
    }
}
