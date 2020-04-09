using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ANDOR_CS.Classes;
using DIPOL_UF.Enums;

#nullable enable
// TODO : Remove in prod
[assembly:InternalsVisibleTo("")]

namespace DIPOL_UF.Jobs
{
    internal class Target1
    {
        public string? StarName { get; set; }
        public CycleType CycleType { get; set; }

        public Dictionary<string, Dictionary<string, object?>?>? UniqueParameters { get; set; }

        public Dictionary<string, SettingsBase>? AcquisitionParameters { get; set; }
        
    }
}
