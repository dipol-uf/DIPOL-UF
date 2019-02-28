using System;
using System.Reactive;
using System.Reactive.Disposables;
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
        
        public ReactiveCommand<Window, Unit> WindowDragCommand { get; private set; }
        public ReactiveCommand<Window, Unit> CancelCommand { get; private set; }

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
        public bool IsAborted { get; private set; }
        [Reactive]
        public bool CanAbort { get; set; }

        public IObservable<int> MaximumReached { get; private set;}
        public IObservable<int> MinimumReached { get; private set; }


        public ProgressBar()
        {
            Reset();

            
            InitializeCommands();
            HookValidators();
            HookObservers();
        }

        private void Reset()
        {
            Minimum = 0;
            Maximum = 100;
            Value = 0;
        }

        private void InitializeCommands()
        {
            WindowDragCommand =
                ReactiveCommand.Create<Window>(
                                   Commands.WindowDragCommandProvider.Execute)
                               .DisposeWith(Subscriptions);
            CancelCommand =
                ReactiveCommand.Create<Window>(param =>
                               {
                                   if (Helper.IsDialogWindow(param))
                                       param.DialogResult = false;
                                   IsAborted = true;
                                   param.Close();
                               }, this.WhenAnyPropertyChanged(nameof(CanAbort), nameof(IsAborted))
                                      .Select(x => x.CanAbort && !x.IsAborted)
                                      .ObserveOnUi())
                               .DisposeWith(Subscriptions);
        }

        private void HookObservers()
        {
            MaximumReached = this.WhenAnyPropertyChanged(nameof(Value), nameof(Maximum))
                                 .Where(x => x.Value == x.Maximum)
                                 .Select(x => x.Value);
            MinimumReached = this.WhenAnyPropertyChanged(nameof(Value), nameof(Minimum))
                                 .Where(x => x.Value == x.Minimum)
                                 .Select(x => x.Value);


            this.WhenAnyPropertyChanged(nameof(IsIndeterminate))
                .DistinctUntilChanged()
                .Subscribe(x => Reset())
                .DisposeWith(Subscriptions);
        }

        protected sealed override void HookValidators()
        {
            CreateValidator(
                this.WhenAnyPropertyChanged(
                        nameof(Value), nameof(Minimum), nameof(Maximum))
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

        protected override void Dispose(bool disposing)
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
