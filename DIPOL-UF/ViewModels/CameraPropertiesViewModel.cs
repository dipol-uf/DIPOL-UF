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
        public CameraBase Camera => model;

        public ObservableCollection<Tuple<string, string>> AllProperties
        {
            get
            {
                return new ObservableCollection<Tuple<string, string>>( DeviceCapabilities.Concat(DeviceProperties));
            }
        }

        public ObservableCollection<Tuple<string, string>> DeviceCapabilities
        {
            get
            {
                var collection = new ObservableCollection<Tuple<string, string>>();
                var type = model.Capabilities.GetType();
                var propertyList = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);

                foreach (var item in propertyList)
                    collection.Add(new Tuple<string, string>(
                        item.Name,
                        item.GetValue(model.Capabilities)?.ToString() ?? "Unknown"));

                return collection;

            }
        }

        public ObservableCollection<Tuple<string, string>> DeviceProperties
        {
            get
            {
                var collection = new ObservableCollection<Tuple<string, string>>();
                var type = model.Properties.GetType();
                var propertyList = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);

                foreach (var item in propertyList)
                {
                    var value = item.GetValue(model.Properties);
                    if (value is Array a)
                        collection.Add(new Tuple<string, string>(item.Name, a.ArrayToString()));
                    else
                        collection.Add(new Tuple<string, string>(item.Name, value.ToString()));
                }

                    return collection;
            }
        }

        public CameraPropertiesViewModel(CameraBase model) 
            : base(model)
        {
        }
    }
}
