using System;
using System.Windows;

namespace DIPOL_UF.Models
{
    class ProgressBar : ObservableObject
    {
        private int _minimum;
        private int _maximum = 100;
        private int _value = 50;
        private bool _isIndeterminate;
        private bool _displayPercents;
        private string _barTitle = "";
        private string _barComment = "";
        private bool _isAborted;
        private bool _canAbort = true;

        public event EventHandler AbortButtonClick;
        public event EventHandler MaximumReached;
        public event EventHandler MinimumReached;

        public int Minimum
        {
            get => _minimum;
            set
            {
                if (value != _minimum)
                {
                    if (value >= _maximum)
                        _minimum = _maximum - 1;
                    else
                        _minimum = value;

                    if (Value < _minimum)
                        Value = _minimum;

                    RaisePropertyChanged();
                }
            }
        }

        public int Maximum
        {
            get => _maximum;
            set
            {
                if (value != _maximum)
                {
                    if (value <= _minimum)
                        _maximum = _minimum + 1;
                    else
                        _maximum = value;

                    if (Value > _maximum)
                        Value = _maximum;

                    RaisePropertyChanged();
                }
            }
        }

        public int Value
        {
            get => _value;
            set
            {
                if (value != _value)
                {
                    int old = _value;

                    if (value < _minimum)
                        _value = _minimum;
                    else if (value > _maximum)
                        _value = _maximum;
                    else
                        _value = value;

                    if (_value != old)
                    {
                        RaisePropertyChanged();

                        if (_value == _maximum)
                            OnMaximumReached(this, new EventArgs());
                        else if (_value == _minimum)
                            OnMinimumReached(this, new EventArgs());
                    }
                }
            }
        }

        public bool IsIndeterminate
        {
            get => _isIndeterminate;
            set
            {
                if (value != _isIndeterminate)
                {
                    _isIndeterminate = value;
                    RaisePropertyChanged();
                }
            }
        }

        public bool DisplayPercents
        {
            get => _displayPercents;
            set
            {
                if (value != _displayPercents)
                {
                    _displayPercents = value;
                    RaisePropertyChanged();
                }
            }
        }

        public string BarTitle
        {
            get => _barTitle;
            set
            {
                if (value != _barTitle)
                {
                    _barTitle = value;
                    RaisePropertyChanged();
                }
            }
        }

        public string BarComment
        {
            get => _barComment;
            set
            {
                if (value != _barComment)
                {
                    _barComment = value;
                    RaisePropertyChanged();
                }
            }
        }

        public bool IsAborted
        {
            get => _isAborted;
            set
            {
                if (value != _isAborted)
                {
                    _isAborted = value;
                    RaisePropertyChanged();
                }
            }
        }

        public bool CanAbort
        {
            get => _canAbort;
            set
            {
                if (value != _canAbort)
                {
                    _canAbort = value;
                    RaisePropertyChanged();
                }
            }
        }
        
        public Commands.DelegateCommand WindowDragCommand
        {
            get;
        }

        public Commands.DelegateCommand CancelCommand
        {
            get;
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
                        if(Helper.IsDialogWindow(w))
                            w.DialogResult = false;
                        IsAborted = true;
                        OnAbortButtonClick(this, new EventArgs());
                        w.Close();
                    }
                },
                (param) => CanAbort && !IsAborted);

            WindowDragCommand = new Commands.WindowDragCommandProvider().Command;
        }


        protected virtual void OnAbortButtonClick(object sender, EventArgs e)
            => AbortButtonClick?.Invoke(this, e);
        protected virtual void OnMaximumReached(object sender, EventArgs e)
            => MaximumReached?.Invoke(this, e);
        protected virtual void OnMinimumReached(object sender, EventArgs e)
            => MinimumReached?.Invoke(this, e);
    }
}
