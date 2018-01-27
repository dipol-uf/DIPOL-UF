using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Xml;

using ANDOR_CS.Attributes;
using ANDOR_CS.Enums;
using NonSerializedAttribute = ANDOR_CS.Attributes.NonSerializedAttribute;

namespace ANDOR_CS.Classes
{
    static class XmlParser
    {
        private static readonly Regex TupleTypeParser = new Regex(@"ValueTuple`(\d)+\[?.?\]?");

        private static readonly Type[] AllowedTypes = {
            typeof(int),
            typeof(float),
            typeof(double)
        };

        public static  Dictionary<string, object>  ReadXml(XmlReader reader)
        {
            var result = new Dictionary<string, object>();

            while (reader.Read())
                if (reader.NodeType != XmlNodeType.Whitespace &&
                    reader.NodeType != XmlNodeType.DocumentType &&
                    reader.NodeType != XmlNodeType.XmlDeclaration &&
                    reader.NodeType != XmlNodeType.EndElement)
                {
                    var value = (DeserializeElement(reader));
                    result.Add(value.Key, value.Value);
                }

            return result;
        }

        public static void WriteXml(XmlWriter writer, SettingsBase settings)
        {
            var props = settings.GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => 
                    p.GetCustomAttribute<NonSerializedAttribute>(true) == null &&
                    p.SetMethod != null &&
                    p.GetMethod != null)
                .OrderBy(p => p.GetCustomAttribute<SerializationOrderAttribute>(true)?.Index ?? 0);

            writer.WriteStartDocument();
            writer.WriteStartElement("Settings");
#if DEBUG
            if(settings.Camera != null)
#endif
                writer.WriteAttributeString("CompatibleDevice", settings.Camera.Capabilities.CameraType.ToString());

            object value;

            foreach (var p in props)
                if ((value = p.GetValue(settings)) != null)
                    SerializeElement(value, p.Name, writer);

            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        private static void SerializeElement(object element, string name, XmlWriter writer)
        {
            var elemType = element.GetType();
            writer.WriteStartElement(name);
            writer.WriteAttributeString("Type", elemType.ToString());
            if (!elemType.IsEnum &&
                elemType.GetCustomAttribute<DataContractAttribute>(true) != null)
            {

                var fields = GetFieldsWithAttribute<DataMemberAttribute>(elemType);
                var props = GetPropertiesWithAttribute<DataMemberAttribute>(elemType);

                foreach (var f in fields)
                    SerializeElement(f.GetValue(element), f.Name, writer);

                foreach (var p in props)
                    SerializeElement(p.GetValue(element), p.Name, writer);

            }
            else if (element is ITuple tuple)
            {
                for (int i = 0; i < tuple.Length; i++)
                    SerializeElement(tuple[i], "Tpl_" + i, writer);
            }
            else
                writer.WriteAttributeString("Value", element.ToString());

            writer.WriteEndElement();
        }

        private static KeyValuePair<string, object> DeserializeElement(XmlReader reader)
        {
            var name = reader.Name;
            var typeStr = reader.GetAttribute("Type");
            if (string.IsNullOrWhiteSpace(typeStr) && name != @"Settings")
                throw new XmlException("Parsed node does not provide type information where it was expected.");

            Type type;

            if (reader.AttributeCount == 2)
            {
                var valStr = reader.GetAttribute("Value");

                if (string.IsNullOrWhiteSpace(valStr))
                    return new KeyValuePair<string, object>(name, null);

                if (typeStr == typeof(string).FullName)
                    return new KeyValuePair<string, object>(name, valStr);


                if ((type = AllowedTypes.FirstOrDefault(tp => tp.FullName == typeStr)) != null)
                {
                    var mi = type.GetMethod("Parse",
                        new[]
                        {
                            typeof(string),
                            typeof(NumberStyles),
                            typeof(IFormatProvider)
                        });

                    return new KeyValuePair<string, object>(name, mi?.Invoke(
                        null,
                        new object[]
                        {
                            valStr,
                            NumberStyles.Any,
                            NumberFormatInfo.InvariantInfo
                        }
                    ));
                }

                if ((type = typeof(AndorSdkInitialization)
                        .Assembly
                        .ExportedTypes
                        .FirstOrDefault(tp => tp.FullName == typeStr)) != null &&
                    type.IsEnum)
                    return new KeyValuePair<string, object>(name, Enum.Parse(type, valStr));
            }
            else if (reader.AttributeCount == 1)
            {
                if (name == @"Settings")
                    return new KeyValuePair<string, object>(@"CompatibleDevice",
                        Enum.Parse(typeof(CameraType), reader.GetAttribute(0) ?? ""));

                var match = TupleTypeParser.Match(typeStr);
                if (match.Success && match.Groups.Count == 2)
                {
                    var size = int.Parse(match.Groups[1].Value, NumberStyles.Any, NumberFormatInfo.InvariantInfo);
                    var results = new KeyValuePair<string, object>[size];

                    for (var i = 0; i < size; i++)
                    {
                        ReadUntilData(reader);

                        results[i] = DeserializeElement(reader);
                    }

                    var ctor = typeof(ValueTuple)
                        .GetMethods()
                        .FirstOrDefault(m => 
                            m.Name == "Create" &&
                            m.IsGenericMethod &&
                            m.GetGenericArguments().Length == size);

                    if (ctor is null)
                        throw
                            new XmlException(
                                $"Unable to parse XML file: element {name} of ValueTuple type has too many parameters ({size}).");
                    var tupleVal = ctor
                        .MakeGenericMethod(results.Select(res => res.Value.GetType()).ToArray())
                        .Invoke(null, results.Select(res => res.Value).ToArray());

                    return new KeyValuePair<string, object>(name, tupleVal);
                }

                if ((type = typeof(AndorSdkInitialization)
                        .Assembly
                        .ExportedTypes
                        .FirstOrDefault(tp => tp.FullName == typeStr)) == null || 
                    type.IsEnum || 
                    type.IsClass ||
                    type.GetCustomAttribute<DataContractAttribute>(true) == null)
                    return new KeyValuePair<string, object>(reader.Name, null);
                {
                    var fields = GetFieldsWithAttribute<DataMemberAttribute>(type);
                    var props = GetPropertiesWithAttribute<DataMemberAttribute>(type);

                    var resultsFields = new KeyValuePair<string, object>[fields.Length];
                    var resultsProps = new KeyValuePair<string, object>[props.Length];


                    for (var i = 0; i < fields.Length; i++)
                    {
                        ReadUntilData(reader);
                        resultsFields[i] = DeserializeElement(reader);
                    }

                    for (var i = 0; i < props.Length; i++)
                    {
                        ReadUntilData(reader);
                        resultsProps[i] = DeserializeElement(reader);
                    }

                    var complexTypeVal = Activator.CreateInstance(type);


                    for (var i = 0; i < fields.Length; i++)
                        fields
                            .FirstOrDefault(fi => fi.Name == resultsFields[i].Key && fi.IsPublic)
                            ?.SetValue(complexTypeVal, resultsFields[i].Value);

                    for (var i = 0; i < props.Length; i++)
                        props
                            .FirstOrDefault(pi => pi.Name == resultsProps[i].Key && pi.CanWrite)
                            ?.SetValue(complexTypeVal, resultsProps[i].Value);

                    return new KeyValuePair<string, object>(name, complexTypeVal);
                }
            }


            return new KeyValuePair<string, object>(reader.Name, null);
        }

        private static FieldInfo[] GetFieldsWithAttribute<T>(Type t) where T : Attribute
            => t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(fi => fi.GetCustomAttribute<T>(true) != null)
            .ToArray();

        private static PropertyInfo[] GetPropertiesWithAttribute<T>(Type t) where T : Attribute
            => t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(pi => pi.GetCustomAttribute<T>() != null)
            .ToArray();

        private static void ReadUntilData(XmlReader reader)
        {
            while (reader.Read() &&
                (reader.NodeType == XmlNodeType.Whitespace ||
                reader.NodeType == XmlNodeType.DocumentType ||
                reader.NodeType == XmlNodeType.XmlDeclaration ||
                reader.NodeType == XmlNodeType.EndElement))
            { }
        }
    }
}
