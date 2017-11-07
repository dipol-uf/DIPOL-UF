using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                    var key = line.Substring(0, ind);
                    if (!pars.ContainsKey(key))
                    {
                        string val = line.Substring(ind + 1).Trim();
                        if (val == bool.TrueString || val == bool.FalseString)
                            pars.Add(key, val == bool.TrueString);
                        else if (val.Contains("\""))
                            pars.Add(key, val.Trim('"'));
                        else if (int.TryParse(val, out int intVal))
                            pars.Add(key, intVal);
                        else if (double.TryParse(val, out double doubleVal))
                            pars.Add(key, doubleVal);
                    }
                }
            }

            return pars;
        }

        public static void Write(StreamWriter str, Dictionary<string, object> pars)
        {
            if (!pars.Values.Select(item => item.GetType()).All(item => AllowedTypes.Contains(item)))
                throw new ArgumentException("One or more provided parameter values are of illegal type.");

            foreach (var item in pars)
            {
                string valStr = "";
                if (item.Value.GetType() == AllowedTypes[0])
                    valStr = item.Value.ToString();
                else if(item.Value.GetType() == AllowedTypes[1])
                    valStr = ((double)item.Value).ToString("e");
                else if (item.Value.GetType() == AllowedTypes[2])
                    valStr = (bool)item.Value ? bool.TrueString : bool.FalseString;
                else
                    valStr = "\"" + ((item.Value as string) ?? "") + "\"";

                str.WriteLine($"{item.Key} = {valStr}");
            }
        }
    }
}
