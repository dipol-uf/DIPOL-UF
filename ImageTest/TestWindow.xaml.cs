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

namespace ImageTest
{
    /// <summary>
    /// Interaction logic for TestWindow.xaml
    /// </summary>
    public partial class TestWindow : Window
    {

        ANDOR_CS.Classes.CameraBase camera = null;
        ImageDisplayLib.Image Im = null;

        public TestWindow(ANDOR_CS.Classes.CameraBase cam)
        {
            InitializeComponent();
            camera = cam;
            
        }
        public TestWindow(ImageDisplayLib.Image im)
        {
            InitializeComponent();
            Im = im;

        }
        private void Cam_NewImageReceived(object sender, ANDOR_CS.Events.NewImageReceivedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (camera.AcquiredImages.TryDequeue(out ImageDisplayLib.Image im))
                    ImageHandle.LoadImage(im, ImageDisplayLib.ImageType.GrayScale16Int);
            });
        }

        public TestWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {if (camera != null)
            {
                camera.NewImageReceived += Cam_NewImageReceived;
                camera.TemperatureStatusChecked += Cam_TemperatureStatusChecked;
            }
            if (Im != null)
                ImageHandle.LoadImage(Im, ImageDisplayLib.ImageType.GrayScale16Int);
            //if (camera == null)
            //{
            //    int N = 4096;
            //    int M = 8192;
            //    UInt16[] arr = new UInt16[N * M];

            //    for (int i = 0; i < N; i++)
            //        for (int j = 0; j < M; j++)
            //            arr[M * i + j] = (UInt16)(60 * (i + j) + 1000);

            //    var im = new ImageDisplayLib.Image(arr, M, N);

            //    var max = im.Max();

            //    ImageHandle.LoadImage(im, ImageDisplayLib.ImageType.GrayScale16Int);
            //}

        }

        private void Cam_TemperatureStatusChecked(object sender, ANDOR_CS.Events.TemperatureStatusEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                TemperatureBlock.Text = $"T = {e.Temperature.ToString("F2")} C";
                StatusBlock.Text = e.Status.ToString();
                if (e.Temperature > 10)
                    TemperatureBlock.Foreground = Brushes.DarkRed;
                else if (e.Temperature > -10)
                    TemperatureBlock.Foreground = Brushes.ForestGreen;
                else
                    TemperatureBlock.Foreground = Brushes.DarkBlue;
            });
        }
    }
}
