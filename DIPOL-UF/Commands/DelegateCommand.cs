﻿using System;
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
        private static readonly Func<object, bool> canExecuteAlways = (param) => true;
        private static readonly Func<object, bool> canExecuteNever = (param) => false;

        private Action<object> worker;
        private Func<object, bool> canExecute;
        private bool oldCanExecuteState;

        public static Func<object, bool> CanExecuteAlways => canExecuteAlways;
        public static Func<object, bool> CanExecuteNever => canExecuteNever;


        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            bool canExecute = this.canExecute(parameter);
            if (canExecute != oldCanExecuteState)
            {
                oldCanExecuteState = canExecute;
                OnCanExecuteChanged(this, new EventArgs());
            }

            return canExecute;
        }
                
        public void Execute(object parameter)
            => worker(parameter);

        public DelegateCommand(Action<object> worker, Func<object, bool> canExecute)
        {
            this.worker = worker ?? throw new ArgumentNullException();
            this.canExecute = canExecute ?? throw new ArgumentNullException();
            OnCanExecuteChanged();
        }

        public void OnCanExecuteChanged() 
            => OnCanExecuteChanged(this, new EventArgs());

        protected virtual void OnCanExecuteChanged(object sender, EventArgs e)
            => CanExecuteChanged?.Invoke(sender, e);
    }
}
