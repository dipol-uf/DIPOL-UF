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
    sealed class WindowDragCommand 
    {
        private DelegateCommand command;

        private void Execute(object parameter)
        {

            if (parameter is Commands.EventCommandArgs<MouseButtonEventArgs> commandArgs)
            {
                var sender = commandArgs.Sender as FrameworkElement;
                var parent = VisualTreeHelper.GetParent(sender);
                while (!(parent is Window))
                    parent = VisualTreeHelper.GetParent(parent);

                if (parent is Window w)
                {
                    if(commandArgs.EventArgs.LeftButton== MouseButtonState.Pressed)
                        w.DragMove();
                }
            }


        }

        private bool CanExecute(object parameter) => true;


        public WindowDragCommand()
            => command = new DelegateCommand(Execute, CanExecute);


        public DelegateCommand Command => command;
    }
}
