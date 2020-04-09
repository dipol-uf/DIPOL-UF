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
using Serializers;

namespace DIPOL_UF.Jobs
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal class SharedSettingsContainer
    {
        private static readonly Dictionary<string, PropertyInfo> Properties = typeof(SharedSettingsContainer)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(x => new {Property = x, Order = x.GetCustomAttribute<SerializationOrderAttribute>()})
            .Where(x => x.Order is {})
            .OrderBy(x => x.Order.Index)
            .ToDictionary(x => x.Property.Name, x => x.Property);
        private static readonly Dictionary<string, PropertyInfo> SBProperties = typeof(SettingsBase)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(x => new { Property = x, Order = x.GetCustomAttribute<SerializationOrderAttribute>() })
            .Where(x => x.Order is { })
            .OrderBy(x => x.Order.Index)
            .ToDictionary(x => x.Property.Name, x => x.Property);

        [SerializationOrder(1)]
        public int? VSSpeed { get; set; }

        [SerializationOrder(5)]
        public int? HSSpeed { get; set; }

        [SerializationOrder(3)]
        public int? ADConverter { get; set; }


        [SerializationOrder(2)]
        public VSAmplitude? VSAmplitude { get; set; }

        [SerializationOrder(4)]
        public OutputAmplification? OutputAmplifier { get; set; }

        [SerializationOrder(6)]
        public int? PreAmpGain { get; set; }

        [SerializationOrder(7)]
        public AcquisitionMode? AcquisitionMode { get; set; }

        [SerializationOrder(8)]
        public ReadMode? ReadoutMode { get; set; }

        [SerializationOrder(9)]
        public TriggerMode? TriggerMode { get; set; }

        [SerializationOrder(0)]
        public float? ExposureTime { get; set; }

        [SerializationOrder(11)]
        public Rectangle? ImageArea { get; set; }

        [SerializationOrder(12, true)]
        public (int Frames, float Time)? AccumulateCycle { get; set; }

        [SerializationOrder(13, true)]
        public (int Frames, float Time)? KineticCycle { get; set; }

        [SerializationOrder(10)]
        public int? EMCCDGain { get; set; }

        public SharedSettingsContainer(SettingsBase? sourceSettings)
        {
            if (sourceSettings is null)
                return;

            foreach (var (name, prop) in Properties)
            {
                // Throws is property does not exist; Type mismatch
                var sbProp = SBProperties[name];
                if (sbProp.GetValue(sourceSettings) is { } value)
                {
                    prop.SetValue(this, value switch
                    {
                        ITuple { } tuple when prop.GetCustomAttribute<SerializationOrderAttribute>()?.All == true =>
                        tuple,
                        ITuple { } tuple => tuple[0],
                        { } val => val,
                        // ReSharper disable once HeuristicUnreachableCode
                        _ => null
                    });

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
            setts.Load(settsCollection);

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
