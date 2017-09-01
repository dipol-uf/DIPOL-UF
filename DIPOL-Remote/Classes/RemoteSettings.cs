using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using ANDOR_CS.Enums;
using ANDOR_CS.DataStructures;
using ANDOR_CS.Interfaces;
using ANDOR_CS.Classes;
using ANDOR_CS.Exceptions;

using DIPOL_Remote.Interfaces;

namespace DIPOL_Remote.Classes
{
    public class RemoteSettings : SettingsBase
    {
        private IRemoteControl session;

        [ANDOR_CS.Attributes.NotSerializeProperty]
        public string SessionID
        {
            get;
            private set;
        }

        [ANDOR_CS.Attributes.NotSerializeProperty]
        internal string SettingsID
        {
            get;
            private set;
        }

        internal RemoteSettings(string sessionID, int cameraIndex, string settingsID, IRemoteControl session)
        {
            SessionID = sessionID;
               SettingsID = settingsID;
            this.session = session;

            if (!RemoteCamera.RemoteCameras.TryGetValue((sessionID, cameraIndex), out camera))
                throw new Exception();
        }

        public override List<(string Option, bool Success, uint ReturnCode)> ApplySettings(out (float ExposureTime, float AccumulationCycleTime, float KineticCycleTime, int BufferSize) timing)
        {
            byte[] data;

            using (var memStr = new MemoryStream())
            {
                Serialize(memStr, $"RemoteSettings_{camera.CameraModel}/{camera.SerialNumber}");

                data = memStr.ToArray();
            }

            using (var anotherStr = new MemoryStream(data))
                Deserialize(anotherStr);

            timing = (0, 0, 0, 0);

            return new List<(string Option, bool Success, uint ReturnCode)>();
            
        }

        public override IEnumerable<(int Index, float Speed)> GetAvailableHSSpeeds(int ADConverter, int amplifier)
            => session.GetAvailableHSSpeeds(
                SettingsID,
                ADConverter,
                amplifier);
                   
        public override IEnumerable<(int Index, string Name)> GetAvailablePreAmpGain(
            int ADConverter,
            int amplifier,
            int HSSpeed)
            => session.GetAvailablePreAmpGain(
                SettingsID, 
                ADConverter, 
                amplifier,
                HSSpeed);

        public override bool IsHSSpeedSupported(
            int speedIndex, 
            int ADConverter,
            int amplifier,
            out float speed)
        {
            speed = 0.0f;
            (bool isSupported, float locSpeed) = session
                .CallIsHSSpeedSupported(SettingsID, ADConverter, amplifier, speedIndex);
             speed = locSpeed;
            return isSupported;
        }

        //public void SetReadoutMode(ReadMode mode)
        //{
        //    throw new NotImplementedException();
        //}

        //public void SetTriggerMode(TriggerMode mode)
        //{
        //    throw new NotImplementedException();
        //}

        //public void SetVSAmplitude(VSAmplitude amplitude)
        //{
        //    throw new NotImplementedException();
        //}

        //public void SetVSSpeed(int speedIndex)
        //{
        //    throw new NotImplementedException();
        //}

        //public void SetVSSpeed()
        //{
        //    throw new NotImplementedException();
        //}
    }
}
