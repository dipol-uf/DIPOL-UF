using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ANDOR_CS.Classes;
using ANDOR_CS.Enums;

namespace DIPOL_UF.ViewModels
{
    class CameraPropertiesViewModel : ViewModel<CameraBase>
    {
        private ObservableCollection<Tuple<string, string>> deviceCapabilities = new ObservableCollection<Tuple<string, string>>();
        private ObservableCollection<Tuple<string, string>> deviceProperties = new ObservableCollection<Tuple<string, string>>();
        private ObservableCollection<Tuple<string, string>> allProperties;

        public CameraBase Camera => model;

        public ObservableCollection<Tuple<string, string>> AllProperties => allProperties;


        public CameraPropertiesViewModel(CameraBase model)
            : base(model)
        {
            var type = model.Capabilities.GetType();
            var propertyList = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);

            foreach (var item in propertyList)
                deviceCapabilities.Add(new Tuple<string, string>(
                    item.Name,
                    item.GetValue(model.Capabilities)?.ToString() ?? "Unknown"));

            type = model.Properties.GetType();
            propertyList = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);

            foreach (var item in propertyList)
            {
                var value = item.GetValue(model.Properties);
                if (value is Array a)
                    deviceProperties.Add(new Tuple<string, string>(item.Name, a.ArrayToString()));
                else
                    deviceProperties.Add(new Tuple<string, string>(item.Name, value.ToString()));
            }


            allProperties = new ObservableCollection<Tuple<string, string>>(
                (new[] {
                    new Tuple<string, string>("Alias", new Converters.CameraToStringAliasValueConverter()
                        .Convert(model, typeof(String), null, System.Globalization.CultureInfo.CurrentUICulture).ToString()),
                    new Tuple<string, string>("Camera Model", model.CameraModel) ,
                    new Tuple<string, string>("Serial Number", model.SerialNumber),
                    new Tuple<string, string>("Software Version", model.Software.ToString()),
                    new Tuple<string, string>("Hardware Version", model.Hardware.ToString())})
                .Concat(deviceCapabilities).Concat(deviceProperties));

        }
    }
}
