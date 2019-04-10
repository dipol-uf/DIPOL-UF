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
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace DIPOL_UF.Jobs
{
    internal partial class JobManager
    {
        private class DelayAction : JobAction
        {
            private static readonly Regex Regex =
                new Regex(@"^(?:delay/)?(?:wait)\s*?((?:[0-9]{1,2}:){0,2}[0-9]+\.?[0-9]*)?$",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase);

            private TimeSpan Delay { get; }
            public DelayAction(string command)
            {
                // Assuming regex produces exactly the amount of groups
                if (command is null)
                    throw new ArgumentNullException(nameof(command));

                var match = Regex.Match(command.ToLowerInvariant());
                if (!match.Success)
                    throw new ArgumentException(@"Delay command is invalid.", nameof(command));

                var delayStr = match.Groups[1].Value;
                if(string.IsNullOrWhiteSpace(delayStr))
                    Delay = TimeSpan.Zero;
                if (int.TryParse(delayStr, NumberStyles.Any, NumberFormatInfo.InvariantInfo, out var ms))
                    Delay = TimeSpan.FromMilliseconds(ms);
                else if (TimeSpan.TryParse(delayStr, DateTimeFormatInfo.InvariantInfo, out var delay))
                    Delay = delay;
                else
                    throw new ArgumentException(@"Delay command is invalid.", nameof(command));

            }

            public override Task Execute(CancellationToken token)
                => Task.Delay(Delay, token);
        }
    }
}
