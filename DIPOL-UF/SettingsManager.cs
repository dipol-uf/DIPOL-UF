using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Script.Serialization;
using System.IO;

namespace DIPOL_UF
{
    static class SettingsManager
    {
        public static Dictionary<string, object> Read(StreamReader str)
            => new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(str.ReadToEnd());

        public static void Write(Dictionary<string, object> data, StreamWriter str)
        {
            var ser = new JavaScriptSerializer();

            var s = 
                "{\n\t" 
                + data.Select(
                    (x) =>
                        (ser.Serialize(x.Key) + " : " + ser.Serialize(x.Value)).Replace("{", "").Replace("}", ""))
                    .Aggregate((x, old) => old + ",\n\t" + x) 
                + "\n}";

            str.WriteLine(s);

        }
        
    }
}
