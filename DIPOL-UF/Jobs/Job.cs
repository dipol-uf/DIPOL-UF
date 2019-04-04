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
using System.Threading.Tasks;
using System.Windows.Automation.Peers;
using Serializers;

namespace DIPOL_UF.Jobs
{
    class Job
    {
        private readonly List<JobAction> _actions;

        public ReadOnlyCollection<JobAction> Actions => _actions.AsReadOnly();


        private Job(ReadOnlyDictionary<string, object> input)
        {
            if (input is null)
                throw new ArgumentNullException(nameof(input));

            _actions = input.ContainsKey("Actions")
                ? (input["Actions"] as object[])
                  ?.Select(x => x is ReadOnlyDictionary<string, object> dict
                                && dict?.Count == 1
                      ? dict.FirstOrDefault()
                      : new KeyValuePair<string, object>())
                  .Select(ItemToJob).ToList()
                : new List<JobAction>(0);
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

        public static Job Create(ReadOnlyDictionary<string, object> input)
        {
            var job = new Job(input);
            return job;
        }

        public static Job Create(Stream stream)
        {
            if (!stream.CanRead)
                throw new IOException(@"Stream does not support reading.");

            ReadOnlyDictionary<string, object> json;
            using (var str = new StreamReader(stream, Encoding.ASCII, true, 512, true))
                json = JsonParser.ReadJson(str);

            var job = new Job(json);

            return job;
        }

    }
}
