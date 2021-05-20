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

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace DIPOL_UF.Jobs
{
    internal class Target
    {
        public string SettingsPath { get; set; }
        public string JobPath { get; set; }
        public string BiasPath { get; set; }
        public string DarkPath { get; set; }
        public string TargetName { get; set; }
        [JsonIgnore]
        public int Repeats { get; set; }

        public Dictionary<string, Dictionary<string, object>> CustomParameters { get; set; }

        private void InitializeFromAnother(Target other)
        {
            SettingsPath = other.SettingsPath;
            JobPath = other.JobPath;
            TargetName = other.TargetName;
            BiasPath = other.BiasPath;
            DarkPath = other.DarkPath;
            Repeats = other.Repeats;
        }

        public async Task Deserialize(Stream stream)
        {
            if(!stream.CanRead)
                throw new IOException("Stream does not support reading.");
            // WATCH : using [leaveOpen = true] in conjunction with [using] closure
            using (var reader = new StreamReader(stream, Encoding.ASCII, true, 512, true))
            {
                var str = await reader.ReadToEndAsync();
                var json = JsonConvert.DeserializeObject<Target>(str);
                InitializeFromAnother(json);
            }
        }

        public async Task Serialize(Stream stream)
        {
            if (!stream.CanWrite)
                throw new IOException("Stream does not support writing.");
            // WATCH : using [leaveOpen = true] in conjunction with [using] closure
            using (var writer = new StreamWriter(stream, Encoding.ASCII, 512, true))
            {
                var str = JsonConvert.SerializeObject(this, Formatting.Indented, new JsonSerializerSettings()
                {
                    NullValueHandling = NullValueHandling.Ignore
                });
                await writer.WriteAsync(str);
            }
        }
    }
}
