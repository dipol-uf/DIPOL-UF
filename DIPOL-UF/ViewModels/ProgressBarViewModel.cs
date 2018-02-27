using System;
using System.ComponentModel;
using System.Windows.Input;

namespace DIPOL_UF.ViewModels
{
    class ProgressBarViewModel : ViewModel<Models.ProgressBar>
    {

        public int Minimum
        {
            get => model.Minimum;
            set => model.Minimum = value;
        }

        public int Maximum
        {
            get => model.Maximum;
            set => model.Maximum = value;
        }

        public int Value
        {
            get => model.Value;
            set => model.Value = value;
        }

        public bool IsIndeterminate
        {
            get => model.IsIndeterminate;
            set => model.IsIndeterminate = value;
        }

        public bool DisplayPercents
        {
            get => model.DisplayPercents;
            set => model.DisplayPercents = value;
        }

        public string BarTitle
        {
            get => model.BarTitle;
            set => model.BarTitle = value;
        }

        public string BarComment
        {
            get => model.BarComment;
            set => model.BarComment = value;
        }

        public string ProgressText
        {
            get
            {
                if (IsIndeterminate)
                    return string.Empty;

                if (DisplayPercents)
                    return $"{100.0 * Value / (Maximum - Minimum):F0}%";
                else
                {
                    var decDigits = Math.Ceiling(Math.Log10(Maximum % 10 == 0 ? Maximum + 1 : Maximum));

                    if (Minimum == 0)
                    {
                        var format = $"{{0, {decDigits:F0} }}/{{1, {decDigits:F0} }}";
                        return string.Format(format, Value, Maximum);
                    }
                    else
                    {
                        var format = $"{{0, {decDigits:F0} }} in ({{1, {decDigits:F0} }}, {{2, {decDigits:F0} }})";

                        return string.Format(format, Value, Minimum, Maximum);
                    }

                }

            }
        }

        public bool CanAbort => model.CanAbort;

        public ICommand MouseDragEventHandler => model.WindowDragCommand;

        public ICommand CancelCommand => model.CancelCommand;

        public ProgressBarViewModel(Models.ProgressBar model) : base(model)
        {
        }

        protected override void OnModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnModelPropertyChanged(sender, e);

            if (e.PropertyName != nameof(BarTitle) && e.PropertyName != nameof(BarComment))
                OnPropertyChanged(this, new PropertyChangedEventArgs(nameof(ProgressText)));
        }

    }
}

