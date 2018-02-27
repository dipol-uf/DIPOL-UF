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
    public class DelegateCommand : ICommand
    {
        public static Func<object, bool> CanExecuteAlways { get; } =
            (param) => true;
        public static Func<object, bool> CanExecuteNever { get; } = 
            (param) => false;

        private readonly Action<object> _worker;
        private readonly Func<object, bool> _canExecute;
        private bool _oldCanExecuteState;
        
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            var canExecute = _canExecute(parameter);
            if (canExecute != _oldCanExecuteState)
            {
                _oldCanExecuteState = canExecute;
                OnCanExecuteChanged(this, new EventArgs());
            }

            return canExecute;
        }
                
        public void Execute(object parameter)
            => _worker(parameter);

        public DelegateCommand(Action<object> worker, Func<object, bool> canExecute)
        {
            _worker = worker ?? throw new ArgumentNullException();
            _canExecute = canExecute ?? throw new ArgumentNullException();
            OnCanExecuteChanged();
        }

        public void OnCanExecuteChanged() 
            => OnCanExecuteChanged(this, new EventArgs());

        protected virtual void OnCanExecuteChanged(object sender, EventArgs e)
            => Helper.ExecuteOnUI(() => CanExecuteChanged?.Invoke(sender, e));
    }
}
