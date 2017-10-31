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

namespace DIPOL_UF.Views
{
    /// <summary>
    /// Interaction logic for ProgressWindow.xaml
    /// </summary>
    public partial class ProgressWindow : Window
    {
        private static DependencyProperty DisplayedProgressTextProperty = 
            DependencyProperty.Register("DisplayedProgressText", typeof(string), typeof(ProgressWindow));
        private static DependencyProperty DisplayedTitleTextProperty = 
            DependencyProperty.Register("DisplayedTitleText", typeof(string), typeof(ProgressWindow));
        private static DependencyProperty DisplayedCommentTextProperty = 
            DependencyProperty.Register("DisplayedCommentText", typeof(string), typeof(ProgressWindow));

        private static DependencyProperty DisplayPercentsProperty
            = DependencyProperty.Register("DisplayPercents", typeof(bool), typeof(ProgressWindow));

        private static DependencyProperty IsIndeterminateProperty
            = DependencyProperty.Register("IsIndeterminate", typeof(bool), typeof(ProgressWindow));

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
            get => (bool)GetValue(DisplayPercentsProperty);

            set => SetValue(DisplayPercentsProperty, value);
        }

        public bool IsIndereminate
        {
            get => (bool)GetValue(IsIndeterminateProperty);
            set => SetValue(IsIndeterminateProperty, value);
        } 

        public ProgressWindow()
        {
            InitializeComponent();
            Loaded += ProgressWindow_Loaded;
          
        }

        private void ProgressWindow_Loaded(object sender, RoutedEventArgs e)
        {
            IsIndereminate = true;
            Console.WriteLine(Bar.IsIndeterminate);
        }
    }
}
