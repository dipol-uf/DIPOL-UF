﻿#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using ANDOR_CS;
using ANDOR_CS.Classes;
using ANDOR_CS.DataStructures;
using ANDOR_CS.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Serializers;

namespace DIPOL_UF.Jobs
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal sealed class SharedSettingsContainer : ICloneable
    {
        private static readonly Dictionary<string, PropertyInfo> Properties = typeof(SharedSettingsContainer)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(x => new { Property = x, Order = x.GetCustomAttribute<SerializationOrderAttribute>() })
            .Where(x => x.Order is { })
            .OrderBy(x => x.Order.Index)
            .ToDictionary(x => x.Property.Name, x => x.Property);

        private static readonly Dictionary<string, PropertyInfo> SbProperties = typeof(IAcquisitionSettings)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(x => new { Property = x, Order = x.GetCustomAttribute<SerializationOrderAttribute>() })
            .Where(x => x.Order is { })
            .OrderBy(x => x.Order.Index)
            .ToDictionary(x => x.Property.Name, x => x.Property);

        private static readonly Dictionary<string, (PropertyInfo This, PropertyInfo Settings)> JoinedProperties = 
            Properties.Join(SbProperties, x => x.Key, y => y.Key, (x, y) => (x.Key, This: x.Value, Settings: y.Value))
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

        public SharedSettingsContainer(IAcquisitionSettings? sourceSettings)
        {
            if (sourceSettings is null)
                return;

            foreach (var (_, (shared, specific)) in JoinedProperties)
            {
                SetValueFromSettings(specific.GetValue(sourceSettings), shared);
            }
        }

        public SharedSettingsContainer()
        {
        }

        public IAcquisitionSettings PrepareTemplateForCamera(IDevice camera,
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

        public Dictionary<string, object?> AsDictionary(bool outputAll = false)
        {
            var result = new Dictionary<string, object?>();
            foreach (var (key, prop) in Properties)
                if (prop.GetValue(this) is var value  && (value is {} || outputAll))
                    result.Add(key, value);

            return result;
        }

        private void SetValueFromSettings(object value, PropertyInfo prop)
        {
            if(ConvertValueFromSettings(value, prop.Name) is { } notNullValue)
                prop.SetValue(this, notNullValue);
        }

       
        public SharedSettingsContainer Clone()
        {
            var result = new SharedSettingsContainer();
            foreach(var (_, prop) in Properties)
                if(prop.GetValue(this) is { } value)
                    prop.SetValue(result, value);
            return result;
        }

        object ICloneable.Clone() => Clone();

        public static (SharedSettingsContainer Shared, PerCameraSettingsContainer Unique) 
            FindSharedSettings(IImmutableDictionary<string, IAcquisitionSettings> settings)
        {
            if (settings.Count == 0)
                return (new SharedSettingsContainer(), new PerCameraSettingsContainer());
            if (settings.Count == 1)
                return (new SharedSettingsContainer(settings.Values.First()),
                    new PerCameraSettingsContainer());

            var sharedContainer = new SharedSettingsContainer();
            var uniqueVals = new PerCameraSettingsContainer();
            foreach (var (name, (sharedProp, specificProp)) in JoinedProperties)
            {
                var idvVals = settings.ToDictionary(x => x.Key, y => AdjustType(ConvertValueFromSettings(specificProp.GetValue(y.Value), sharedProp.Name), sharedProp.PropertyType));
                switch (idvVals.Values.Distinct().ToList())
                {
                    case { } list when list.Count == 1 && list[0] is null:
                        break;

                    case { } list when list.Count == 1 && list[0] is { } val:
                        //sharedContainer.SetValueFromSettings(val, sharedProp);
                        sharedProp.SetValue(sharedContainer, val);
                        break;

                    case { }:
                        // BUG : Value needs to be converted
                        uniqueVals[name] = idvVals;
                        break;
                }

            }

            return (sharedContainer, uniqueVals);
        }

        private static object? AdjustType(object? value, Type propType)
        {
            if (value is null)
                return null;
            if (value.GetType() == propType)
                return value;

            // This case is for structs/nullables
            if (!propType.IsClass && propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                var innerType = propType.GetGenericArguments()[0];
                if (value.GetType() == innerType)
                    return Activator.CreateInstance(propType, value);
            }

            throw new InvalidOperationException();
        }

        private static object? ConvertValueFromSettings(object? value, string propName)
        {
            return value switch
            {
                ITuple tuple =>
                // Only tuples may have different representation
                propName switch
                {
                    nameof(VSSpeed) when tuple[1] is float speed => speed,
                    nameof(ADConverter) when tuple[0] is int index => index,
                    nameof(HSSpeed) when tuple[1] is float speed => speed,
                    nameof(PreAmpGain) when tuple[1] is string gain => gain,
                    nameof(OutputAmplifier) when tuple[0] is OutputAmplification amplif => amplif,
                    nameof(AccumulateCycle) when tuple[0] is int frames && tuple[1] is float time => (Frames: frames,
                        Time: time),
                    nameof(KineticCycle) when tuple[0] is int frames && tuple[1] is float time => (Frames: frames,
                        Time: time),
                    _ => null
                },
                { } item => item,
                null => null
            };
        }
    }
}
