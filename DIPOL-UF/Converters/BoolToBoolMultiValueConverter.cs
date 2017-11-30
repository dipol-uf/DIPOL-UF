using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Data;
using System.Windows.Media;
using System.Globalization;


namespace DIPOL_UF.Converters
{
    class BoolToBoolMultiValueConverter : IMultiValueConverter
    {
       
        private bool GetBool(object val)
        {
            if (val is bool b)
                return b;
            else
                return false;
        }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {

            if (targetType == typeof(bool))
            {
                if (parameter is string strPar)
                {
                    string key = strPar.Trim().ToLowerInvariant();
                    if (key == "any")
                        return values.Any(GetBool);
                    else if (key == "notall")
                        return !values.All(GetBool);
                    else if (key == "notany")
                        return !values.Any(GetBool);

                }
               

                return values.All(GetBool);

            }
            else return false;

        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
