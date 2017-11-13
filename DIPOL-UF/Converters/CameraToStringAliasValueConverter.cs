using System;
using System.Windows.Data;
using System.Linq;
using System.Globalization;

namespace DIPOL_UF.Converters
{
    [ValueConversion(typeof(ANDOR_CS.Classes.CameraBase), typeof(string))]
    class CameraToStringAliasValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ANDOR_CS.Classes.CameraBase cam)
            {
                var key = $"{cam.CameraModel}_{cam.SerialNumber}";

                var camIndex = DIPOL_UF_App.Settings.GetValueOrNullSafe<object[]>("Cameras");
                var alias = DIPOL_UF_App.Settings.GetValueOrNullSafe<object[]>("CameraAlias");

                string camName = cam.ToString();

                if (camIndex.Length == alias.Length &&
                    camIndex.All(item => item is string) &&
                    alias.All(item => item is string))
                {

                    for (int index = 0; index < camIndex.Length; index++)
                        if ((string)camIndex[index] == key)
                        {
                            camName = (string)alias[index];
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
