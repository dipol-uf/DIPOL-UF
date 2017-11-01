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
using System.Windows.Navigation;
using System.Windows.Shapes;



namespace DIPOL_UF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class DipolMainWindow : Window
    {
      
        public DipolMainWindow()
        {
            InitializeComponent();
            Loaded += (sender, e) => {
                var mdl = new Models.ProgressBar();
                var vm = new ViewModels.ProgressBarViewModel(mdl);
                var wind = new Views.ProgressWindow(vm);
                mdl.BarTitle = "Test Title";
                mdl.BarComment = "Test Comment";
                mdl.DisplayPercents = false;
                mdl.Minimum = -123;
                mdl.Value = 68;

                wind.Show();
            };
        }
    }
}
