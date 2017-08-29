using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ANDOR_CS.Enums;
using ANDOR_CS.DataStructures;
using ANDOR_CS.Interfaces;
using ANDOR_CS.Classes;

using DIPOL_Remote.Interfaces;

namespace DIPOL_Remote.Classes
{
    public class RemoteSettings : ISettings
    {
        private CameraBase camera = null;
        private IRemoteControl session;

        public string SessionID
        {
            get;
            private set;
        }
        public int CameraIndex
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
            CameraIndex = cameraIndex;
            SettingsID = settingsID;

            if (!RemoteCamera.RemoteCameras.TryGetValue((sessionID, cameraIndex), out camera))
                throw new Exception();
        }

        public (int Index, float Speed)? VSSpeed { get; private set; } = null;
        public (int Index, float Speed)? HSSpeed { get; private set; } = null;
        public (int Index, int BitDepth)? ADConverter { get; private set; } = null;
        public VSAmplitude? VSAmplitude { get; private set; } = null;
        public (string Name, OutputAmplification Amplifier, int Index)? Amplifier { get; private set; } = null;
        public (int Index, string Name)? PreAmpGain { get; private set; } = null;
        public AcquisitionMode? AcquisitionMode { get; private set; } = null;
        public ReadMode? ReadMode { get; private set; } = null;
        public TriggerMode? TriggerMode { get; private set; } = null;
        public float? ExposureTime { get; private set; } = null;
        public Rectangle? ImageArea { get; private set; } = null;
        public (int Frames, float Time)? AccumulateCycle { get; private set; } = null;
        public (int Frames, float Time)? KineticCycle { get; private set; } = null;
        public int? EMCCDGain { get; private set; } = null;
        
        public List<(string Option, bool Success, uint ReturnCode)> ApplySettings(out (float ExposureTime, float AccumulationCycleTime, float KineticCycleTime, int BufferSize) timing)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<(int Index, float Speed)> GetAvailableHSSpeeds()
            => session.GetAvailableHSSpeeds(SettingsID);

        public IEnumerable<(int Index, string Name)> GetAvailablePreAmpGain()
            => session.GetAvailablePreAmpGain(SettingsID);

        public void SetAccumulationCycle(int number, float time)
        {
            throw new NotImplementedException();
        }

        public void SetAcquisitionMode(AcquisitionMode mode)
        {
            throw new NotImplementedException();
        }

        public void SetADConverter(int converterIndex)
        {
            throw new NotImplementedException();
        }

        public void SetEMCCDGain(int gain)
        {
            throw new NotImplementedException();
        }

        public void SetExposureTime(float time)
        {
            throw new NotImplementedException();
        }

        public void SetHSSpeed(int speedIndex)
        {
            throw new NotImplementedException();
        }

        public void SetImageArea(Rectangle area)
        {
            throw new NotImplementedException();
        }

        public void SetKineticCycle(int number, float time)
        {
            throw new NotImplementedException();
        }

        public void SetOutputAmplifier(OutputAmplification amplifier)
        {
            throw new NotImplementedException();
        }

        public void SetPreAmpGain(int gainIndex)
        {
            throw new NotImplementedException();
        }

        public void SetReadoutMode(ReadMode mode)
        {
            throw new NotImplementedException();
        }

        public void SetTriggerMode(TriggerMode mode)
        {
            throw new NotImplementedException();
        }

        public void SetVSAmplitude(VSAmplitude amplitude)
        {
            throw new NotImplementedException();
        }

        public void SetVSSpeed(int speedIndex)
        {
            throw new NotImplementedException();
        }

        public void SetVSSpeed()
        {
            throw new NotImplementedException();
        }
    }
}
