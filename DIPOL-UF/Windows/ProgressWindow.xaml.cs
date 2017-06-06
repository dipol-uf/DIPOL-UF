using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DIPOL_UF.Windows
{
    /// <summary>
    /// Interaction logic for ProgressWindow.xaml
    /// </summary>
    public partial class ProgressWindow : Window
    {
        private static DependencyProperty DisplayedProgressTextProperty = DependencyProperty.Register("DisplayedProgressText", typeof(string), typeof(ProgressWindow));
        private static DependencyProperty DisplayedTitleTextProperty = DependencyProperty.Register("DisplayedTitleText", typeof(string), typeof(ProgressWindow));
        private static DependencyProperty DisplayedCommentTextProperty = DependencyProperty.Register("DisplayedCommentText", typeof(string), typeof(ProgressWindow));

        public string DisplayedProgressText
        {
            get
            {
                return (string)GetValue(DisplayedProgressTextProperty);
            }
            set
            {
                SetValue(DisplayedProgressTextProperty, value);
            }
        }

        public string DisplayedTitleText
        {
            get
            {
                return (string)GetValue(DisplayedTitleTextProperty);
            }
            set
            {
                SetValue(DisplayedTitleTextProperty, value);
            }
        }

        public string DisplayedCommentText
        {
            get
            {
                return (string)GetValue(DisplayedCommentTextProperty);
            }
            set
            {
                SetValue(DisplayedCommentTextProperty, value);
            }
        }

        public bool DisplayPercents
        {
            get;
            set;
        } = true;

        public bool IsIndereminate
        {
            get => Bar.IsIndeterminate;
            set
            {
                Bar.IsIndeterminate = value;
                if (Bar.IsIndeterminate)
                    AssignText();
            }
        }
        

        public ProgressWindow(bool indeterminate = false, int maximum = 100, int initial = 50)
        {
            InitializeComponent();
            DataContext = this;
            Bar.Minimum = 0;
            Bar.Maximum = maximum;
            Bar.IsIndeterminate = indeterminate;
            SetValue(initial);

           
        }

        private void AssignText()
        {
            if (!IsIndereminate)
            {
                if (DisplayPercents)
                    DisplayedProgressText = String.Format("{0} %", (int)Math.Floor(100 * (Bar.Value - Bar.Minimum) / (Bar.Maximum - Bar.Minimum)));
                else
                    DisplayedProgressText = String.Format("{0} / {1}", Bar.Value, Bar.Maximum);
            }
            else
            {
                DisplayedProgressText = "";
            }
        }

        public void SetValue(int val)
        {
            Bar.Value = val;
            AssignText();
        }

        public void IncrementStep()
        {
            Bar.Value += 1;
            AssignText();
        }
        
    }
}
