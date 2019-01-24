using System;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Forms.VisualStyles;
using DIPOL_UF.Properties;
using DynamicData.Binding;
using MathNet.Numerics;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ProgressBar = DIPOL_UF.Models.ProgressBar;

namespace DIPOL_UF.ViewModels
{
    internal sealed class ProgressBarViewModel : ReactiveViewModel<ProgressBar>
    {
        private readonly ObservableAsPropertyHelper<int> _value;

        public int Value => _value.Value;
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

        public ProgressBarViewModel(ProgressBar model) : base(model)
        {
            _value = PropagateProperty(x => x.Value, nameof(Value));

            //Value = new ObservableAsPropertyHelper<int>(Model.WhenPropertyChanged(x => x.Value).Select(x => x.Value),
            //    x => this.RaisePropertyChanged(nameof(Value)),
            //    x => this.RaisePropertyChanging(nameof(Value)));
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

            HookValidators();
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

