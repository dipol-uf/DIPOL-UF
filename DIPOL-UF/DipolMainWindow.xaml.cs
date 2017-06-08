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
using DIPOL_UF.EventArgs;

using ANDOR_CS.Classes;

namespace DIPOL_UF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class DipolMainWindow : Window
    {
        private List<Camera> ConnectedLocalCameras = null;

        public delegate void CameraEventHandler(object sender, System.EventArgs e);

        public event CameraEventHandler LocalCameraLoaded;
        
        protected virtual void OnLocalCameraLoaded(Camera sender, CameraEventArgs e) => LocalCameraLoaded?.Invoke(sender, e);
               
        private void Window_Closed(object sender, System.EventArgs e)
        {
            if (ConnectedLocalCameras != null)
                foreach (var cam in ConnectedLocalCameras)
                    cam?.Dispose();

            Application.Current.Shutdown();
        }

        private void DeviceControl_LocalDevices_Click(object sender, RoutedEventArgs e)
        {
            var loader = new CameraLoader();
            loader.Owner = this;
            loader.Topmost = true;
            ConnectedLocalCameras = loader.ShowDialogAndWait()?.ToList();

            if (ConnectedLocalCameras != null)
                foreach (var newCam in ConnectedLocalCameras)
                    OnLocalCameraLoaded(newCam, new CameraEventArgs());
        }



        public DipolMainWindow()
        {
            InitializeComponent();
            LocalCameraLoaded += (s, e) =>
             {
                 if (s is Camera cam && cam.Capabilities.Features.HasFlag(ANDOR_CS.Enums.SDKFeatures.FanControl | ANDOR_CS.Enums.SDKFeatures.LowFanMode))
                 {
                     cam.FanControl(ANDOR_CS.Enums.FanMode.LowSpeed);
                     Console.WriteLine($"Camera {cam.SerialNumber} fan slowed down.");
                 }
                 else
                     Console.WriteLine($"Not suported.");

             };
        }
    }
}
