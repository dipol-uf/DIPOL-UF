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
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DIPOL_UF.UserNotifications;
using Microsoft.Extensions.Logging;
using Serializers;
using ILogger = Serilog.ILogger;

namespace DIPOL_UF.Jobs
{
    internal sealed partial class JobManager
    {
        internal class Job
        {
            private readonly IUserNotifier _notifier;
            private readonly ILoggerFactory _loggerFactory;
            private readonly List<JobAction> _actions;

            public ReadOnlyCollection<JobAction> Actions => _actions.AsReadOnly();

            public Job(ReadOnlyDictionary<string, object> input, IUserNotifier notifier, ILoggerFactory loggerFactory)
            {
                if (input is null)
                    throw new ArgumentNullException(nameof(input));
                _notifier = notifier ?? throw new ArgumentNullException(nameof(notifier));
                _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));

                _actions = input.ContainsKey("Actions")
                    ? (input["Actions"] as object[])
                      ?.Select(x => x is ReadOnlyDictionary<string, object> {Count: 1} dict
                          ? dict.FirstOrDefault()
                          : new KeyValuePair<string, object>())
                      .Select(ItemToJob).ToList()
                    : new List<JobAction>(0);
            }


            private JobAction ItemToJob(KeyValuePair<string, object> obj)
            {
              
                var name = obj.Key.ToLowerInvariant();
                if (name.StartsWith(@"motor") && obj.Value is string motorStr)
                    return new MotorAction(motorStr, _notifier, _loggerFactory.CreateLogger<MotorAction>());
                if (name.StartsWith(@"camera") && obj.Value is string camStr)
                    return new CameraAction(camStr);
                if(name.StartsWith(@"shutter") && obj.Value is string shutterStr)
                    return new ShutterAction(shutterStr);
                if(name.StartsWith(@"delay") && obj.Value is string delayStr)
                    return new DelayAction(delayStr);
                if (name.StartsWith(@"settings") && obj.Value is string settsStr)
                    return new SettingsAction(settsStr);
                if (name.StartsWith(@"repeat") && obj.Value is ReadOnlyDictionary<string, object> innerActions)
                {
                    var list = (innerActions["Actions"] as object[])
                               ?.Select(x => x is ReadOnlyDictionary<string, object> {Count: 1} d
                                   ? ItemToJob(d.First())
                                   : null).ToList();

                    return list?.Count != 0
                        ? new RepeatAction(
                            list,
                            innerActions.TryGetValue("Repeats", out var tempVal)
                                ? (int) Convert.ChangeType(tempVal, TypeCode.Int32)
                                : 1)
                        : null;
                }

                // Modified motor
                if (name.StartsWith(@"motor") && obj.Value is IReadOnlyDictionary<string, object> props)
                {
                    return new MotorAction(props, _notifier, _loggerFactory.CreateLogger<MotorAction>());
                }
                
                return null;
            }


            public async Task Initialize(CancellationToken token)
            {
                foreach (var item in _actions)
                    await item.Initialize(token);
            }

            public async Task Run(CancellationToken token)
            {
                foreach (var action in _actions)
                    await action.Execute(token);
            }

            public bool ContainsActionOfType<T>() where T : JobAction
                => _actions.Any(x => x.ContainsActionOfType<T>());

            public int NumberOfActions<T>() where T : JobAction
                => _actions.Select(x => x.NumberOfActions<T>()).Sum();

        }
    }
}