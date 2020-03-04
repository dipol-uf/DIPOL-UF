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
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Compatibility.ITuple;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Serializers
{
    public static class JsonParser
    {
        // WATCH: NOT-HotFixing here
        internal static object Converter(object input, bool convertAll = false)
        {
            static string[] FlagEnumConverter(Enum @enum)
            {
                
                var type = @enum.GetType();
                //var values = Enum.GetValues(type).OfType<Enum>().ToList();
                //var values = type.GetFields(BindingFlags.Public | BindingFlags.Static)
                //    .Where(f => f.FieldType == type)
                //    .Select(f => (Enum) f.GetValue(null)).ToList();
                var values = type.GetFields(BindingFlags.Public | BindingFlags.Static).Where(f => f.FieldType == type)
                    //.Select(f => new {FieldInfo = f, Ignore = f.GetCustomAttributes().Select(x => (x.TypeId as Type)?.Name ?? string.Empty).Any(x => x == "IgnoreDefaultAttribute")})
                    .Select(f => new {FieldInfo = f, Ignore = f.GetCustomAttribute<IgnoreDefaultAttribute>() is { }})
                    .Where(x => !x.Ignore)
                    .Select(x => (Enum)x.FieldInfo.GetValue(null))
                    .ToList();

                var flagVals = values.Where(@enum.HasFlag).Select(x => Enum.GetName(type, x)).ToArray();
                return flagVals;
            }

            var tempResult = input;

            if (input.IsValueTuple() is {} tuple)
            {
                if (convertAll)
                {
                    var result = new object[tuple.Length];
                    for (var i = 0; i < tuple.Length; i++)
                        result[i] = tuple[i];
                    tempResult = result;
                }
                else
                    tempResult = tuple[0];
            }

            return tempResult switch
            {
                Enum @enum when Enum.IsDefined(@enum.GetType(), @enum) => Enum.GetName(@enum.GetType(), @enum),
                Enum @enum => FlagEnumConverter(@enum),
                _ => tempResult
            };
        }
       

        internal static object Converter3(object inp, bool convertAll = false)
        {
            var result = inp;
            if (inp is { } &&
                inp.GetType().IsValueType &&
                inp.IsValueTuple() is {} vTuple)
            {
                if (convertAll)
                {
                    var coll = new object[vTuple.Length];
                    for (var i = 0; i < vTuple.Length; i++)
                        coll[i] = vTuple[i];
                    result = coll;
                }
                else
                    result = vTuple[0];
            }

            return result is Enum
                ? Enum.GetName(result.GetType(), result)
                : result;
        }

        public static void WriteJson(StreamWriter str, object settings)
        {
            var props = settings.GetType()
                        .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                        .Where(p =>
                            p.GetCustomAttribute<SerializationOrderAttribute>() is {} &&
                            p.SetMethod is {} &&
                            p.GetMethod is {})
                        .OrderBy(p =>
                            p.GetCustomAttribute<SerializationOrderAttribute>().Index);

            var data = props.Select(p => new
                            {
                                p.Name,
                                Value = Converter(p.GetValue(settings),
                                    p.GetCustomAttribute<SerializationOrderAttribute>(true)?.All ?? false)
                            }).Where(item => item.Value is {})
                            .ToDictionary(item => item.Name, item => item.Value);

            var dataString = JsonConvert.SerializeObject(data, Formatting.Indented);

            str.Write(dataString);
            str.Flush();
        }

        public static ReadOnlyDictionary<string, object> ReadJson(StreamReader str)
        {


            var line = str.ReadToEnd();

            var result = JsonConvert.DeserializeObject<Dictionary<string, object>>(line)
                                    .ToDictionary(x => x.Key, x => Process(x.Value));

            return new ReadOnlyDictionary<string, object>(result);
        }

        public static async Task<ReadOnlyDictionary<string, object>> ReadJsonAsync(Stream str, Encoding enc, CancellationToken token)
        {
            if (!str.CanRead)
                throw new ArgumentException("Stream does not support writing.", nameof(str));

            var buffer = new byte[str.Length];

            await str.ReadAsync(buffer, 0, buffer.Length, token);

            var @string = enc.GetString(buffer);

            var result = JsonConvert.DeserializeObject<Dictionary<string, object>>(@string)
                                    .ToDictionary(x => x.Key, x => Process(x.Value));
            return new ReadOnlyDictionary<string, object>(result);

        }

        public static Task WriteJsonAsync(this object settings, Stream str, Encoding enc, CancellationToken token)
        {
            if (!str.CanWrite)
                throw new ArgumentException("Stream does not support writing.", nameof(str));
            var props = settings.GetType()
                                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                                .Where(p =>
                                    p.GetCustomAttribute<SerializationOrderAttribute>() is {} &&
                                    p.GetMethod is {})
                                .OrderBy(p =>
                                    p.GetCustomAttribute<SerializationOrderAttribute>(true)?.Index ?? 0);

            var data = props.Select(p => new
                            {
                                p.Name,
                                Value = Converter(p.GetValue(settings),
                                    p.GetCustomAttribute<SerializationOrderAttribute>(true)?.All ?? false)
                            }).Where(item => item.Value is {})
                            .ToDictionary(item => item.Name, item => item.Value);

            var dataString = JsonConvert.SerializeObject(data, Formatting.Indented);

            var byteRep = enc.GetBytes(dataString);

            return str.WriteAsync(byteRep, 0, byteRep.Length, token);
        }

        private static object Process( object token)
        {
            return token switch
            {
                JObject obj => new ReadOnlyDictionary<string, object>(obj.Properties()
                    .ToDictionary(x => x.Name, x => Process(x.Value))),
                JValue val => val.Value,
                JArray array => array.Select(Process).ToArray(),
                _ => token
            };
        }

    }
}