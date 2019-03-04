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
using System.IO;
using SettingsManager;
using StreamReader = System.IO.StreamReader;

namespace DIPOL_UF
{
    /// <summary>
    /// 
    /// </summary>
    public class UiSettingsProvider
    {
        private static TimeSpan _uiThrottlingDelay = default;
        /// <summary>
        /// 
        /// </summary>
        private const string Path = "ui-config.json";

        /// <summary>
        /// 
        /// </summary>
        public static JsonSettings Settings;

        public static TimeSpan UiThrottlingDelay
        {
            get
            {
                if (_uiThrottlingDelay == default)
                    _uiThrottlingDelay =
                        TimeSpan.Parse(Settings.Get("UiThrottlingDelay", "00:00:00.100"));

                return _uiThrottlingDelay;
            }
        }

        static UiSettingsProvider()
        {
            if (File.Exists(Path))
                using (var str = new StreamReader(Path))
                    Settings = new JsonSettings(str.ReadToEnd());
            else
                Settings = new JsonSettings();
        }
    }
}