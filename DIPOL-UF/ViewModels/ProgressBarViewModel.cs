using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIPOL_UF.ViewModels
{
    class ProgressBarViewModel : ObservableObject
    {
        private Models.ProgressBar progressBar = new Models.ProgressBar();
        private bool isIndeterminate = false;

        public int Minimum
        {
            get => progressBar?.Minimum ?? throw new ArgumentNullException();
            set
            {
                progressBar.Minimum = value;
                RaisePropertyChanged();
            }
        }

        public int Maximum
        {
            get => progressBar?.Maximum ?? throw new ArgumentNullException();
            set
            {
                progressBar.Maximum = value;
                RaisePropertyChanged();
            }
        }

        public int Value
        {
            get => progressBar?.Value ?? throw new ArgumentNullException();
            set
            {
                progressBar.Value = value;
                RaisePropertyChanged();
            }
        }

        public bool IsIndeterminate
        {
            get => isIndeterminate;
            set
            {
                isIndeterminate = value;
                RaisePropertyChanged();
            }
        }

        public ProgressBarViewModel()
        {
            PropertyChanged += (sender, e) => Console.WriteLine(e.PropertyName);
        }
    }
}
