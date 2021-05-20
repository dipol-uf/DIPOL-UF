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
        internal static object? Converter(object? input, bool convertAll = false)
        {
            static string[] FlagEnumConverter(Enum @enum)
            {
                
                var type = @enum.GetType();
                List<Enum> values = type.GetFields(BindingFlags.Public | BindingFlags.Static)
                                        .Where(f => f.FieldType == type)
                                        .Select(
                                            f => (
                                                FieldInfo: f,
                                                Ignore: f.GetCustomAttribute<IgnoreDefaultAttribute>() is not null
                                            )
                                        )
                                        .Where(x => !x.Ignore)
                                        .Select(x => (Enum) x.FieldInfo.GetValue(null))
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
       

        internal static object? Converter3(object? inp, bool convertAll = false)
        {
            var result = inp;
            if (inp is { } &&
                inp.GetType().IsValueType &&
                inp.IsValueTuple() is { } vTuple)
                // BUG : Check this
            //if (inp is ITuple vTuple)
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
            IOrderedEnumerable<PropertyInfo> props =
                settings.GetType()
                        .GetProperties(
                            BindingFlags.Instance | BindingFlags.Public
                        )
                        .Where(
                            p =>
                                p.GetCustomAttribute<
                                    SerializationOrderAttribute>() is not null &&
                                p.SetMethod is not null &&
                                p.GetMethod is not null
                        )
                        .OrderBy(
                            p =>
                                p.GetCustomAttribute<
                                    SerializationOrderAttribute>().Index
                        );

            Dictionary<string, object?> data =
                props.Select(
                         p => (
                             p.Name,
                             Value: Converter(
                                 p.GetValue(settings),
                                 p.GetCustomAttribute<SerializationOrderAttribute>(true)?.All ?? false
                             )
                         )
                     ).Where(item => item.Value is not null)
                     .ToDictionary(item => item.Name, item => item.Value);

            var dataString = JsonConvert.SerializeObject(data, Formatting.Indented);

            str.Write(dataString);
            str.Flush();
        }

        public static IReadOnlyDictionary<string, object?> GenerateJson(object settings)
        {
            IOrderedEnumerable<PropertyInfo> props =
                settings.GetType()
                        .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                        .Where(
                            p =>
                                p.GetCustomAttribute<SerializationOrderAttribute>() is not null &&
                                p.SetMethod is not null &&
                                p.GetMethod is not null
                        )
                        .OrderBy(
                            p =>
                                p.GetCustomAttribute<SerializationOrderAttribute>().Index
                        );

            return props.Select(
                            p =>
                            (
                                p.Name,
                                Value: Converter(
                                    p.GetValue(settings),
                                    p.GetCustomAttribute<SerializationOrderAttribute>(true)?.All ?? false
                                )
                            )
                        ).Where(item => item.Value is not null)
                        .ToDictionary(item => item.Name, item => item.Value);
        }

        public static ReadOnlyDictionary<string, object?> ReadJson(StreamReader str)
        {


            var line = str.ReadToEnd();

            Dictionary<string, object?> result =
                JsonConvert.DeserializeObject<Dictionary<string, object>>(line)
                           ?.ToDictionary(x => x.Key, x => Process(x.Value))
                ?? new Dictionary<string, object?>();

            return new ReadOnlyDictionary<string, object?>(result);
        }

        public static async Task<ReadOnlyDictionary<string, object?>> ReadJsonAsync(
            Stream str, Encoding enc, CancellationToken token
        )
        {
            if (!str.CanRead)
                throw new ArgumentException("Stream does not support writing.", nameof(str));

            var buffer = new byte[str.Length];

            await str.ReadAsync(buffer, 0, buffer.Length, token);

            var @string = enc.GetString(buffer);

            Dictionary<string, object?> result =
                JsonConvert.DeserializeObject<Dictionary<string, object>>(@string)
                           ?.ToDictionary(x => x.Key, x => Process(x.Value))
                ?? new Dictionary<string, object?>();
            return new ReadOnlyDictionary<string, object?>(result);

        }

        public static Task WriteJsonAsync(this object settings, Stream str, Encoding enc, CancellationToken token)
        {
            if (!str.CanWrite)
                throw new ArgumentException("Stream does not support writing.", nameof(str));
            IOrderedEnumerable<PropertyInfo> props =
                settings.GetType()
                        .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                        .Where(
                            p =>
                                p.GetCustomAttribute<SerializationOrderAttribute>() is not null &&
                                p.GetMethod is not null
                        )
                        .OrderBy(
                            p =>
                                p.GetCustomAttribute<SerializationOrderAttribute>(true)?.Index ?? 0
                        );

            var data = props.Select(
                                p =>
                                    (p.Name,
                                        Value: Converter(
                                            p.GetValue(settings),
                                            p.GetCustomAttribute<SerializationOrderAttribute>(true)?.All ?? false
                                        )
                                    )
                            ).Where(item => item.Value is not null)
                            .ToDictionary(item => item.Name, item => item.Value);

            var dataString = JsonConvert.SerializeObject(data, Formatting.Indented);

            var byteRep = enc.GetBytes(dataString);

            return str.WriteAsync(byteRep, 0, byteRep.Length, token);
        }

        private static object? Process(object? token) =>
            token switch
            {
                JObject obj => new ReadOnlyDictionary<string, object?>(
                    obj.Properties()
                       .ToDictionary(x => x.Name, x => Process(x.Value))
                ),
                JValue val => val.Value,
                JArray array => array.Select(Process).ToArray(),
                _ => token
            };
    }
}