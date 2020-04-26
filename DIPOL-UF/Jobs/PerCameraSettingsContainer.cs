#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using ANDOR_CS.Classes;
using ANDOR_CS.DataStructures;
using ANDOR_CS.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Serializers;

namespace DIPOL_UF.Jobs
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal sealed class PerCameraSettingsContainer: ICloneable
    {
        private static readonly ImmutableDictionary<string, PropertyInfo> Properties =
            typeof(PerCameraSettingsContainer)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(x => new {Property = x, Order = x.GetCustomAttribute<SerializationOrderAttribute>()})
                .Where(x => x.Order is { })
                .OrderBy(x => x.Order.Index)
                .ToImmutableDictionary(x => x.Property.Name, x => x.Property);

        [SerializationOrder(1)]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, float?>? VSSpeed { get; private set; }

        [SerializationOrder(5)]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, float?>? HSSpeed { get; private set; }
        
        [SerializationOrder(3)]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, int?>? ADConverter { get; set; }

        [SerializationOrder(2)]
        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, VSAmplitude?>? VSAmplitude { get; set; }

        [SerializationOrder(4)]
        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, OutputAmplification?>? OutputAmplifier { get; set; }

        [SerializationOrder(6)]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, string?>? PreAmpGain { get; set; }

        [SerializationOrder(7)]
        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, AcquisitionMode?>? AcquisitionMode { get; set; }

        [SerializationOrder(8)]
        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, ReadMode?>? ReadoutMode { get; set; }

        [SerializationOrder(9)]
        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, TriggerMode?>? TriggerMode { get; set; }

        [SerializationOrder(0)]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, float?>? ExposureTime { get; set; }

        [SerializationOrder(11)]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, Rectangle?>? ImageArea { get; set; }

        [SerializationOrder(12, true)]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, (int Frames, float Time)?>? AccumulateCycle { get; set; }

        [SerializationOrder(13, true)]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, (int Frames, float Time)?>? KineticCycle { get; set; }

        [SerializationOrder(10)]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, int?>? EMCCDGain { get; set; }

        public IDictionary? this[string propName]
        {
            get => Properties.TryGetValue(propName, out var prop)
                ? prop.GetValue(this) as IDictionary
                : null;
            set
            {
                if (Properties.TryGetValue(propName, out var prop))
                    prop.SetValue(this, value);
                else throw new KeyNotFoundException();
            }
        }
        public PerCameraSettingsContainer Clone()
        {
            var result = new PerCameraSettingsContainer();
            foreach (var (_, prop) in Properties)
            {
                if(prop.GetValue(this) is { } value)
                    prop.SetValue(result, value);
            }

            return result;
        }

        object ICloneable.Clone() => Clone();

        public IImmutableDictionary<string, object?> GatherCameraSettings(string camKey)
        {
            var result = ImmutableDictionary.CreateBuilder<string, object?>();

            foreach (var (name, prop) in Properties)
            {
                var setts = prop.GetValue(this) as IDictionary;
                if (setts?[camKey] is { } value)
                    result[name] = value;
            }

            return result.ToImmutable();
        }

    }
}
