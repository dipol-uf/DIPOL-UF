using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace DIPOL_UF.Models
{
    internal class ProgressBar
    {
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

        public IObservable<bool> MaximumReached { get; }
        public IObservable<bool> MinimumReached { get; }


        public bool TryIncrement()
        {
            if (Value.Value < Maximum.Value)
            {
                Value.Value++;
                return true;
            }

            return false;
        }

        public bool Decrement()
        {
            if (Value.Value > Minimum.Value)
            {
                Value.Value--;
                return true;
            }

            return false;
        }

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

            MaximumReached = Value.CombineLatest(Maximum, (v, m) => v == m).Where(x => x);
            MinimumReached = Value.CombineLatest(Minimum, (v, m) => v == m).Where(x => x);

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
    }
}
