using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using DIPOL_UF.Validators;

namespace DIPOL_UF.Models
{
    internal class ProgressBar : ReactiveObjectEx
    {
        
        public ReactiveCommand<object, Unit> WindowDragCommand { get; }
        public ReactiveCommand<object, Unit> CancelCommand { get; }

        [Reactive]
        public int Minimum { get; set; }
        [Reactive]
        public int Maximum { get; set; }
        [Reactive]
        public int Value { get; set; }
        [Reactive]
        public bool IsIndeterminate { get; set; }
        [Reactive]
        public bool DisplayPercents { get; set; }
        [Reactive]
        public string BarTitle { get; set; }
        [Reactive]
        public string BarComment { get; set; }
        [Reactive]
        public bool IsAborted { get; set; }
        [Reactive]
        public bool CanAbort { get; set; }

        public IObservable<int> MaximumReached { get; }
        public IObservable<int> MinimumReached { get; }


        public ProgressBar()
        {
            Reset();

            MaximumReached = this.WhenAnyValue(x => x.Value, y => y.Maximum)
                                 .Where(x => x.Item1 == x.Item2)
                                 .Select(x => x.Item1);
            MinimumReached = this.WhenAnyValue(x => x.Value, y => y.Minimum)
                                 .Where(x => x.Item1 == x.Item2)
                                 .Select(x => x.Item1);


            WindowDragCommand = ReactiveCommand.Create<object>(Commands.WindowDragCommandProvider.Execute);
            CancelCommand = ReactiveCommand.Create<object>(param =>
            {
                if (param is Window w)
                {
                    if (Helper.IsDialogWindow(w))
                        w.DialogResult = false;
                    IsAborted = true;
                    w.Close();
                }
            }, this.WhenAnyPropertyChanged(nameof(CanAbort), nameof(IsAborted)).Select(x => x.CanAbort && !x.IsAborted));


            HookValidators();
            HookObservers();
        }

        private void HookObservers()
        {
            this.WhenPropertyChanged(x => x.IsIndeterminate)
                .DistinctUntilChanged()
                .Subscribe(x => Reset())
                .AddTo(_subscriptions);
        }

        protected sealed override void HookValidators()
        {

            this.WhenAnyPropertyChanged(nameof(Value), nameof(Minimum), nameof(Maximum))
                .Select(x => Validate.ShouldFallWithinRange(x.Value, x.Minimum, x.Maximum))
                .Subscribe(x => UpdateErrors(x, nameof(Value), nameof(Validate.ShouldFallWithinRange)))
                .AddTo(_subscriptions);

            this.WhenPropertyChanged(x => x.Minimum)
                .Select(x => Validate.CannotBeGreaterThan(x.Value, Maximum))
                .Subscribe(x => UpdateErrors(x, nameof(Minimum), nameof(Validate.CannotBeGreaterThan)))
                .AddTo(_subscriptions);

            this.WhenPropertyChanged(x => x.Maximum)
                .Select(x => Validate.CannotBeLessThan(x.Value, Minimum))
                .Subscribe(x => UpdateErrors(x, nameof(Maximum), nameof(Validate.CannotBeLessThan)))
                .AddTo(_subscriptions);

            base.HookValidators();
        }
        
        private void Reset()
        {
            Minimum = 0;
            Maximum = 100;
            Value = 0;
        }

        public bool TryIncrement()
        {
            if (!HasErrors && Value < Maximum)
            {
                Value++;
                return true;
            }

            return false;
        }

        public bool TryDecrement()
        {
            if (!HasErrors && Value > Minimum)
            {
                Value--;
                return true;
            }

            return false;
        }


    }
}
