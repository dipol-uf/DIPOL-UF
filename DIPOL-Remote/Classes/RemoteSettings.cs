//    This file is part of Dipol-3 Camera Manager.

//    Dipol-3 Camera Manager is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.

//    Dipol-3 Camera Manager is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//    GNU General Public License for more details.

//    You should have received a copy of the GNU General Public License
//    along with Dipol-3 Camera Manager.  If not, see<http://www.gnu.org/licenses/>.
//
//    Copyright 2017-2018, Ilia Kosenkov, Tuorla Observatory, Finland

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
