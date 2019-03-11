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
using System.Collections;
using System.Collections.Generic;
using System.Resources;

namespace DIPOL_UF
{
    internal class ResourceMangerEx : IReadOnlyDictionary<string, string>
    {
        public ResourceManager Manger { get; }

        public ResourceMangerEx(ResourceManager manger)
        {
            Manger = manger ?? throw new ArgumentNullException(nameof(manger));
        }


        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            //TODO: Fix exception message
            throw new NotSupportedException(@"Enumeration not supported");
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count => throw new NotSupportedException(@"Enumeration not supported");

        public bool ContainsKey(string key)
        {
            return !(Manger.GetString(key) is null);
        }

        public bool TryGetValue(string key, out string value)
        {
            return !((value = Manger.GetString(key)) is null);
        }

        public string this[string key] => Manger.GetString(key);

        public IEnumerable<string> Keys => throw new NotSupportedException(@"Enumeration not supported");
        public IEnumerable<string> Values => throw new NotSupportedException(@"Enumeration not supported");
    }
}
