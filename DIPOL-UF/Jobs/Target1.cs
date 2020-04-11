using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using ANDOR_CS.Classes;
using DIPOL_UF.Enums;
using Newtonsoft.Json;

#nullable enable
// TODO : Remove in prod
[assembly:InternalsVisibleTo("Sandbox")]

namespace DIPOL_UF.Jobs
{
    internal class Target1
    {
        [JsonRequired]
        public string? StarName { get; set; }
        [JsonRequired]
        public CycleType CycleType { get; set; }
        public Dictionary<string, Dictionary<string, object?>?>? PerCameraParameters { get; set; }
        [JsonRequired]
        public SharedSettingsContainer? SharedParameters { get; set; }

        public IReadOnlyDictionary<string, SettingsBase> CreateTemplatesForCameras(
            IReadOnlyDictionary<string, CameraBase> cameras)
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

            return PerCameraParameters
                       ?.Where(x => x.Value?.ContainsKey(key) is true)
                       .ToDictionary(x => x.Key, x => x.Value?[key])
                   ?? new Dictionary<string, object?>();
        }

        public static void FromSettings(
            IReadOnlyDictionary<string, SettingsBase> settings,
            string? starName = null,
            CycleType cycleType = CycleType.Photometric)
        {
            var result = new Target1()
            {
                StarName = starName,
                CycleType = cycleType
            };


        }

    }

    
}
