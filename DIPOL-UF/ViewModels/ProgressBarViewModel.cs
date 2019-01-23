using System;
using System.Linq;
using DIPOL_UF.Properties;
using MathNet.Numerics;
using ProgressBar = DIPOL_UF.Models.ProgressBar;

namespace DIPOL_UF.ViewModels
{
    internal class ProgressBarViewModel // : ViewModel<Models.ProgressBar>
    {

        private readonly ProgressBar _model;

        //public ReactiveProperty<int> Value => _model.Value;
        //public ReactiveProperty<int> Minimum => _model.Minimum;
        //public ReactiveProperty<int> Maximum => _model.Maximum;

        //public ReadOnlyReactiveProperty<bool> IsIndeterminate { get; }
        //public ReadOnlyReactiveProperty<string> BarTitle { get; }
        //public ReadOnlyReactiveProperty<string> BarComment { get; }
        //public ReadOnlyReactiveProperty<string> ProgressText { get; }

        //public ReadOnlyReactiveProperty<bool> CanAbort => _model.CanAbort.ToReadOnlyReactiveProperty();

        //public ReactiveCommand MouseDragEventHandler => _model.WindowDragCommand;

        //public ReactiveCommand CancelCommand => _model.CancelCommand;

        public ProgressBarViewModel(ProgressBar model)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));

            //    IsIndeterminate = _model.IsIndeterminate.ToReadOnlyReactiveProperty();
            //    BarTitle = _model.BarTitle.ToReadOnlyReactiveProperty();
            //    BarComment = _model.BarComment.ToReadOnlyReactiveProperty();

            //    //Value = _model.Value.ToReadOnlyReactiveProperty();
            //    //Value.Subscribe(x => Console.WriteLine($"Value is {x}"));

            //    //ReSharper disable once InvokeAsExtensionMethod
            //    ProgressText = Value.CombineLatest(
            //                            Minimum, Maximum, _model.DisplayPercents, IsIndeterminate,
            //                            ProgressTextFormatter)
            //                        .ToReadOnlyReactiveProperty();

            //    ProgressText.Subscribe(Console.WriteLine);


        }

        private static string ProgressTextFormatter(int value, int min, int max, bool displayPercent, bool isIndeterminate)
        {
            if (isIndeterminate)
                return Localization.ProgressBar_IndeterminateString;

            if (displayPercent)
                return string.Format(Localization.ProgressBar_DisplayPercentString, 100.0 * value / (max - min));

            var format = "";
            var decDigit = new[] {value, min, max}
                       .Select(x => Math.Log10(x))
                       .Select(x => new {Log = x, Ceiling = Math.Ceiling(x)})
                       .Select(x => x.Log.AlmostEqualRelative(x.Ceiling) ? x.Ceiling + 1 : x.Ceiling)
                       .Max();

            if (min == 0)
            {
                format = string.Format(Localization.ProgressBar_DisplayCountFormatString, decDigit);

                return string.Format(format, value, max);
            }

            format = string.Format(Localization.ProgressBar_DisplayRangeFormatString, decDigit);

            return string.Format(format, value, min, max);
        }
    }
}

