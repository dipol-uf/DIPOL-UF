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

namespace DIPOL_UF.Commands
{
    class CommandEventArgs<T> where T : EventArgs
    {
        public T EventArgs
        {
            get;
            private set;
        }

        public object Sender
        {
            get;
            private set;
        }

        public CommandEventArgs(object sender, T args)
        {
            Sender = sender;
            EventArgs = args;
        }
    }
}
