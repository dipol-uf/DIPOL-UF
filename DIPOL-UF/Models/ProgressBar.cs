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

            MaximumReached = this.WhenAnyPropertyChanged(nameof(Value), nameof(Maximum))
                                 .Where(x => x.Value == x.Maximum)
                                 .Select(x => x.Value);
            MinimumReached = this.WhenAnyPropertyChanged(nameof(Value), nameof(Minimum))
                                 .Where(x => x.Value == x.Minimum)
                                 .Select(x => x.Value);


            WindowDragCommand = ReactiveCommand.Create<object>(
                Commands.WindowDragCommandProvider.Execute);
            CancelCommand = ReactiveCommand.Create<object>(param =>
            {
                if (param is Window w)
                {
                    if (Helper.IsDialogWindow(w))
                        w.DialogResult = false;
                    IsAborted = true;
                    w.Close();
                }
            }, this.WhenAnyPropertyChanged(nameof(CanAbort), nameof(IsAborted))
                   .Select(x => x.CanAbort && !x.IsAborted));


            HookValidators();
            HookObservers();
        }

        private void Reset()
        {
            Minimum = 0;
            Maximum = 100;
            Value = 0;
        }

        private void HookObservers()
        {
            this.WhenAnyPropertyChanged(nameof(IsIndeterminate))
                .DistinctUntilChanged()
                .Subscribe(x => Reset())
                .AddTo(_subscriptions);
        }

        protected sealed override void HookValidators()
        {
            CreateValidator(
                this.WhenAnyPropertyChanged(nameof(Value), nameof(Minimum), nameof(Maximum))
                    .Select(x => (
                        Type: nameof(Validate.ShouldFallWithinRange),
                         Message: Validate.ShouldFallWithinRange(x.Value, x.Minimum, x.Maximum))),
                nameof(Value));

            CreateValidator(
                this.WhenAnyPropertyChanged(nameof(Minimum))
                    .Select(x => (
                        Type: nameof(Validate.CannotBeGreaterThan),
                        Message: Validate.CannotBeGreaterThan(x.Minimum, x.Maximum))),
                nameof(Minimum));

            CreateValidator(
                this.WhenAnyPropertyChanged(nameof(Maximum))
                    .Select(x => (
                        Type: nameof(Validate.CannotBeLessThan),
                        Message: Validate.CannotBeLessThan(x.Maximum, x.Minimum))),
                nameof(Maximum));

            base.HookValidators();
        }

        public override void Dispose(bool disposing)
        {
            if (!IsDisposed && disposing)
            {
                CancelCommand.Dispose();
                WindowDragCommand.Dispose();
            }

            base.Dispose(disposing);
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
