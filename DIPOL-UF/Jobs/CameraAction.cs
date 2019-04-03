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
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DIPOL_UF.Jobs
{
    class CameraAction : JobAction
    {
        private static readonly Regex Regex =
            new Regex(@"^(?:camera/)?(expose)\s*((?:\s*[0-9]+,?)+)?$",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public List<int> SpecificCameras { get; }


        public CameraAction()
        {
            SpecificCameras = new List<int>(0);
        }

        public CameraAction(string command)
        {

            if (command is null)
                throw new ArgumentNullException(nameof(command));

            var match = Regex.Match(command.ToLowerInvariant());
            if (!match.Success)
                throw new ArgumentException(@"Motor command is invalid.", nameof(command));

           SpecificCameras = match.Groups[2].Value.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries)
                            .Select(x =>
                                int.TryParse(x, NumberStyles.Any, NumberFormatInfo.InvariantInfo, out var y)
                                    ? new int?(y)
                                    : null)
                            .Where(x => !(x is null))
                            .Select(x => x.Value)
                            .ToList();
        }

        public override async Task Execute()
        {
            var info =
                SpecificCameras.Count == 0
                    ? "all"
                    : SpecificCameras.EnumerableToString();

            Console.WriteLine($@"{DateTime.Now:HH:mm:ss.fff} Cameras ({info}) start exposure");
            await Task.Delay(TimeSpan.FromMilliseconds(2500));
            Console.WriteLine($@"{DateTime.Now:HH:mm:ss.fff} Cameras ({info}) finish exposure");
        }
    }
}
