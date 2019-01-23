using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Forms.VisualStyles;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using DIPOL_UF.Validators;

using PropertyErrorCache = DynamicData.SourceCache<(string ErrorType, string Message), string>;
using GlobalErrorCache = DynamicData.SourceCache<(string Property, DynamicData.SourceCache<(string ErrorType, string Message), string> Errors), string>;

namespace DIPOL_UF.Models
{
    internal class ProgressBar : ReactiveObject, INotifyDataErrorInfo
    {
        
        private readonly GlobalErrorCache _observableErrors =
            new GlobalErrorCache(x => x.Property);

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

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

        public IEnumerable GetErrors(string propertyName)
        {
            var result = _observableErrors.Lookup(propertyName);
            return result.HasValue ? result.Value.Errors.Items.Select(x => x.Message).ToList() : null;
        }

        public bool HasErrors => _observableErrors.KeyValues.Any(x => x.Value.Errors.KeyValues.Any());

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
                .Subscribe(x => Reset());
        }

        private void HookValidators()
        {

            this.WhenAnyPropertyChanged(nameof(Value), nameof(Minimum), nameof(Maximum))
                .Select(x => Validate.ShouldFallWithinRange(x.Value, x.Minimum, x.Maximum))
                .Subscribe(x => UpdateErrors(x, nameof(Value), nameof(Validate.ShouldFallWithinRange)));

            this.WhenPropertyChanged(x => x.Minimum)
                .Select(x => Validate.CannotBeGreaterThan(x.Value, Maximum))
                .Subscribe(x => UpdateErrors(x, nameof(Minimum), nameof(Validate.CannotBeGreaterThan)));

            this.WhenPropertyChanged(x => x.Maximum)
                .Select(x => Validate.CannotBeLessThan(x.Value, Minimum))
                .Subscribe(x => UpdateErrors(x, nameof(Maximum), nameof(Validate.CannotBeLessThan)));

            _observableErrors.Connect().Subscribe(_ => this.RaisePropertyChanged(nameof(HasErrors)));
        }

        private void UpdateErrors(string error, string propertyName, string validatorName)
        {
            this.RaisePropertyChanging(nameof(HasErrors));

            _observableErrors.Edit(global =>
            {
                var globalCollection = global.Lookup(propertyName);

                var value = globalCollection.HasValue
                    ? globalCollection.Value
                    : (Property: propertyName, Errors: new PropertyErrorCache(x => x.ErrorType));

                value.Errors.Edit(local =>
                {
                    if (error is null)
                        local.Remove(validatorName);
                    else
                        local.AddOrUpdate((validatorName, error));
                });
                global.AddOrUpdate(value);

            });

        }

        private void Reset()
        {
            Minimum = 0;
            Maximum = 100;
            Value = 0;

           _observableErrors.Edit(globalUpdater =>
           {
               foreach(var item in globalUpdater.Items)
                   item.Errors.Edit(locUpdater => locUpdater.Clear());
           });
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
