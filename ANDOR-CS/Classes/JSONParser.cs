using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Web.Script.Serialization;
using ANDOR_CS.Attributes;
using NonSerializedAttribute = ANDOR_CS.Attributes.NonSerializedAttribute;

namespace ANDOR_CS.Classes
{
    internal static class JsonParser
    {
        public static void WriteJson(StreamWriter str, SettingsBase settings)
        {
            object Converter(object inp, bool convertAll = false)
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
