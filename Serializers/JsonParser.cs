﻿//    This file is part of Dipol-3 Camera Manager.

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
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Serializers
{
    public static class JsonParser
    {
        internal static object Converter(object inp, bool convertAll = false)
        {
            var result = inp;
            if (inp is ITuple vTuple)
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
                            p.GetCustomAttribute<SerializationOrderAttribute>() != null &&
                            p.SetMethod != null &&
                            p.GetMethod != null)
                        .OrderBy(p =>
                            p.GetCustomAttribute<SerializationOrderAttribute>().Index);

            var data = props.Select(p => new
                            {
                                p.Name,
                                Value = Converter(p.GetValue(settings),
                                    p.GetCustomAttribute<SerializationOrderAttribute>(true)?.All ?? false)
                            }).Where(item => item.Value != null)
                            .ToDictionary(item => item.Name, item => item.Value);

            var dataString = JsonConvert.SerializeObject(data, Formatting.Indented);

            str.Write(dataString);
            str.Flush();
        }

        public static IReadOnlyDictionary<string, object> GenerateJson(object settings)
        {
            var props = settings.GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p =>
                    p.GetCustomAttribute<SerializationOrderAttribute>() != null &&
                    p.SetMethod != null &&
                    p.GetMethod != null)
                .OrderBy(p =>
                    p.GetCustomAttribute<SerializationOrderAttribute>().Index);

            return props.Select(p => new
                {
                    p.Name,
                    Value = Converter(p.GetValue(settings),
                        p.GetCustomAttribute<SerializationOrderAttribute>(true)?.All ?? false)
                }).Where(item => item.Value != null)
                .ToDictionary(item => item.Name, item => item.Value);
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
                                    p.GetCustomAttribute<SerializationOrderAttribute>() != null &&
                                    p.GetMethod != null)
                                .OrderBy(p =>
                                    p.GetCustomAttribute<SerializationOrderAttribute>(true)?.Index ?? 0);

            var data = props.Select(p => new
                            {
                                p.Name,
                                Value = Converter(p.GetValue(settings),
                                    p.GetCustomAttribute<SerializationOrderAttribute>(true)?.All ?? false)
                            }).Where(item => item.Value != null)
                            .ToDictionary(item => item.Name, item => item.Value);

            var dataString = JsonConvert.SerializeObject(data, Formatting.Indented);

            var byteRep = enc.GetBytes(dataString);

            return str.WriteAsync(byteRep, 0, byteRep.Length, token);
        }

        private static object Process( object token)
        {
            if (token is JObject obj)
                return new ReadOnlyDictionary<string, object>(obj.Properties().ToDictionary(x => x.Name, x => Process(x.Value)));
            if (token is JValue val)
                return val.Value;
            if (token is JArray array)
            {
                return array.Select(Process).ToArray();
            }


            return token;
        }

    }
}