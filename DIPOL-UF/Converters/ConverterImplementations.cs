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

using System.Linq;
using ANDOR_CS.Classes;
using Newtonsoft.Json.Linq;

namespace DIPOL_UF.Converters
{
    internal static class ConverterImplementations
    {
        public static string CameraToStringAliasConversion(CameraBase cam)
        {
            var camString = $"{cam.CameraModel}_{cam.SerialNumber}";
            return UiSettingsProvider.Settings
                                     .GetArray<JToken>(@"CameraDescriptors")
                                     .Where(x => x.Value<string>(@"Name") == camString)
                                     .Select(x => x.Value<string>(@"Alias"))
                                     .DefaultIfEmpty(camString)
                                     .FirstOrDefault();

        }
    }
}
