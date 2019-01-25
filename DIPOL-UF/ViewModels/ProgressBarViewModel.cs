using System;
using System.Linq;
using System.Reactive;
using System.Windows.Input;
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

        // [ObservableAsProperty] generates readonly property
        // with a backing field via ReactiveUI.Fody
        // ReSharper disable UnassignedGetOnlyAutoProperty
        public int Value { [ObservableAsProperty] get; }
        public int Minimum { [ObservableAsProperty] get; }
        public int Maximum { [ObservableAsProperty] get; }
        public bool IsIndeterminate { [ObservableAsProperty] get; }
        public bool DisplayPercent { [ObservableAsProperty] get; }
        public string BarTitle { [ObservableAsProperty] get; }
        public string BarComment { [ObservableAsProperty] get; }
        public string ProgressText { [ObservableAsProperty] get; }
        // ReSharper restore UnassignedGetOnlyAutoProperty

        public ICommand WindowDragCommand => Model.WindowDragCommand;
        public ICommand CancelCommand => Model.CancelCommand;

        public ProgressBarViewModel(ProgressBar model) : base(model)
        {
            MapProperties();
            HookValidators();
        }

        private void MapProperties()
        {
            PropagateReadOnlyProperty(this, x => x.Value, y => y.Value);
            PropagateReadOnlyProperty(this, x => x.Minimum, y => y.Minimum);
            PropagateReadOnlyProperty(this, x => x.Maximum, y => y.Maximum);
            PropagateReadOnlyProperty(this, x => x.IsIndeterminate, y => y.IsIndeterminate);
            PropagateReadOnlyProperty(this, x => x.BarTitle, y => y.BarTitle);
            PropagateReadOnlyProperty(this, x => x.BarComment, y => y.BarComment);
            PropagateReadOnlyProperty(this, x => x.DisplayPercents, y => y.DisplayPercent);

            PropagateReadOnlyProperty(
                this,
                this.WhenAnyPropertyChanged(
                    nameof(Value), nameof(Minimum),
                    nameof(Maximum), nameof(IsIndeterminate),
                    nameof(DisplayPercent), nameof(HasErrors)),
                x => x.ProgressText,
                ProgressTextFormatter);
        }

        private static string ProgressTextFormatter(ProgressBarViewModel @this)
        {
            if (@this.HasErrors)
                return Localization.ProgressBar_IsInvalidString;

            if (@this.IsIndeterminate)
                return Localization.ProgressBar_IndeterminateString;

            if (@this.DisplayPercent)
                return string.Format(
                    Localization.ProgressBar_DisplayPercentString, 
                    100.0 * @this.Value / (@this.Maximum - @this.Minimum));

            string format;
            var decDigit = new[] { @this.Value, @this.Minimum, @this.Maximum, 1 }
                           .Where(x => x != 0)
                           .Select(x => Math.Log10(Math.Abs(x)))
                           .Select(x => new {Log = x, Ceiling = Math.Ceiling(x)})
                           .Select(x => x.Log.AlmostEqualRelative(x.Ceiling) ? x.Ceiling + 1 : x.Ceiling)
                           .Max();
            if (@this.Minimum == 0)
            {
                format = string.Format(Localization.ProgressBar_DisplayCountFormatString, decDigit);

                return string.Format(format, @this.Value, @this.Maximum);
            }

            format = string.Format(Localization.ProgressBar_DisplayRangeFormatString, decDigit);

            return string.Format(format, @this.Value, @this.Minimum, @this.Maximum);
        }
    }
}

