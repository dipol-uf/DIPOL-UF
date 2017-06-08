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

using DIPOL_UF.Windows;

using ANDOR_CS.Classes;

namespace DIPOL_UF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class DipolMainWindow : Window
    {
        private List<Camera> ConnectedCameras = null;

        public DipolMainWindow()
        {
            InitializeComponent();

        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {            

            

        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (ConnectedCameras != null)
                foreach (var cam in ConnectedCameras)
                    cam?.Dispose();

            Application.Current.Shutdown();
        }

        private void DeviceControl_LocalDevices_Click(object sender, RoutedEventArgs e)
        {
            var loader = new CameraLoader();
            loader.Owner = this;
            loader.Topmost = true;
            ConnectedCameras = loader.ShowDialogAndWait().ToList();
        }
    }
}
