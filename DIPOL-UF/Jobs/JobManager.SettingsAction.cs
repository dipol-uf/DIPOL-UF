//    This file is part of Dipol-3 Camera Manager.
//
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
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ANDOR_CS.Classes;
using DIPOL_UF.Properties;

namespace DIPOL_UF.Jobs
{
    internal partial class JobManager
    {
        private class SettingsAction : JobAction
        {
            private static string[] SupportedSettings = {
                @"exposuretime"
            };
            private static readonly Regex Regex =
                    new Regex(@"^(?:settings\/)?(set|reset)\s*?(?:(\w+)\s?(.*))?$",
                        RegexOptions.Compiled | RegexOptions.IgnoreCase);


            private bool IsReset { get; }
            private string SettingsName { get; }
            private object ReplacingValue { get; }

            public SettingsAction(string command)
            {
                // Assuming regex produces exactly the amount of groups
                if (command is null)
                    throw new ArgumentNullException(nameof(command));

                var match = Regex.Match(command.ToLowerInvariant());
                if (!match.Success)
                    throw new ArgumentException(@"Settings command is invalid.", nameof(command));

                IsReset = match.Groups[1].Value != @"set";

                SettingsName = SupportedSettings.Contains(match.Groups[2].Value)
                    ? match.Groups[2].Value
                    : throw new ArgumentException(@"Settings command is invalid.", nameof(command));

                if (!IsReset && String.IsNullOrWhiteSpace(match.Groups[3].Value))
                    throw new ArgumentException(@"Settings command is invalid.", nameof(command));
                
                // INFO : Currently support only exposure time, entered manually
                if (SettingsName == "exposuretime" &&
                    float.TryParse(match.Groups[3].Value, NumberStyles.Any,
                        NumberFormatInfo.InvariantInfo, out var expVal))
                    ReplacingValue = expVal;
                else throw new ArgumentException(@"Settings command is invalid.", nameof(command));

            }

            public override Task Execute()
            {
                throw new NotImplementedException();
            }
        }
    }
}
