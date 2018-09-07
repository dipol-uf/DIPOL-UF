//    This file is part of Dipol-3 Camera Manager.

//     MIT License
//     
//     Copyright(c) 2018 Ilia Kosenkov
//     
//     Permission is hereby granted, free of charge, to any person obtaining a copy
//     of this software and associated documentation files (the "Software"), to deal
//     in the Software without restriction, including without limitation the rights
//     to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//     copies of the Software, and to permit persons to whom the Software is
//     furnished to do so, subject to the following conditions:
//     
//     The above copyright notice and this permission notice shall be included in all
//     copies or substantial portions of the Software.
//     
//     THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//     IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//     FITNESS FOR A PARTICULAR PURPOSE AND NONINFINGEMENT. IN NO EVENT SHALL THE
//     AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//     LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//     OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//     SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using ANDOR_CS.Enums;
using ANDOR_CS.DataStructures;
using ANDOR_CS.Classes;
using ANDOR_CS.Exceptions;

using DIPOL_Remote.Interfaces;

namespace DIPOL_Remote.Classes
{
    public class RemoteSettings : SettingsBase
    {
        private IRemoteControl session;

        [ANDOR_CS.Attributes.NonSerialized]
        public string SessionID
        {
            get;
            private set;
        }

        [ANDOR_CS.Attributes.NonSerialized]
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
            
            if (!RemoteCamera.RemoteCameras.TryGetValue((sessionID, cameraIndex), out var cam))
                throw new Exception();
            Camera = cam;
        }

        public override List<(string Option, bool Success, uint ReturnCode)> ApplySettings(
            out (float ExposureTime, float AccumulationCycleTime, float KineticCycleTime, int BufferSize) timing)
        {
            // Stores byte representation of settings
            byte[] data;

            // Creates MemoryStream and serializes settings into it.
            using (var memStr = new MemoryStream())
            {
                Serialize(memStr);
                
                // Writes stream bytes to array.
                data = memStr.ToArray();
            }

            // Calls remote method.
            var result = session.CallApplySettings(SettingsID, data);

            base.ApplySettings(out _);
            // Assigns out values
            timing = result.Timing;

            return result.Result.ToList();
            
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

        public override void Dispose()
        {
            session.RemoveSettings(SettingsID);
            session = null;
            base.Dispose();
        }
    }
}
