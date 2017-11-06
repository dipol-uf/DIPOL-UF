using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows;

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
                    return String.Empty;

                if (DisplayPercents)
                    return String.Format("{0:F0}%", 100.0 * Value / (Maximum - Minimum));
                else
                {
                    double decDigits = Math.Ceiling(Math.Log10(Maximum % 10 == 0 ? Maximum + 1 : Maximum));

                    if (Minimum == 0)
                    {
                        var format = String.Format("{{0, {0:F0} }}/{{1, {0:F0} }}", decDigits);
                        return String.Format(format, Value, Maximum);
                    }
                    else
                    {
                        var format = String.Format("{{0, {0:F0} }} in ({{1, {0:F0} }}, {{2, {0:F0} }})", decDigits);

                        return String.Format(format, Value, Minimum, Maximum);
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

