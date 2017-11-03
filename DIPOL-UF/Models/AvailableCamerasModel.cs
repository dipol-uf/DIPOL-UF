using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Threading.Tasks;

using ANDOR_CS.Classes;
using ANDOR_CS.Exceptions;


namespace DIPOL_UF.Models
{
    class AvailableCamerasModel : ObservableObject
    {
        private ObservableConcurrentDictionary<string, CameraBase> foundCameras = new ObservableConcurrentDictionary<string, CameraBase>();
        private List<string> selectedItems = new List<string>();

        private Commands.DelegateCommand selectionChangedCommand;
        private Commands.DelegateCommand cancelCommand;
        private Commands.DelegateCommand connectCommand;
        private Commands.DelegateCommand connactAllCommand;

        public ObservableConcurrentDictionary<string, CameraBase> FoundCameras => foundCameras;

       
        public Commands.DelegateCommand SelectionChangedCommand
        {
            get => selectionChangedCommand;
            set
            {
                if (value != selectionChangedCommand)
                {
                    selectionChangedCommand = value;
                    RaisePropertyChanged();
                }
            }
        }

        public AvailableCamerasModel()
        {
            InitializeCommands();

            foundCameras.PropertyChanged += (sender, e) => Helper.WriteLog(e.PropertyName);
            Task.Run(() =>
            {
                try
                {
                    QueryLocalCameras();
                    QueryRemoteCameras();
                }
                catch (Exception e)
                {
                    Helper.WriteLog(e.Message);
                }
            });
        }

        private void InitializeCommands()
        {
            
            SelectionChangedCommand = new Commands.DelegateCommand(
                SelectionChangedHandler,
                (param) => true);
            
        }

        private void QueryLocalCameras()
        {
            int nCams;
            try
            {
                nCams = Camera.GetNumberOfCameras();
            }
            catch (AndorSDKException aExp)
            {
                Helper.WriteLog(aExp);
                nCams = 0;
            }

            nCams = 10;


            for (int camIndex = 0; camIndex < nCams; camIndex++)
            {
                CameraBase cam = null;
                try
                {
                    //cam = new Camera(camIndex);
                    cam = Camera.GetDebugInterface(camIndex);
                }
                catch (AndorSDKException aExp)
                {
                    Helper.WriteLog(aExp);
                }

                if (cam != null)
                    foundCameras.TryAdd($"{cam.CameraIndex}:{cam.CameraModel}:{cam.SerialNumber}", cam);

            }
        }
        private void QueryRemoteCameras()
        {
        }

        private void SelectionChangedHandler(object parameter)
        {
            if (parameter is Commands.EventCommandArgs<SelectionChangedEventArgs> commandPar)
            {
                foreach (var remItem in commandPar.EventArgs.RemovedItems)
                    if (remItem is KeyValuePair<string, CameraBase> rawItem)
                        selectedItems.RemoveAll(x => x == rawItem.Key);

                foreach (var addItem in commandPar.EventArgs.AddedItems)
                    if (addItem is KeyValuePair<string, CameraBase> rawItem)
                        selectedItems.Add(rawItem.Key);
            }
        }
    }
}
