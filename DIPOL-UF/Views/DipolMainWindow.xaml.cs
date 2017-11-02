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



namespace DIPOL_UF.Views
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
                wind.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                wind.Owner = this;
                mdl.BarTitle = "Test Title";
                mdl.BarComment = "Test Comment";
                mdl.DisplayPercents = true;
                mdl.Minimum = 0;
                mdl.Maximum = 100;
                mdl.Value = 0;

                Task.Run(() =>
                {
                    Task.Delay(100).Wait();
                    for (int i = 1; i <= 100; i++)
                    {
                        Task.Delay(150).Wait();

                        if (!Dispatcher.Invoke(mdl.TryIncrement))
                            break;
                    }
                    Task.Delay(150).Wait();
                    Dispatcher.Invoke(() => { mdl.IsIndeterminate = true; });
                });

                var result = wind.ShowDialog();
            };
        }
    }
}
