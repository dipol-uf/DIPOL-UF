using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using ANDOR_CS;
using ANDOR_CS.Classes;
using DIPOL_UF.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

#nullable enable
// TODO : Remove in prod
[assembly:InternalsVisibleTo("Sandbox")]

namespace DIPOL_UF.Jobs
{
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
        
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, Dictionary<string, object?>>? PerCameraParameters { get; set; }

        [JsonRequired]
        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public SharedSettingsContainer SharedParameters { get; set; } = new SharedSettingsContainer();

        public IReadOnlyDictionary<string, IAcquisitionSettings> CreateTemplatesForCameras(
            IReadOnlyDictionary<string, IDevice> cameras)
        {
            _ = cameras ?? throw new ArgumentNullException(nameof(cameras));

            return cameras.ToDictionary(
                x => x.Key,
                x => (SharedParameters ?? new SharedSettingsContainer())
                    .PrepareTemplateForCamera(
                        x.Value,
                        GatherPerCameraParameters(x.Key)));
        }

        private IReadOnlyDictionary<string, object?> GatherPerCameraParameters(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException(nameof(key));

            var result = PerCameraParameters
                       ?.Where(x => x.Value?.ContainsKey(key) is true)
                       .ToDictionary(x => x.Key, x => x.Value?[key])
                   ?? new Dictionary<string, object?>();

            return result;
        }

        public static Target1 FromSettings(
            IReadOnlyDictionary<string, IAcquisitionSettings> settings,
            string? starName = null,
            string? description = null,
            CycleType cycleType = CycleType.Polarimetric)
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
                PerCameraParameters =
                    PerCameraParameters?.ToDictionary(x => x.Key, y => y.Value.ToDictionary(z => z.Key, z => z.Value))
            };

        object ICloneable.Clone() => Clone();
    }

    
}
