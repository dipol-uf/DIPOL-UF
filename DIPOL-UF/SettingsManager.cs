using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

namespace DIPOL_UF
{
    static class SettingsManager
    {
        public static readonly Type[] AllowedTypes = { typeof(int), typeof(double), typeof(bool), typeof(string) };

        public static Dictionary<string, object> Read(StreamReader str)
        {
            Dictionary<string, object> pars = new Dictionary<string, object>();

            while (!str.EndOfStream)
            {
                var line = str.ReadLine();
                int ind = line.IndexOf('=');
                if (ind > 0)
                {
                    var key = line.Substring(0, ind).Trim();
                    if (!pars.ContainsKey(key))
                    {
                        string val = line.Substring(ind + 1).Trim();

                        if (val.StartsWith("{"))
                        {
                            val = val.Trim('{', '}').Trim();

                            pars.Add(key, val.Split(new[] { ",\t" }, StringSplitOptions.RemoveEmptyEntries).Select(StrToObject).ToArray());

                        }
                        else
                            pars.Add(key, StrToObject(val));
                    }
                }
            }

            return pars;
        }

        public static void Write(StreamWriter str, Dictionary<string, object> pars)
        {
            if (!pars.Values.Select(item => item.GetType()).All(item => AllowedTypes.Contains(item) || item.BaseType == typeof(Array)))
                throw new ArgumentException("One or more provided parameter values are of illegal type.");

            foreach (var item in pars)
            {
                string valStr = "";
                if (item.Value is Array arr)
                {
                    var strBuilder = new StringBuilder("{ ");
                    var enumer = arr.GetEnumerator();

                    if(enumer.MoveNext())
                        strBuilder.Append(ObjToString(enumer.Current));

                    while (enumer.MoveNext())
                        strBuilder.Append(",\t" + ObjToString(enumer.Current));


                    valStr = strBuilder.Append(" }").ToString();

                }
                else
                    valStr = ObjToString(item.Value);

                str.WriteLine($"{item.Key} = {valStr}");
            }
        }

        private static string ObjToString(object o)
        {
            string result = String.Empty;
            if (o.GetType() == AllowedTypes[0])
                result = o.ToString();
            else if (o.GetType() == AllowedTypes[1])
                result = ((double)o).ToString("e");
            else if (o.GetType() == AllowedTypes[2])
                result = (bool)o ? bool.TrueString : bool.FalseString;
            else
                result = "\"" + ((o as string) ?? "") + "\"";
            return result.Replace("\t", @"\\t").Replace("\n", @"\\n");
        }

        private static object StrToObject(string s)
        {
            if (s == bool.TrueString || s == bool.FalseString)
                return s == bool.TrueString;
            else if (s.Contains("\""))
                return s.Trim('"').Replace(@"\\t", "\t").Replace(@"\\n", "\n");
            else if (int.TryParse(s, out int intVal))
                return intVal;
            else if (double.TryParse(s, out double doubleVal))
                return doubleVal;
            else
                return null;
        }
    }
}
