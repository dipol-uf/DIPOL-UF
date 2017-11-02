using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Concurrent;
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

using ANDOR_CS.Classes;

namespace DIPOL_UF.Models
{
    class DipolMainWindow : ObservableObject, IDisposable
    {


        private ObservableCollection<ViewModels.MenuItemViewModel> menuBarItems;


        private ConcurrentDictionary<string, CameraBase> connectedCameras = new ConcurrentDictionary<string, CameraBase>();



        public ObservableCollection<ViewModels.MenuItemViewModel> MenuBarItems
        {
            get => menuBarItems;
            set
            {
                if (value != menuBarItems)
                {
                    menuBarItems = value;
                    RaisePropertyChanged();
                }
            }
        }
        
        public bool IsDisposed
        {
            get;
            private set;
        } = false;


        public DipolMainWindow()
        {
            InitializeMenu();
        }

        private void InitializeMenu()
        {
            MenuBarItems = new ObservableCollection<ViewModels.MenuItemViewModel>()
        {
            new ViewModels.MenuItemViewModel(new MenuItemModel()
                {
                 Header = "_Camera",
                 MenuItems = new ObservableCollection<ViewModels.MenuItemViewModel>()
                 {
                     new ViewModels.MenuItemViewModel(new MenuItemModel()
                     {
                         Header = "Find Available",
                         Command = new Commands.DelegateCommand(
                             ListCameras,
                             (x) => CamerasAvailable()),

                     })
                 }
            })
        };
        }

        private async void ListCameras(object parameter)
        {
            int numLocCameras = Camera.GetNumberOfCameras();

            var pbModel = new ProgressBar()
            {
                Minimum = 0,
                Maximum = numLocCameras,
                Value = 0,
                IsIndeterminate = false,
                DisplayPercents = false,
                BarTitle = "Connecting to local cameras..."
            };

            var pbWindow = new Views.ProgressWindow(new ViewModels.ProgressBarViewModel(pbModel));
                        
            if (parameter is FrameworkElement element)
                if (Helper.FindParentOfType<Window>(element) is Window parent)
                    pbWindow.Owner = parent;
                else
                    pbWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            var worker = Task.Run(() => ConnectToLocalCameras(numLocCameras, pbModel, pbWindow));

            pbWindow.ShowDialog();

            foreach (var camera in await worker)
                connectedCameras.TryAdd($"{camera.CameraModel}{camera.SerialNumber}", camera);


        }

        

        private bool CamerasAvailable()
        {
            try
            {
                return Camera.GetNumberOfCameras() > 0;
            }
            catch (ANDOR_CS.Exceptions.AndorSDKException aEx)
            {
                return false;
            }

        }

        private List<CameraBase> ConnectToLocalCameras(int camNumber, ProgressBar progressReporter, Window dialog)
        {
            if (camNumber < 0)
                throw new ArgumentOutOfRangeException();


            List<CameraBase> cams = new List<CameraBase>();
            CameraBase cam;
            for (int camIndex = 0; camIndex < camNumber; camIndex++)
            {
                if (progressReporter?.IsAborted ?? false)
                {
                    return cams;
                }
                try
                {
                    if ((cam = new Camera(camIndex)) != null)
                    {
                        cams.Add(cam);
                        progressReporter.BarComment = $"Camera #{camIndex}: {cam.CameraModel}[{cam.SerialNumber}]";
                    }
                }
                catch (Exception e)
                {
                    progressReporter.BarComment = $"Failed to connect to Camera #{camIndex}";
                }
                finally
                {
                    progressReporter?.TryIncrement();
                }
            }
            Task.Delay(1500).Wait();
            Application.Current.Dispatcher.Invoke(() => dialog.DialogResult = true);
            return cams;

        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool diposing)
        {
            if (diposing)
            {
                foreach (var cam in connectedCameras)
                {
                    connectedCameras.TryRemove($"{cam.Value.CameraModel}{cam.Value.SerialNumber}", out CameraBase camInstance);
                    camInstance?.Dispose();
                }
                IsDisposed = true;

            }
        }

    }
}
