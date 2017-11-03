using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ANDOR_CS.Classes;
using ANDOR_CS.Exceptions;


namespace DIPOL_UF.Models
{
    class AvailableCamerasModel : ObservableObject
    {
        private ObservableConcurrentDictionary<string, CameraBase> foundCameras = new ObservableConcurrentDictionary<string, CameraBase>();

        public ObservableConcurrentDictionary<string, CameraBase> FoundCameras => foundCameras;

        public AvailableCamerasModel()
        {
            foundCameras.PropertyChanged += (sender, e) => Helper.WriteLog(e.PropertyName);
            Task.Run(() =>
            {
                try
                {
                    QueryLocalCameras();
                    QueryRemoteCameras();
                }
                catch (Exception e)
                { }
            });
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

            nCams = 26;


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
                    Helper.WriteLog(foundCameras.TryAdd($"{camIndex}:{cam.CameraModel}:{cam.SerialNumber}", cam).ToString());

            }
        }
        private void QueryRemoteCameras()
        {
        }
    }
}
