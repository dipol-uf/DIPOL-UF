using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public string SessionID
        {
            get;
            private set;
        }
        
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
            throw new NotImplementedException();
        }

        public override IEnumerable<(int Index, float Speed)> GetAvailableHSSpeeds()
            => session.GetAvailableHSSpeeds(
                SettingsID,
                ADConverter?.Index ??
                    throw new NullReferenceException($"AD Converter ({nameof(ADConverter)}) is not set."),
                Amplifier?.Amplifier ?? 
                    throw new NullReferenceException($"Output amplifier ({nameof(Amplifier)}) is not set."));
                   
        public override IEnumerable<(int Index, string Name)> GetAvailablePreAmpGain()
            => session.GetAvailablePreAmpGain(SettingsID);

        public override bool IsHSSpeedSupported(int speedIndex, out float speed)
        {
            speed = 0.0f;
            (bool isSupported, float locSpeed) = session.CallIsHSSpeedSupported(SettingsID, speedIndex);
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
