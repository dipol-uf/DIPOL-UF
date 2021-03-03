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
using System.Globalization;
using System.Windows.Media;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using ANDOR_CS;
using ANDOR_CS.Classes;
using ANDOR_CS.Enums;
using DIPOL_UF.Properties;
using Newtonsoft.Json.Linq;

namespace DIPOL_UF.Converters
{
    public static class ConverterImplementations
    {
        //private static List<(string Name, string Alias)> _cachedAliases;
        private static Dictionary<string, string> _cachedAliases;
        private static Dictionary<string, string> _cachedFilters;
        private static Dictionary<string, int> _cachedOrders;

        public static void CameraDesc(IDevice cam)
        {
            //var camString = $"{cam.CameraModel}_{cam.SerialNumber}";
            var setts = UiSettingsProvider.Settings.GetArray<JToken>(@"CameraDescriptors");
        }

        public static string CameraToStringAliasConversion(IDevice cam)
        {
            if (cam is null)
                return string.Empty;
            var camString = $"{cam.CameraModel}_{cam.SerialNumber}";
            if (_cachedAliases is null)
                _cachedAliases =
                    UiSettingsProvider.Settings
                                      .GetArray<JToken>(@"CameraDescriptors")
                                      .ToDictionary(x => x.Value<string>(@"Name"),
                                          y => y.Value<string>(@"Alias"));
            return _cachedAliases.TryGetValue(camString, out var result) && !string.IsNullOrWhiteSpace(result)
                ? result
                : camString;
        }

        public static string CameraToFilterConversion(IDevice cam)
        {
            if (cam is null)
                return string.Empty;
            var camString = $"{cam.CameraModel}_{cam.SerialNumber}";
            if (_cachedFilters is null)
                _cachedFilters =
                    UiSettingsProvider.Settings
                                      .GetArray<JToken>(@"CameraDescriptors")
                                      .ToDictionary(x => x.Value<string>(@"Name"),
                                          y => y.Value<string>(@"Filter"));
            return _cachedFilters.TryGetValue(camString, out var result) && !string.IsNullOrWhiteSpace(result)
                ? result
                : cam.CameraIndex.ToString();
        }

        public static int CameraToIndexConversion(IDevice cam)
        {
            if (cam is null)
                return int.MaxValue;
            var camString = $"{cam.CameraModel}_{cam.SerialNumber}";
            if (_cachedOrders is null)
                _cachedOrders =
                    UiSettingsProvider.Settings
                        .GetArray<JToken>(@"CameraDescriptors")
                        .ToDictionary(x => x.Value<string>(@"Name"),
                            y => y.Value<int>(@"Order"));
            return _cachedOrders.TryGetValue(camString, out var result)
                ? result
                : cam.CameraIndex;
        }

        public static int CameraToIndexConversion(string cam)
        {
            if (cam is null)
                return int.MaxValue;
            if (_cachedFilters is null)
                _cachedFilters =
                    UiSettingsProvider.Settings
                        .GetArray<JToken>(@"CameraDescriptors")
                        .ToDictionary(x => x.Value<string>(@"Name"),
                            y => y.Value<string>(@"Filter"));

            if (_cachedOrders is null)
                _cachedOrders =
                    UiSettingsProvider.Settings
                        .GetArray<JToken>(@"CameraDescriptors")
                        .ToDictionary(x => x.Value<string>(@"Name"),
                            y => y.Value<int>(@"Order"));

            var camString = _cachedFilters.Where(x => x.Value == cam).Select(x => x.Key).FirstOrDefault();


            return !string.IsNullOrWhiteSpace(camString) && _cachedOrders.TryGetValue(camString, out var result)
                ? result
                : int.TryParse(cam, NumberStyles.Any, NumberFormatInfo.InvariantInfo, out var id)
                    ? id
                    : cam.GetHashCode();
        }


        public static string CameraKeyToHostConversion(string input)
            => Helper.GetCameraHostName(input);

        public static Brush TemperatureToBrushConversion(float temp, Brush[] brushes)
        {
            if (brushes is null || brushes.Length < 4)
                return Brushes.Black;

            if (temp > 20)
                return brushes[0];
            if (temp > 5)
                return brushes[1] ;
            if (temp > -15)
                return brushes[2];
            return brushes[3];
        }

        public static string EnumToDescriptionConversion(Enum @enum)
            => @enum.GetEnumStringEx().EnumerableToString();

        public static List<string> EnumToDescriptionConversion(IEnumerable<Enum> enums)
            => enums.Select(x => x.GetEnumStringEx().EnumerableToString())
                    .ToList();
       
        public static Enum DescriptionToEnumConversion(string desc, Type type)
        {
            if (type.BaseType == typeof(Enum))
                return (Enum)Helper.GetEnumFromDescription(desc, type);

            if(Nullable.GetUnderlyingType(type) is Type innerType &&
               innerType.BaseType == typeof(Enum))
                return (Enum)Helper.GetEnumFromDescription(desc, innerType);

            return null;
        }

        public static Brush TemperatureStatusToBrushConversion(TemperatureStatus status, Brush[] brushes)
        {
            if (brushes.Length >= 5)

                switch (status)
                {
                    case TemperatureStatus.Off:
                        return brushes[0];
                    case TemperatureStatus.Stabilized:
                        return brushes[1];
                    case TemperatureStatus.NotReached:
                        return brushes[2];
                    case TemperatureStatus.Drift:
                        return brushes[3];
                    case TemperatureStatus.NotStabilized:
                        return brushes[4];
                    default:
                        return Brushes.Black;
                }
            else
                return Brushes.Black;
        }

        public static bool BoolToBoolConversion(List<bool> values, string parameter = null)
        {
            if (parameter is string strPar)
            {
                var key = strPar.Trim().ToLowerInvariant();
                switch (key)
                {
                    case "all":
                        return values.All(x => x);
                    case "any":
                        return values.Any();
                    case "notall":
                        return !values.All(x => x);
                    case "notany":
                        return !values.Any();
                }
            }

            return values.All(x => x);
        }

        public static bool CompareToConversion(object src, object parameter)
        {
            if(!src.GetType().IsPrimitive)
                throw new TypeAccessException(nameof(src));

            if (parameter is string s)
            {
                s = s.Trim();

                var comparison = Regex.Match(s, "[+-]?[0-9]+\\.?[0-9]*") is var match && match.Success
                    ? match.Value
                    : throw new ArgumentException(string.Format(Localization.General_InvalidArgument, nameof(parameter)));

                var operation = match.Index > 0
                    ? s.Substring(0, match.Index).Trim()
                    : throw new ArgumentException(string.Format(Localization.General_InvalidArgument, nameof(parameter)));

                var diff = (src.GetType()
                               .GetMethod("Parse",
                                   new[] {typeof(string), typeof(NumberStyles), typeof(IFormatProvider)})
                               ?.Invoke(null,
                                   new object[] {comparison, NumberStyles.Any, NumberFormatInfo.InvariantInfo})
                    as IComparable)
                    ?.CompareTo(src);

                switch (operation)
                {
                    case "=":
                    case "==":
                        return diff == 0;
                    case ">":
                        return diff < 0;
                    case ">=":
                        return diff <= 0;
                    case "<":
                        return diff > 0;
                    case "<=":
                        return diff >= 0;
                    case "!=":
                        return diff != 0;
                    default:
                        throw new ArgumentException(string.Format(Localization.General_InvalidArgument, nameof(parameter)));
                }

            }
            throw new ArgumentException(string.Format(Localization.General_InvalidArgument, nameof(parameter)));
        }

        public static object FieldAccessConversion(object src, string fieldName)
            => src?.GetType()
                  .GetField(fieldName, BindingFlags.Public | BindingFlags.Instance)
                  ?.GetValue(src);

        public static string ValidationErrorsToStringConversion(object value)
            => value is IReadOnlyCollection<ValidationError> collection
                ? collection.Select(x => x.ErrorContent).EnumerableToString(";\r\n") + "."
                : null;
    }
}
