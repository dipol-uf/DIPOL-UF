using System;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Windows.Input;
using DIPOL_UF.Models;
using Reactive.Bindings;

namespace DIPOL_UF.ViewModels
{
    internal class ProgressBarViewModel // : ViewModel<Models.ProgressBar>
    {

        private readonly ProgressBar _model;

        public ReactiveProperty<int> Value => _model.Value;
        public ReactiveProperty<int> Minimum => _model.Minimum;
        public ReactiveProperty<int> Maximum => _model.Maximum;
        public ReactiveProperty<bool> IsIndeterminate => _model.IsIndeterminate;
        public ReactiveProperty<bool> DisplayPercents => _model.DisplayPercents;
        public ReactiveProperty<string> BarTitle => _model.BarTitle;
        public ReactiveProperty<string> BarComment => _model.BarComment;
        public ReadOnlyReactiveProperty<string> ProgressText { get; }
        
        public ReadOnlyReactiveProperty<bool> CanAbort => _model.CanAbort.ToReadOnlyReactiveProperty();

        public ReactiveCommand MouseDragEventHandler => _model.WindowDragCommand;

        public ReactiveCommand CancelCommand => _model.CancelCommand;

        public ProgressBarViewModel(ProgressBar model)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            
            //IsIndeterminate.
            ProgressText = IsIndeterminate.CombineLatest(DisplayPercents, Value, Minimum, Maximum,
                (isInd, percent, val, min, max) =>
                {
                    if (isInd)
                        return Properties.Localization.ProgressBar_IndeterminateString;
                    if (percent)
                        return string.Format(
                            Properties.Localization.ProgressBar_DisplayPercentString,
                            100 * val / (max - min));

                    var decDigits =
                        Math.Ceiling(Math.Log10(max % 10 == 0 ? max + 1 : max));
                    var format = "";

                    if (min == 0)
                    {
                        format = string.Format(
                            Properties.Localization.ProgressBar_DisplayCountFormatString,
                            decDigits);
                        return string.Format(format, val, max);
                    }

                    format = string.Format(
                        Properties.Localization.ProgressBar_DisplayRangeFormatString,
                        decDigits);

                    return string.Format(format, val, min, max);
                }).ToReadOnlyReactiveProperty();
        }



    }
}

