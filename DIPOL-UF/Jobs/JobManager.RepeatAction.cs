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
using System.Linq;
using System.Threading.Tasks;

namespace DIPOL_UF.Jobs
{
    internal sealed partial class JobManager
    {
        private class RepeatAction : JobAction
        {
            private int Repeats { get; }

            private readonly List<JobAction> _actions;

            public RepeatAction(List<JobAction> actions, int repeats)
            {
                // WATCH : Prototyping constructor
                _actions = actions;
                Repeats = repeats;
            }

            public override Task Execute()
            {
                return Task.Run(async () =>
                {
                    Console.WriteLine(App.Current.Dispatcher.CheckAccess() + " In repeat");
                    for (var i = 0; i < Repeats; i++)
                    {
                        Console.WriteLine($@"{DateTime.Now:HH:mm:ss.fff} Repeat block {i:00} starts");
                        foreach (var action in _actions)
                            await action.Execute();
                        Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} Repeat block {i:00} ends\r\n");
                    }
                });

            }

            public override bool ContainsActionOfType<T>()
                => _actions.Any(x => x.ContainsActionOfType<T>());
        }
    }
}