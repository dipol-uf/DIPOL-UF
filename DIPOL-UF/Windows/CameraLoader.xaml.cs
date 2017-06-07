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

using ANDOR_CS.Classes;
using ANDOR_CS.Exceptions;

namespace DIPOL_UF.Windows
{
    /// <summary>
    /// Interaction logic for CamerasLoader.xaml
    /// </summary>
    public partial class CameraLoader : Window
    {
        private const int TimeOut = 10000;

        private Camera[] DetectedCameras;

        public CameraLoader(ref Camera[] cameras)
        {
            InitializeComponent();

            DetectedCameras = cameras;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //int n = Camera.GetNumberOfCameras();

            //if (n < 1)
            //{
            //    MessageBox.Show(
            //        this,
            //        "Make sure your camera is properly connected and drivers are up to date.",
            //        "No ANDOR - compatible cameras detected.",
            //        MessageBoxButton.OK,
            //        MessageBoxImage.Error,
            //        MessageBoxResult.OK);
            //    Close();
            //}

            //var progress = new ProgressWindow(false, n, 0);
            //progress.DisplayedTitleText = "Checking available cameras...";
            //progress.DisplayPercents = false;
            //progress.IsIndereminate = true;

            //var task = Task<List<Camera>>.Run<List<Camera>>(() => LoadCameras(n, progress));

            //progress.ShowDialog();                       

            //task.Wait();


            //List<Camera> cams = task.Result;


            //if (cams.Count < 1)
            //{
            //    MessageBox.Show(
            //       this,
            //       $"We detected at least {n} cameras, connected to this computer, but were unable to connect to any of these. Make sure no other software is currently using these cameras.",
            //       $"None of {n} detected cameras are responding.",
            //       MessageBoxButton.OK,
            //       MessageBoxImage.Error,
            //       MessageBoxResult.OK);

            //    Close();
            //}

            //foreach (
            //    var cameraEntry
            //    in
            //        from camera
            //        in cams
            //        select new ListBoxItem()
            //        {
            //            Content = $"{camera.Capabilities.CameraType} - {camera.CameraModel}"
            //        }
            //    )
            //    CameraList.Items.Add(cameraEntry);



            var source = new System.Collections.ObjectModel.ObservableCollection<string>();
            source.Add("A");
            source.Add("B");
            CameraList.ItemsSource = source;

           

        }

        private List<Camera> LoadCameras(int n, ProgressWindow progress)
        {
           
            List<Camera> result = new List<Camera>();

            
            for (int i = 0; i < n; i++)
            {
                Dispatcher.Invoke(() => progress.DisplayedCommentText = "Checking camera...");

                Camera localCam = null;

                var task = Task<Camera>.Run<Camera>(() => new Camera(i));

                if (task.Wait(TimeOut) && !task.IsFaulted)
                {
                    localCam = task.Result;
                    result.Add(localCam);
                }
                            
                if(i == 0)
                    Dispatcher.Invoke(() => progress.IsIndereminate = false);

                Dispatcher.Invoke(() =>
                {
                    progress.IncrementStep();
                    progress.DisplayedCommentText = localCam == null ? "Camera is unavailable" : $"Found camera {localCam.Capabilities.CameraType} {localCam.CameraModel}";
                });
            }
            Task.Delay(1500).Wait();
            Dispatcher.Invoke(progress.Close);
            return result;

            
        }

        private void CameraList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (sender is ListBox box)
                {
                    
                    
                         var source = new System.Collections.ObjectModel.ObservableCollection<DIPOL_UF.Classes.CameraListItem>();
                    source.Add(new Classes.CameraListItem(1, false, "NAME2"));
                    source.Add(new Classes.CameraListItem(2, false, "NAME3"));

                    box.ItemsSource = source;
                    box.UpdateLayout();
                }
            }
        }
    }
}
