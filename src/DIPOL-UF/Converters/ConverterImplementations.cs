#nullable enable
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
        private static Dictionary<string, string>? _cachedAliases;
        private static Dictionary<string, string>? _cachedFilters;
        private static Dictionary<string, int>? _cachedOrders;

        private static Dictionary<string, DeviceSettingsDescriptor>? _cachedSettings;
        public static DeviceSettingsDescriptor? DeviceDescriptor(IDevice? cam)
        {
            if(cam is null)
            {
                return null;
            }
            var camString = $"{cam.CameraModel}_{cam.SerialNumber}";
            _cachedSettings ??=
                UiSettingsProvider.Settings
                    .GetArray<DeviceSettingsDescriptor>(@"CameraDescriptors")
                    .ToDictionary(x => x.Name);

            return _cachedSettings.TryGetValue(camString, out var desc) ? desc : null;
        }

        public static string CameraToStringAliasConversion(IDevice? cam)
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

        public static string CameraToFilterConversion(IDevice? cam)
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

        public static Brush TemperatureToBrushConversion(float temp, Brush[]? brushes) =>
            brushes is {Length: >= 4}
                ? temp switch
                {
                    > 20 => brushes[0],
                    > 5 => brushes[1],
                    > -15 => brushes[2],
                    _ => brushes[3]
                }
                : Brushes.Black;

        public static string EnumToDescriptionConversion(Enum @enum)
            => @enum.GetEnumNameRep().Full;

        public static List<string> EnumToDescriptionConversion(IEnumerable<Enum> enums)
            => enums.GetEnumNamesRep().Select(x => x.Full).ToList();
       
        public static Enum? DescriptionToEnumConversion(string desc, Type type)
        {
            if (type is {IsEnum: true})
            {
                return EnumHelper.FromDescription(desc, type);
            }

            if (Nullable.GetUnderlyingType(type) is {IsEnum: true} innerType)
            {
                return EnumHelper.FromDescription(desc, innerType);
            }
            
            return null;
        }

        public static Brush TemperatureStatusToBrushConversion(TemperatureStatus status, Brush[] brushes) =>
            brushes.Length >= 5
                ? status switch
                {
                    TemperatureStatus.Off => brushes[0],
                    TemperatureStatus.Stabilized => brushes[1],
                    TemperatureStatus.NotReached => brushes[2],
                    TemperatureStatus.Drift => brushes[3],
                    TemperatureStatus.NotStabilized => brushes[4],
                    _ => Brushes.Black
                }
                : Brushes.Black;

        public static bool BoolToBoolConversion(List<bool> values, string? parameter = null) =>
            parameter?.Trim().ToLowerInvariant() switch
            {
                "any" => values.Any(x => x),
                "notall" => values.Any(x => !x),
                "notany" => !values.Any(x => x),
                // Default, also "all" branch
                _ => values.All(x => x),
            };

        public static bool CompareToConversion(object src, object? parameter)
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

                return operation switch
                {
                    "=" or "==" => diff == 0,
                    ">" => diff < 0,
                    ">=" => diff <= 0,
                    "<" => diff > 0,
                    "<=" => diff >= 0,
                    "!=" => diff != 0,
                    _ => throw new ArgumentException(
                        string.Format(Localization.General_InvalidArgument, nameof(parameter))
                    )
                };
            }
            throw new ArgumentException(string.Format(Localization.General_InvalidArgument, nameof(parameter)));
        }

        public static object? FieldAccessConversion(object? src, string fieldName)
            => src?.GetType()
                  .GetField(fieldName, BindingFlags.Public | BindingFlags.Instance)
                  ?.GetValue(src);

        public static string? ValidationErrorsToStringConversion(object value)
            => value is IReadOnlyCollection<ValidationError> collection
                ? $"{collection.Select(x => x.ErrorContent).EnumerableToString(";\r\n")}."
                : null;
    }
}
