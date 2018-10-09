using System;
using System.Windows.Data;
using System.Linq;
using System.Globalization;
using System.Collections;
using ANDOR_CS.Classes;

namespace DIPOL_UF.Converters
{
    [ValueConversion(typeof(CameraBase), typeof(string))]
    class CameraToStringAliasValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ANDOR_CS.Classes.CameraBase cam)
            {
                var key = $"{cam.CameraModel}_{cam.SerialNumber}";

                var camIndex = SettingsProvider.Settings.GetArray<string>("Cameras") ?? new string [0];
                var alias = SettingsProvider.Settings.GetArray<string>("CameraAlias") ?? new string[0];

                var camName = cam.ToString();

                if (camIndex.Length == alias.Length)
                {

                    for (int index = 0; index < camIndex.Length; index++)
                        if (camIndex[index] == key)
                        {
                            camName = alias[index];
                            break;
                        }
                }


                return camName;
            }
            else return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
