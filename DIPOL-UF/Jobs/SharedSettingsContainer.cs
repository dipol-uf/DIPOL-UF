#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using ANDOR_CS.Classes;
using ANDOR_CS.DataStructures;
using ANDOR_CS.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Serializers;

namespace DIPOL_UF.Jobs
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal class SharedSettingsContainer
    {
        private static readonly Dictionary<string, PropertyInfo> Properties = typeof(SharedSettingsContainer)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(x => new { Property = x, Order = x.GetCustomAttribute<SerializationOrderAttribute>() })
            .Where(x => x.Order is { })
            .OrderBy(x => x.Order.Index)
            .ToDictionary(x => x.Property.Name, x => x.Property);

        private static readonly Dictionary<string, PropertyInfo> SbProperties = typeof(SettingsBase)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(x => new { Property = x, Order = x.GetCustomAttribute<SerializationOrderAttribute>() })
            .Where(x => x.Order is { })
            .OrderBy(x => x.Order.Index)
            .ToDictionary(x => x.Property.Name, x => x.Property);

        private static readonly Dictionary<string, (PropertyInfo This, PropertyInfo Settings)> JoinedProperties = Properties.Join(SbProperties, x => x.Key, y => y.Key, (x, y) => (x.Key, This: x.Value, Settings: y.Value))
            .ToDictionary(x => x.Key, x => (Shared: x.This, x.Settings));

        
        [SerializationOrder(1)]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public float? VSSpeed { get; set; }

        [SerializationOrder(5)]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public float? HSSpeed { get; set; }

        [SerializationOrder(3)]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? ADConverter { get; set; }

        [SerializationOrder(2)]
        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public VSAmplitude? VSAmplitude { get; set; }

        [SerializationOrder(4)]
        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public OutputAmplification? OutputAmplifier { get; set; }

        [SerializationOrder(6)]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? PreAmpGain { get; set; }

        [SerializationOrder(7)]
        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public AcquisitionMode? AcquisitionMode { get; set; }

        [SerializationOrder(8)]
        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public ReadMode? ReadoutMode { get; set; }

        [SerializationOrder(9)]
        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public TriggerMode? TriggerMode { get; set; }

        [SerializationOrder(0)]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public float? ExposureTime { get; set; }

        [SerializationOrder(11)]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Rectangle? ImageArea { get; set; }

        [SerializationOrder(12, true)]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public (int Frames, float Time)? AccumulateCycle { get; set; }

        [SerializationOrder(13, true)]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public (int Frames, float Time)? KineticCycle { get; set; }

        [SerializationOrder(10)]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? EMCCDGain { get; set; }

        public SharedSettingsContainer(SettingsBase? sourceSettings)
        {
            if (sourceSettings is null)
                return;

            foreach (var (name, (shared, specific)) in JoinedProperties)
            {
                var value = specific.GetValue(sourceSettings);
                switch (value)
                {
                    case ITuple tuple:
                        // Only tuples may have different representation
                        shared.SetValue(this, name switch
                        {
                            nameof(SettingsBase.VSSpeed) when tuple[1] is float speed => speed,
                            nameof(SettingsBase.ADConverter) when tuple[0] is int index => index,
                            nameof(SettingsBase.HSSpeed) when tuple[1] is float speed => speed,
                            nameof(SettingsBase.PreAmpGain) when tuple[1] is string gain => gain,
                            nameof(SettingsBase.OutputAmplifier) when tuple[0] is OutputAmplification amplif => amplif,
                            nameof(SettingsBase.AccumulateCycle) when tuple[0] is int frames && tuple[1] is float time => (Frames: frames, Time: time),
                            nameof(SettingsBase.KineticCycle) when tuple[0] is int frames && tuple[1] is float time => (Frames: frames, Time: time),
                            _ => null
                        });
                        break;
                    case { }:
                        shared.SetValue(this, value);
                        break;
                }
            }
        }

        public SharedSettingsContainer()
        {
        }

        public SettingsBase PrepareTemplateForCamera(CameraBase camera,
            IReadOnlyDictionary<string, object?>? overridingParams = null)
        {
            _ = camera ?? throw new ArgumentNullException(nameof(camera));
            var settsCollection = AsDictionary();
            if (overridingParams is { })
            {
                foreach (var (name, value) in overridingParams)
                    if (value is { })
                        settsCollection[name] = value;
            }

            var setts = camera.GetAcquisitionSettingsTemplate();
            
            setts.Load1(settsCollection);

            return setts;
        }

        private Dictionary<string, object> AsDictionary()
        {
            var result = new Dictionary<string, object>();
            foreach (var (key, prop) in Properties)
                if (prop.GetValue(this) is {} value)
                    result.Add(key, value);

            return result;
        }

        
        
    }
}
