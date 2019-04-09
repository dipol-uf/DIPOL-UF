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
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using ANDOR_CS.Enums;
using ReactiveUI;

namespace DIPOL_UF.Jobs
{
    internal partial class JobManager
    {
        public class ShutterAction : JobAction
        {
            private static readonly Regex Regex =
                new Regex(@"^(?:shutter/)?(open|close|auto)\s*?(int(?:ernal)?|ext(?:ernal)?|all)?$",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase);

            private ShutterMode? Internal { get; }
            private ShutterMode? External {get;}

            public ShutterAction(string command)
            {
                // Assuming regex produces exactly the amount of groups
                if (command is null)
                    throw new ArgumentNullException(nameof(command));

                var match = Regex.Match(command.ToLowerInvariant());
                if (!match.Success)
                    throw new ArgumentException(@"Shutter command is invalid.", nameof(command));

                ShutterMode action;
                switch (match.Groups[1].Value)
                {
                    case "open":
                        action = ShutterMode.PermanentlyOpen;
                        break;
                    case "close":
                        action = ShutterMode.PermanentlyClosed;
                        break;
                    default:
                        action = ShutterMode.FullyAuto;
                        break;
                }

                if (match.Groups[2].Value.StartsWith("int"))
                    Internal = action;
                else if (match.Groups[2].Value.StartsWith("ext"))
                    External = action;
                else if (match.Groups[2].Value == "all")
                {
                    Internal = action;
                    External = action;
                }
                else
                    throw new ArgumentException(@"Shutter command is invalid.", nameof(command));
            }

            public override Task Execute()
            {
                var tasks = Manager._jobControls.Select(tab =>
                    Task.Run(async () =>
                    {
                        if (!(Internal is null) &&
                            await tab.InternalShutterCommand.CanExecute.FirstAsync())
                            await tab.InternalShutterCommand.Execute(Internal.Value);
                        if (!(External is null) &&
                            await tab.ExternalShutterCommand.CanExecute.FirstAsync())
                            await tab.ExternalShutterCommand.Execute(External.Value);
                    }));

                return Task.WhenAll(tasks);
            }
        }
    }
}
