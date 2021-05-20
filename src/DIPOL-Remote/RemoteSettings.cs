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

using System.Collections.Generic;
using System.Linq;
using ANDOR_CS.Classes;

namespace DIPOL_Remote
{
    public class RemoteSettings : SettingsBase
    {
        private IControlClient _client;
        
        internal string SettingsID
        {
            get;
            private set;
        }

        // ReSharper disable once SuggestBaseTypeForParameter
        internal RemoteSettings(Camera cam, string settingsID, IControlClient client)
        {
            SettingsID = settingsID;
            _client = client;
            
            Camera = cam;
        }

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

        protected sealed override SettingsBase MakeEmptyCopy()
            => new RemoteSettings(Camera as RemoteCamera, 
                // Here [CallMakeCopy] makes a full server-side copy, but returns empty client side copy.
                // It should be used only in conjunction with [this.MakeCopy], otherwise behavior is undefined
                _client.CallMakeCopy(SettingsID), 
                _client);
        

    }
}
