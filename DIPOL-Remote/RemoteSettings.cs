//    This file is part of Dipol-3 Camera Manager.

//     MIT License
//     
//     Copyright(c) 2018-2019 Ilia Kosenkov
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
using ANDOR_CS.Classes;

namespace DIPOL_Remote
{
    public class RemoteSettings : SettingsBase
    {
        private DipolClient _client;

      
        [ANDOR_CS.Attributes.NonSerialized]
        internal string SettingsID
        {
            get;
            private set;
        }

        // ReSharper disable once SuggestBaseTypeForParameter
        internal RemoteSettings(RemoteCamera cam, string settingsID, DipolClient client)
        {
            SettingsID = settingsID;
            _client = client;
            
            Camera = cam;
        }

        // TODO: Migrate to [Camera]
        //public override List<(string Option, bool Success, uint ReturnCode)> ApplySettings(
        //    out (float ExposureTime, float AccumulationCycleTime, float KineticCycleTime, int BufferSize) timing)
        //{
        //    // Stores byte representation of settings
        //    byte[] data;

        //    // Creates MemoryStream and serializes settings into it.
        //    using (var memStr = new MemoryStream())
        //    {
        //        Serialize(memStr);
                
        //        // Writes stream bytes to array.
        //        data = memStr.ToArray();
        //    }

        //    // Calls remote method.
        //    var result = _client.CallApplySettings(SettingsID, data);

        //    base.ApplySettings(out _);
        //    // Assigns out values
        //    timing = result.Timing;

        //    return result.Result.ToList();
            
        //}

        public override List<(int Index, float Speed)> GetAvailableHSSpeeds(int adConverter, int amplifier)
            => _client.GetAvailableHsSpeeds(
                SettingsID,
                adConverter,
                amplifier).ToList();
                   
        public override List<(int Index, string Name)> GetAvailablePreAmpGain(
            int adConverter,
            int amplifier,
            int hsSpeed)
            => _client.GetAvailablePreAmpGain(
                SettingsID, 
                adConverter, 
                amplifier,
                hsSpeed).ToList();

        public override (int Low, int High) GetEmGainRange()
            => _client.CallGetEmGainRange(SettingsID);

        public override bool IsHSSpeedSupported(
            int speedIndex, 
            int adConverter,
            int amplifier,
            out float speed)
        {
            speed = 0.0f;
            var (isSupported, locSpeed) = _client
                .CallIsHsSpeedSupported(SettingsID, adConverter, amplifier, speedIndex);
             speed = locSpeed;
            return isSupported;
        }

        public override void Dispose()
        {
            _client.RemoveSettings(SettingsID);
            _client = null;
            base.Dispose();
        }
    }
}
