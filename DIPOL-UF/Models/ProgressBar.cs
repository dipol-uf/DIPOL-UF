using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DIPOL_UF.Models
{
    class ProgressBar : ObservableObject
    {
        private int minimum = 0;
        private int maximum = 100;
        private int value = 50;
        private bool isIndeterminate = false;
        private bool displayPercents = false;
        private string barTitle = "";
        private string barComment = "";
        private bool isAborted = false;

        public int Minimum
        {
            get => minimum;
            set
            {
                if (value != minimum)
                {
                    if (value >= maximum)
                        minimum = maximum - 1;
                    else
                        minimum = value;

                    if (Value < minimum)
                        Value = minimum;

                    RaisePropertyChanged();
                }
            }
        }

        public int Maximum
        {
            get => maximum;
            set
            {
                if (value != maximum)
                {
                    if (value <= minimum)
                        maximum = minimum + 1;
                    else
                        maximum = value;

                    if (Value > maximum)
                        Value = maximum;

                    RaisePropertyChanged();
                }
            }
        }

        public int Value
        {
            get => value;
            set
            {

                if (value != this.value)
                {
                    if (value < minimum)
                        this.value = minimum;
                    else if (value > maximum)
                        this.value = maximum;
                    else
                        this.value = value;

                    RaisePropertyChanged();
                }
            }
        }

        public bool IsIndeterminate
        {
            get => isIndeterminate;
            set
            {
                if (value != isIndeterminate)
                {
                    isIndeterminate = value;
                    RaisePropertyChanged();
                }
            }
        }

        public bool DisplayPercents
        {
            get => displayPercents;
            set
            {
                if (value != displayPercents)
                {
                    displayPercents = value;
                    RaisePropertyChanged();
                }
            }
        }

        public string BarTitle
        {
            get => barTitle;
            set
            {
                if (value != barTitle)
                {
                    barTitle = value;
                    RaisePropertyChanged();
                }
            }
        }

        public string BarComment
        {
            get => barComment;
            set
            {
                if (value != barComment)
                {
                    barComment = value;
                    RaisePropertyChanged();
                }
            }
        }

        public bool IsAborted
        {
            get => isAborted;
            set
            {
                if (value != isAborted)
                {
                    isAborted = value;
                    RaisePropertyChanged();
                }
            }
        }
        
        public Commands.DelegateCommand WindowDragCommand
        {
            get;
            private set;
        }

        public Commands.DelegateCommand CancelCommand
        {
            get;
            private set;
        }

        public bool TryIncrement()
            => ++Value <= Maximum;

        public bool Decrement()
            => --Value >= Minimum;


        public ProgressBar()
        {
            CancelCommand = new Commands.DelegateCommand(
                (param) => {
                    if (param is Window w)
                    {
                        w.DialogResult = false;
                        IsAborted = true;
                        w.Close();
                    }
                },
                (param) => !IsAborted);

            WindowDragCommand = new Commands.WindowDragCommandProvider().Command;
        }

    }
}
