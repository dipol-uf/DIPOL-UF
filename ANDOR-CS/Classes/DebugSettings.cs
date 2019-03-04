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
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ANDOR_CS.Classes
{
#if DEBUG
    public class DebugSettings : SettingsBase
    {
        public DebugSettings(DebugCamera camera)
        {
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

        public override IEnumerable<(int Index, float Speed)> GetAvailableHSSpeeds(int adConverter, int amplifier)
        {
            yield return (Index: 0, Speed: 0);
            yield return (Index: 1, Speed: 10);

        }

        public override IEnumerable<(int Index, string Name)> GetAvailablePreAmpGain(int adConverter, int amplifier, int hsSpeed)
        {
            yield return (Index: 0, Name: "Zero");
            yield return (Index: 1, Name: "One");
        }

        public override HashSet<string> SupportedSettings()
        {
            return AllowedSettings();
        }
    }
#endif
}
