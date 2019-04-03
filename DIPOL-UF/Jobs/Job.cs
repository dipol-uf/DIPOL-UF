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
using System.Threading.Tasks;
using ANDOR_CS.Classes;
using Microsoft.Xaml.Behaviors.Media;
using Serializers;

namespace DIPOL_UF.Jobs
{
    class Job
    {
        public string SettingsPath { get; private set; } = ".";
        
        private readonly List<JobAction> _actions;

        public ReadOnlyCollection<JobAction> Actions => _actions.AsReadOnly();


        private Job(ReadOnlyDictionary<string, object> input)
        {
            if (input is null)
                throw new ArgumentNullException(nameof(input));

            _actions = input.Select(ItemToJob).ToList();
        }
        private JobAction ItemToJob(KeyValuePair<string, object> obj)
        {
            var name = obj.Key.ToLowerInvariant();
            if(name.StartsWith(@"motor") && obj.Value is string motorStr)
                return new MotorAction(motorStr);
            if(name.StartsWith(@"camera") && obj.Value is string camStr)
                return new CameraAction(camStr);
            if (name.StartsWith(@"repeat") && obj.Value is ReadOnlyDictionary<string, object> innerActions)
            {
                var list = (innerActions["Actions"] as object[])
                    ?.Select(x => x is ReadOnlyDictionary<string, object> d && d.Count == 1 
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

            return null;
        }


        public async Task Prepare()
        {
            await Task.Delay(1);
            // TODO : If present, step motor goes to 0
            // TODO : If [MotorAction] in [_actions], but no motor -> throw
            // TODO : Wait for camera acquisitions to finish
            // TODO : Use settings template to create new settings
            // TODO : Update exposure times if necessary
            // TODO : Apply settings
        }

        public async Task Run()
        {
            // TODO : check motor is in 0
            // TODO : check cameras idle

            // TODO : run job

            foreach (var action in _actions)
            {
                await action.Execute();
            }
        }

        public static Job Create(ReadOnlyDictionary<string, object> input, string path = null)
        {
            var job = new Job(input) {SettingsPath = path};
            return job;
        }

        public static Job Create(Stream stream, string path = null)
        {
            if (!stream.CanRead)
                throw new IOException(@"Stream does not support reading.");

            ReadOnlyDictionary<string, object> json = null;
            using (var str = new StreamReader(stream))
                json = JsonParser.ReadJson(str);

            var job = new Job(json) {SettingsPath = path};

            return job;
        }

    }
}
