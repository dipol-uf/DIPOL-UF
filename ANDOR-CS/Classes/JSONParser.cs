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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using ANDOR_CS.Attributes;
using NonSerializedAttribute = ANDOR_CS.Attributes.NonSerializedAttribute;

namespace ANDOR_CS.Classes
{
    internal static class JsonParser
    {
        private static readonly int WriteChunkSize = 64;

        internal static object Converter(object inp, bool convertAll = false)
        {
            var result = inp;
            if (inp != null &&
                inp.GetType().IsValueType &&
                inp is ITuple vTuple)
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

        public static void WriteJson(StreamWriter str, SettingsBase settings)
        {
            var props = settings.GetType()
                        .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                        .Where(p =>
                            p.GetCustomAttribute<NonSerializedAttribute>(true) == null &&
                            p.SetMethod != null &&
                            p.GetMethod != null)
                        .OrderBy(p =>
                            p.GetCustomAttribute<SerializationOrderAttribute>(true)?.Index ?? 0);

            var data = props.Select(p => new
                            {
                                p.Name,
                                Value = Converter(p.GetValue(settings), p.GetCustomAttribute<SerializationOrderAttribute>(true)?.All ?? false)
                            })
                            .Where(item => item.Value != null)
                            .ToDictionary(item => item.Name, item => item.Value);
            data.Add("CompatibleDevice", settings.Camera?.Capabilities.CameraType.ToString());

            var dataString = TabifyNestedNodes(new JavaScriptSerializer().Serialize(data));

            str.Write(dataString);
            str.Flush();
        }


        public static Dictionary<string, object> ReadJson(StreamReader str)
            => new JavaScriptSerializer()
                .DeserializeObject(str.ReadToEnd()) as Dictionary<string, object>;
        
        public static async Task WriteJsonAsync(this SettingsBase settings, Stream str, Encoding enc, CancellationToken token)
        {
            if(!str.CanWrite)
                throw new ArgumentException("Stream does not support writing.", nameof(str));
            var props = settings.GetType()
                                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                                .Where(p =>
                                    p.GetCustomAttribute<NonSerializedAttribute>(true) == null &&
                                    p.SetMethod != null &&
                                    p.GetMethod != null)
                                .OrderBy(p =>
                                    p.GetCustomAttribute<SerializationOrderAttribute>(true)?.Index ?? 0);

            var data = props.Select(p => new
                            {
                                p.Name,
                                Value = Converter(p.GetValue(settings), p.GetCustomAttribute<SerializationOrderAttribute>(true)?.All ?? false)
                            })
                            .Where(item => item.Value != null)
                            .ToDictionary(item => item.Name, item => item.Value);
            data.Add("CompatibleDevice", settings.Camera?.Capabilities.CameraType.ToString());

            var byteRep = enc.GetBytes(TabifyNestedNodes(new JavaScriptSerializer().Serialize(data)));

            var nStep = (int)Math.Ceiling(1.0 * byteRep.Length / WriteChunkSize);

            try
            {
                for (var i = 0; i < nStep; i++, token.ThrowIfCancellationRequested())
                    await str.WriteAsync(byteRep, WriteChunkSize * i, WriteChunkSize, token);
                token.ThrowIfCancellationRequested();
                await str.WriteAsync(byteRep, nStep * WriteChunkSize, byteRep.Length - nStep * WriteChunkSize, token);
            }
            finally
            {
                await str.FlushAsync(token);
            }
        }

        //public static void ReadJson(Stream str, Encoding enc, CancellationToken token)
        //{
            
        //}

        private static string TabifyNestedNodes(string nodeVal)
        {
            var tabIndex = 0;
            var sb = new StringBuilder(nodeVal.Length + 16);

            foreach (var t in nodeVal)
                switch (t)
                {
                    case '[':
                    case '{':
                        sb.Append(t);
                        sb.Append("\r\n");
                        sb.Append('\t', ++tabIndex);
                        break;
                    case ']':
                    case '}':
                        sb.Append("\r\n");
                        sb.Append('\t', --tabIndex);
                        sb.Append(t);
                        break;
                    case ',':
                        sb.Append(t);
                        sb.Append("\r\n");
                        sb.Append('\t', tabIndex);
                        break;
                    case ':':
                        sb.Append(" : ");
                        break;
                    default:
                        sb.Append(t);
                        break;
                }

            return sb.ToString();
        }
    }
}
