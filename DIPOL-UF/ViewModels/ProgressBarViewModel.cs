using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Windows.Input;
using DIPOL_UF.Models;
using DIPOL_UF.Properties;
using MathNet.Numerics;
using Reactive.Bindings;
using ReactiveUI;
using ReactiveCommand = Reactive.Bindings.ReactiveCommand;

namespace DIPOL_UF.ViewModels
{
    internal class ProgressBarViewModel // : ViewModel<Models.ProgressBar>
    {

        private readonly ProgressBar _model;

        public ReadOnlyReactiveProperty<int> Value => _model.Value.ToReadOnlyReactiveProperty();
        public ReadOnlyReactiveProperty<int> Minimum => _model.Minimum.ToReadOnlyReactiveProperty();
        public ReadOnlyReactiveProperty<int> Maximum => _model.Maximum.ToReadOnlyReactiveProperty();
        public ReadOnlyReactiveProperty<bool> IsIndeterminate => _model.IsIndeterminate.ToReadOnlyReactiveProperty();
        public ReadOnlyReactiveProperty<bool> DisplayPercents => _model.DisplayPercents.ToReadOnlyReactiveProperty();
        public ReadOnlyReactiveProperty<string> BarTitle => _model.BarTitle.ToReadOnlyReactiveProperty();
        public ReadOnlyReactiveProperty<string> BarComment => _model.BarComment.ToReadOnlyReactiveProperty();
        public ReadOnlyReactiveProperty<string> ProgressText { get; }
        
        public ReadOnlyReactiveProperty<bool> CanAbort => _model.CanAbort.ToReadOnlyReactiveProperty();

        public ReactiveCommand MouseDragEventHandler => _model.WindowDragCommand;

        public ReactiveCommand CancelCommand => _model.CancelCommand;

        public ProgressBarViewModel(ProgressBar model)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));

            //ReSharper disable once InvokeAsExtensionMethod
            ProgressText = Value.CombineLatest(
                                    Minimum, Maximum, DisplayPercents, IsIndeterminate,
                                    ProgressTextFormatter)
                                .ToReadOnlyReactiveProperty();

            ProgressText.Subscribe(Console.WriteLine);
        }

        private static string ProgressTextFormatter(int value, int min, int max, bool displayPercent, bool isIndeterminate)
        {
            if (isIndeterminate)
                return Properties.Localization.ProgressBar_IndeterminateString;

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

