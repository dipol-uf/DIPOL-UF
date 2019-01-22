using System;
using System.Windows;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace DIPOL_UF.Models
{
    internal class ProgressBar // : ObservableObject
    {
        public event EventHandler AbortButtonClick;
        public event EventHandler MaximumReached;
        public event EventHandler MinimumReached;
      
        public Commands.DelegateCommand WindowDragCommand
        {
            get;
        }


        public ReactiveCommand CancelCommand { get; }

        public ReactiveProperty<int> Minimum { get; }
        public ReactiveProperty<int> Maximum { get; }
        public ReactiveProperty<int> Value { get; }
        public ReactiveProperty<bool> IsIndeterminate { get; }
        public ReactiveProperty<bool> DisplayPercents { get; }
        public ReactiveProperty<string> BarTitle { get; }
        public ReactiveProperty<string> BarComment { get; }
        public ReactiveProperty<bool> IsAborted { get; }
        public ReactiveProperty<bool> CanAbort { get; }

        public bool TryIncrement()
            => ++Value.Value <= Maximum.Value;

        public bool Decrement()
            => --Value.Value >= Minimum.Value;

        public ProgressBar()
        {
            Minimum = new ReactiveProperty<int>(0).SetValidateNotifyError(x => Validators.Validator.CannotBeGreaterThan(x, Maximum.Value));
            Maximum = new ReactiveProperty<int>(100).SetValidateNotifyError(x => Validators.Validator.CannotBeLessThan(x, Minimum.Value));
            Value = new ReactiveProperty<int>(0).SetValidateNotifyError(x => Validators.Validator.ShouldFallWithinRange(x, Minimum.Value, Maximum.Value));
            IsIndeterminate = new ReactiveProperty<bool>(false);
            DisplayPercents = new ReactiveProperty<bool>(false);
            BarTitle = new ReactiveProperty<string>("");
            BarComment = new ReactiveProperty<string>("");
            IsAborted = new ReactiveProperty<bool>(false);
            CanAbort = new ReactiveProperty<bool>(true);

            CancelCommand = new ReactiveCommand(new[] {CanAbort, IsAborted.Inverse()}.CombineLatestValuesAreAllTrue());
            WindowDragCommand = new Commands.WindowDragCommandProvider().Command;

            HookCommands();
        }

        private void HookCommands()
        {
            CancelCommand.Subscribe(param =>
            {
                if (param is Window w)
                {
                    if (Helper.IsDialogWindow(w))
                        w.DialogResult = false;
                    IsAborted.Value = true;
                    w.Close();
                }
            });
        }


        protected virtual void OnAbortButtonClick(object sender, EventArgs e)
            => AbortButtonClick?.Invoke(this, e);
        protected virtual void OnMaximumReached(object sender, EventArgs e)
            => MaximumReached?.Invoke(this, e);
        protected virtual void OnMinimumReached(object sender, EventArgs e)
            => MinimumReached?.Invoke(this, e);
    }
}
