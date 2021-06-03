#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using ANDOR_CS;
using DIPOL_UF.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;


namespace DIPOL_UF.Jobs
{
    [JsonObject]
    internal class Target1 : ICloneable
    {
        [JsonRequired]
        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public string? StarName { get; set; }

        [JsonRequired]
        [JsonConverter(typeof(StringEnumConverter))]
        public CycleType CycleType { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? Description { get; set; }
        
        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public PerCameraSettingsContainer? PerCameraParameters { get; set; }

        [JsonRequired]
        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public SharedSettingsContainer? SharedParameters { get; set; } = new();

        public IImmutableDictionary<string, IAcquisitionSettings> CreateTemplatesForCameras(
            IImmutableDictionary<string, IDevice> cameras)
        {
            _ = cameras ?? throw new ArgumentNullException(nameof(cameras));

            return cameras.ToImmutableDictionary(
                x => x.Key,
                x => (SharedParameters ?? new SharedSettingsContainer())
                    .PrepareTemplateForCamera(
                        x.Value,
                        PerCameraParameters?.GatherCameraSettings(x.Key) ?? ImmutableDictionary<string, object?>.Empty));
        }

        public static Target1 FromSettings(
            IImmutableDictionary<string, IAcquisitionSettings> settings,
            string? starName = null,
            string? description = null,
            CycleType cycleType = CycleType.LinearPolarimetry)
        {
            var result = new Target1()
            {
                StarName = starName,
                CycleType = cycleType,
                Description = description
            };


            var (shared, unique) = SharedSettingsContainer.FindSharedSettings(settings);

            result.SharedParameters = shared;
            result.PerCameraParameters = unique!;

            return result;
        }

        public Target1 Clone() =>
            new Target1
            {
                StarName = StarName,
                Description = Description,
                CycleType = CycleType,
                SharedParameters = SharedParameters?.Clone() ?? new SharedSettingsContainer(),
                PerCameraParameters = PerCameraParameters?.Clone()
            };

        object ICloneable.Clone() => Clone();
       
    }

    
}
