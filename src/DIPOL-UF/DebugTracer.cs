//    This file is part of Dipol-3 Camera Manager.

//     MIT License
//     
//     Copyright(c) 2018-2019 Ilia Kosenkov
//     
//     Permission is hereby granted, free of charge, to any person obtaining a copy
//     of this software and associated documentation files (the "Software"), to deal
//     in the Software without restriction, including without limitation the rights
//     to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//     copies of the Software, and to permit persons to whom the Software is
//     furnished to do so, subject to the following conditions:
//     
//     The above copyright notice and this permission notice shall be included in all
//     copies or substantial portions of the Software.
//     
//     THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//     IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//     FITNESS FOR A PARTICULAR PURPOSE AND NONINFINGEMENT. IN NO EVENT SHALL THE
//     AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//     LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//     OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//     SOFTWARE.

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
