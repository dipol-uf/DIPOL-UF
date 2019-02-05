using System;
using System.Reflection;
using System.Linq;
using ANDOR_CS.Classes;
using ANDOR_CS.DataStructures;
using DIPOL_UF.Converters;
using DynamicData.Binding;
using ReactiveUI.Fody.Helpers;

namespace DIPOL_UF.ViewModels
{
    internal sealed class CameraPropertiesViewModel : ReactiveObjectEx
    {
        private static readonly PropertyInfo[] capabilitiesAccessors;
        private static readonly PropertyInfo[] propertiesAccessors;

        public IObservableCollection<Tuple<string, string>> AllProperties { get; }
        [Reactive]
        public string CameraAlias { get; private set; }

    static CameraPropertiesViewModel()
        {
            capabilitiesAccessors = typeof(DeviceCapabilities).GetProperties(BindingFlags.Instance | BindingFlags.Public);
            propertiesAccessors = typeof(CameraProperties).GetProperties(BindingFlags.Instance | BindingFlags.Public);
        }

        public CameraPropertiesViewModel(CameraBase model)
        {
            var capabilities = capabilitiesAccessors.Select(x => new Tuple<string, string>(
                x.Name,
                x.GetValue(model.Capabilities)?.ToString() ?? "Unknown"));

            var properties = propertiesAccessors.Select(x => new Tuple<string, string>(
                x.Name,
                x.GetValue(model.Properties)?.ToString() ?? "Unknown"));

            var additionalInfo = new[]
            {
                new Tuple<string, string>(
                    Properties.Localization.CameraProperties_Alias, 
                    Converters.ConverterImplementations.CameraToStringAliasConversion(model)),
                new Tuple<string, string>(Properties.Localization.CameraProperties_CamModel, model.CameraModel),
                new Tuple<string, string>(Properties.Localization.CameraProperties_SerialNumber, model.SerialNumber),
                new Tuple<string, string>(Properties.Localization.CameraProperties_SoftwareVers, model.Software.ToString()),
                new Tuple<string, string>(Properties.Localization.CameraProperties_HardwareVers, model.Hardware.ToString())
            };

            AllProperties = new ObservableCollectionExtended<Tuple<string, string>>(additionalInfo.Concat(capabilities).Concat(properties));
            CameraAlias = ConverterImplementations.CameraToStringAliasConversion(model);
        }
    }
}
