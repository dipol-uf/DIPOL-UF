#if DEBUG
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace DIPOL_UF
{
    static class DebugTracer
    {
        
        public static void AddTarget(INotifyPropertyChanged target, string id)
        {
            var props = target.GetType()
                              .GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
                              .Where(pi => pi.CanRead)
                              .ToList();
            
            var info = new Tuple<string, List<PropertyInfo>>(id, props);
            target.PropertyChanged += (sender, e) => Target_PropertyChanged(info, sender, e);
        }

        private static void Target_PropertyChanged(Tuple<string, List<PropertyInfo>> info, object sender, PropertyChangedEventArgs e)
        {
            var prop = info?.Item2?.FirstOrDefault(pi => pi.Name == e.PropertyName);

            var timeStamp = $"[{DateTime.Now,-13:HH:mm:ss.fff}]";

            var val = "Unavailable";

            if (prop != null && sender != null)
                val = prop.GetValue(sender)?.ToString();

            var notification = $"{timeStamp} -> {info?.Item1} raised {e.PropertyName}\r\n\tValue: {val}";

            Console.WriteLine(notification);
        }
    }
}
#endif
