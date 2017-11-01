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
    class DelegateCommand : ICommand
    {
        private Action<object> worker;
        private Func<object, bool> canExecute;
        private bool canExecuteState;

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            bool state = canExecute(parameter);
            if (state != canExecuteState)
            {
                canExecuteState = state;
                OnCanExecuteChanged(this, new EventArgs());
            }

            return state;
        }

        public void Execute(object parameter)
            => worker(parameter);

        public DelegateCommand(Action<object> worker, Func<object, bool> canExecute, bool initialCanExecuteState)
        {
            this.worker = worker ?? throw new ArgumentNullException();
            this.canExecute = canExecute ?? throw new ArgumentNullException();
            this.canExecuteState = initialCanExecuteState;
            OnCanExecuteChanged(this, new EventArgs());
        }

        protected virtual void OnCanExecuteChanged(object sender, EventArgs e)
            => CanExecuteChanged?.Invoke(sender, e);
    }
}
