using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ANDOR_CS.Enums;
using ANDOR_CS.DataStructures;
using ANDOR_CS.Interfaces;

namespace DIPOL_Remote.Classes
{
    public class RemoteSettings : ISettings
    {
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
        public string SettingsID
        {
            get;
            private set;
        }

        internal RemoteSettings(string sessionID, int cameraIndex, string settingsID)
        {
            SessionID = sessionID;
            CameraIndex = cameraIndex;
            SettingsID = settingsID;
        }

        (int Index, float Speed)? ISettings.VSSpeed => throw new NotImplementedException();

        (int Index, float Speed)? ISettings.HSSpeed => throw new NotImplementedException();

        (int Index, int BitDepth)? ISettings.ADConverter => throw new NotImplementedException();

        VSAmplitude? ISettings.VSAmplitude => throw new NotImplementedException();

        (string Name, OutputAmplification Amplifier, int Index)? ISettings.Amplifier => throw new NotImplementedException();

        (int Index, string Name)? ISettings.PreAmpGain => throw new NotImplementedException();

        AcquisitionMode? ISettings.AcquisitionMode => throw new NotImplementedException();

        ReadMode? ISettings.ReadMode => throw new NotImplementedException();

        TriggerMode? ISettings.TriggerMode => throw new NotImplementedException();

        float? ISettings.ExposureTime => throw new NotImplementedException();

        Rectangle? ISettings.ImageArea => throw new NotImplementedException();

        (int Frames, float Time)? ISettings.AccumulateCycle => throw new NotImplementedException();

        (int Frames, float Time)? ISettings.KineticCycle => throw new NotImplementedException();

        int? ISettings.EMCCDGain => throw new NotImplementedException();

        List<(string Option, bool Success, uint ReturnCode)> ISettings.ApplySettings(out (float ExposureTime, float AccumulationCycleTime, float KineticCycleTime, int BufferSize) timing)
        {
            throw new NotImplementedException();
        }

        IEnumerable<(int Index, float Speed)> ISettings.GetAvailableHSSpeeds()
        {
            throw new NotImplementedException();
        }

        IEnumerable<(int Index, string Name)> ISettings.GetAvailablePreAmpGain()
        {
            throw new NotImplementedException();
        }

        void ISettings.SetAccumulationCycle(int number, float time)
        {
            throw new NotImplementedException();
        }

        void ISettings.SetAcquisitionMode(AcquisitionMode mode)
        {
            throw new NotImplementedException();
        }

        void ISettings.SetADConverter(int converterIndex)
        {
            throw new NotImplementedException();
        }

        void ISettings.SetEMCCDGain(int gain)
        {
            throw new NotImplementedException();
        }

        void ISettings.SetExposureTime(float time)
        {
            throw new NotImplementedException();
        }

        void ISettings.SetHSSpeed(int speedIndex)
        {
            throw new NotImplementedException();
        }

        void ISettings.SetImageArea(Rectangle area)
        {
            throw new NotImplementedException();
        }

        void ISettings.SetKineticCycle(int number, float time)
        {
            throw new NotImplementedException();
        }

        void ISettings.SetOutputAmplifier(OutputAmplification amplifier)
        {
            throw new NotImplementedException();
        }

        void ISettings.SetPreAmpGain(int gainIndex)
        {
            throw new NotImplementedException();
        }

        void ISettings.SetReadoutMode(ReadMode mode)
        {
            throw new NotImplementedException();
        }

        void ISettings.SetTriggerMode(TriggerMode mode)
        {
            throw new NotImplementedException();
        }

        void ISettings.SetVSAmplitude(VSAmplitude amplitude)
        {
            throw new NotImplementedException();
        }

        void ISettings.SetVSSpeed(int speedIndex)
        {
            throw new NotImplementedException();
        }

        void ISettings.SetVSSpeed()
        {
            throw new NotImplementedException();
        }
    }
}
