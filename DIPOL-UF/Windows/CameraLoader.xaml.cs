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

using DIPOL_UF.Classes;

namespace DIPOL_UF.Windows
{
    /// <summary>
    /// Interaction logic for CamerasLoader.xaml
    /// </summary>
    public partial class CameraLoader : Window
    {
        private const int TimeOut = 10000;
  
        private List<Camera> DetectedCameras = new List<Camera>();
        private IEnumerable<int> SelectedCameraIndexes = null;

        public CameraLoader()
        {
            InitializeComponent();           
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InstructionTextBox.Text = "Waiting for cameras...";

            int n = Camera.GetNumberOfCameras();
            
            // DEBUG CHANGES MADE HERE
            n = 3;

            if (n < 1)
            {
                MessageBox.Show(
                    this,
                    "Make sure your camera is properly connected and drivers are up to date.",
                    "No ANDOR-compatible cameras detected.",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error,
                    MessageBoxResult.OK);
                Close();
            }
            else
            {
                var progress = new ProgressWindow(false, n, 0);
                progress.DisplayedTitleText = "Checking available cameras...";
                progress.DisplayPercents = false;
                progress.IsIndereminate = true;

                var task = Task.Run(() => LoadCameras(n, progress));

                progress.ShowDialog();

                task.Wait();

               DetectedCameras = task.Result;


               
                if (DetectedCameras.Where(c => c != null).Count() < 1)
                {

                    // DEBUG CHANGES MADE HERE
                    //MessageBox.Show(
                    //   this,
                    //   $"We detected at least {n} camera(s), connected to this computer, but were unable to connect to any of these. Make sure no other software is currently using these cameras.",
                    //   $"None of {n} detected cameras are responding.",
                    //   MessageBoxButton.OK,
                    //   MessageBoxImage.Error,
                    //   MessageBoxResult.OK);

                    //Close();

                    CameraList.Items.Add(new CameraListItem(1, "Cam 1"));
                    CameraList.Items.Add(new CameraListItem(2, "Cam 2"));
                    CameraList.Items.Add(new CameraListItem(3, "Cam 3"));

                    InstructionTextBox.Text = "Select camera(s) you would like to use. ";

                }
                else
                {
                    foreach (
                        var cameraEntry
                        in
                            from camera
                            in DetectedCameras
                            select  $"{camera.Capabilities.CameraType} - {camera.CameraModel} ({camera.SerialNumber})"                            
                        )
                        CameraList.Items.Add(cameraEntry);

                    InstructionTextBox.Text = "Select camera(s) you would like to use. ";
                }
            }
            
        }

        private List<Camera> LoadCameras(int n, ProgressWindow progress)
        {
           
            List<Camera> result = new List<Camera>();


            for (int i = 0; i < n; i++)
            {
                Dispatcher.Invoke(() => progress.DisplayedCommentText = "Checking camera...");

                Camera localCam = null;

                var task = Task<Camera>.Run<Camera>(() =>
                {
                    Camera cam = null;

                    //try
                    //{
                    //    cam = new Camera(i);
                    //}
                    //catch (Exception e)
                    //{ }

                    return cam;
                }
                );

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

        private void CameraList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox list)
            {
                int n = list.SelectedItems.Count;

                if (n == 0)
                    InstructionTextBox.Text = "No camera is selected.";
                else 
                    InstructionTextBox.Text = $"Selected {n} device(s).";
            }
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedCameraIndexes = CameraList.SelectedItems.Cast<CameraListItem>().Select(item => item.Index - 1).OrderBy((item) => item);

            DialogResult = true;
           
        }

        public IEnumerable<Camera> ShowDialogAndWait()
        {
            var result = ShowDialog();

            try
            {
                if (result ?? false)
                    return from index
                            in SelectedCameraIndexes
                           select DetectedCameras[index];
                else
                    return null;
            }
            finally
            {
                List<Task> nonUsedCamerasCleaning = new List<Task>();

                for (int ind = 0; ind < DetectedCameras.Count; ind++)
                {
                    if (!SelectedCameraIndexes.Contains(ind))
                    {
                       nonUsedCamerasCleaning.Add(Task.Factory.StartNew((locInd) =>DetectedCameras[(int)locInd]?.Dispose(), ind));
                    }
                }

                Owner.Closed += (sender, e) =>
                {
                    Task.WaitAll(nonUsedCamerasCleaning.ToArray());
                };
                
            }
        }


    }
}
