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
using ANDOR_CS.Enums;


#if DEBUG

namespace ANDOR_CS.Classes
{
    public class DebugSettings : SettingsBase
    {
        public DebugSettings(DebugCamera camera)
        {
            Console.WriteLine("--> Settings created");


            Camera = camera;
        }

        public override void SetEmCcdGain(int gain)
        {
            
        }

        public override bool IsHSSpeedSupported(int speedIndex, int adConverter, int amplifier, out float speed)
        {
            speed = 1;
            return true;
        }

        public override List<(int Index, float Speed)> GetAvailableHSSpeeds(int adConverter, int amplifier)
            => new List<(int Index, float Speed)>
            {
                (Index: 0, Speed: 0),
                (Index: 1, Speed: 10)
            };

        public override List<(int Index, string Name)> GetAvailablePreAmpGain(int adConverter, int amplifier,
            int hsSpeed)
            => new List<(int Index, string Name)>
            {
                (Index: 0, Name: "Zero"),
                (Index: 1, Name: "One")
            };

        public override (int Low, int High) GetEmGainRange()
        {
            // Limit functionality to only default regime
            // (with limits from 0 to 255 as stated in the manual)
            CheckCamera();

            if (!Camera.Capabilities.SetFunctions.HasFlag(SetFunction.EMCCDGain)
                || Camera.Capabilities.CameraType == CameraType.Clara
                || Camera.Properties.OutputAmplifiers.All(x => x.OutputAmplifier != OutputAmplification.ElectronMultiplication))
                throw new NotSupportedException("EM CCD gain control is not supported.");
            
            return (0, 255);
        }

        public override HashSet<string> SupportedSettings()
        {
            return AllowedSettings();
        }

        public override void Dispose()
        {
            base.Dispose();
            Console.WriteLine("--> Settings disposed");
        }
    }
}
#endif
