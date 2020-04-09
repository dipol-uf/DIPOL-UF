using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using ANDOR_CS.Classes;
using DIPOL_UF.Enums;
using Newtonsoft.Json;
using Serializers;

#nullable enable
// TODO : Remove in prod
[assembly:InternalsVisibleTo("Sandbox")]

namespace DIPOL_UF.Jobs
{
    [JsonConverter(typeof(Target1JSonConverter))]
    internal class Target1
    {
        public string? StarName { get; set; }
        public CycleType CycleType { get; set; }

        public Dictionary<string, Dictionary<string, object?>?>? PerCameraParameters { get; set; }

        public SettingsBase? SharedParameters { get; set; }
        
    }

    internal class Target1JSonConverter : JsonConverter<Target1>
    {
        public override void WriteJson(JsonWriter writer, Target1 value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            
            writer.WritePropertyName(nameof(value.StarName));
            writer.WriteValue(value.StarName);

            writer.WritePropertyName(nameof(value.CycleType));
            writer.WriteValue(Enum.GetName(typeof(CycleType), value.CycleType));

            if (value.PerCameraParameters?.Any() == true)
            {
                writer.WritePropertyName(nameof(value.PerCameraParameters));
                serializer.Serialize(writer, value.PerCameraParameters);
            }

            if (value.SharedParameters is {})
            {
                var sharedParams = JsonParser.GenerateJson(value.SharedParameters);
                if (value.PerCameraParameters is { })
                {
                    var excludedKeys = value.PerCameraParameters.Keys;

                    sharedParams = sharedParams.Where(x => !excludedKeys.Contains(x.Key))
                        .ToDictionary(x => x.Key, x => x.Value);
                }

                writer.WritePropertyName(nameof(value.SharedParameters));
                serializer.Serialize(writer, sharedParams);
            }

            writer.WriteEndObject();
        }

        public override Target1 ReadJson(JsonReader reader, Type objectType, Target1 existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
    
}
