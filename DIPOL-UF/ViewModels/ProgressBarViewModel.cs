using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows.Input;

namespace DIPOL_UF.ViewModels
{
    class ProgressBarViewModel : ObservableObject
    {
        private Models.ProgressBar progressBar = new Models.ProgressBar();

        public int Minimum
        {
            get => progressBar.Minimum;
            set => progressBar.Minimum = value;
        }

        public int Maximum
        {
            get => progressBar.Maximum;
            set => progressBar.Maximum = value;
        }

        public int Value
        {
            get => progressBar.Value;
            set => progressBar.Value = value;
        }

        public bool IsIndeterminate
        {
            get => progressBar.IsIndeterminate;
            set => progressBar.IsIndeterminate = value;
        }

        public bool DisplayPercents
        {
            get => progressBar.DisplayPercents;
            set => progressBar.DisplayPercents = value;
        }    

        public string BarTitle
        {
            get => progressBar.BarTitle;
            set => progressBar.BarTitle = value;            
        }

        public string BarComment
        {
            get => progressBar.BarComment;
            set => progressBar.BarComment = value;
        }

        public string ProgressText
        {
            get
            {
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
                        try
                        {
                            Console.WriteLine(String.Format(format, Value, Minimum, Maximum));
                        }
                        catch (Exception e)
                        {

                            throw;
                        }
                        return String.Format(format, Value, Minimum, Maximum);
                    }
                    
                }
                    
            }
        }

        public double PercentageWidth
        {
            get
            {

                if (DisplayPercents)
                    return 60;
                else if (Minimum == 0)
                    return 90;
                else return 150;
            }
        }

        public ICommand TestCommand
            => new Commands.DelegateCommand(x => Console.WriteLine(x), _ => true, true);

        public ProgressBarViewModel(Models.ProgressBar model)
        {
            progressBar = model ?? throw new ArgumentNullException();

            model.PropertyChanged += ModelPropertyChanged;

            PropertyChanged += (sender, e) => Console.WriteLine(e.PropertyName);
        }


        protected virtual void ModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            RaisePropertyChanged(e.PropertyName);

            if (e.PropertyName != nameof(BarTitle) &&
                e.PropertyName != nameof(BarComment))
                RaisePropertyChanged(nameof(ProgressText));

            if (e.PropertyName == nameof(DisplayPercents) || e.PropertyName == nameof(Minimum))
                RaisePropertyChanged(nameof(PercentageWidth));
        }
    }
}

