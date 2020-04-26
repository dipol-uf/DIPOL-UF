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
using System.Runtime.CompilerServices;

namespace DIPOL_UF
{
    internal class CustomComparer<T> : IEqualityComparer<T>, IComparer<T>
    {
        public static IEqualityComparer<T> Default { get; }
            = EqualityComparer<T>.Default;

        public Func<T, T, bool> EqualityComparer { get; }
        public Func<T, T, int> Comparer { get; }

        private CustomComparer(Func<T, T, bool> method, Func<T, T, int> orderMethod)
        {
            EqualityComparer = method ?? throw new ArgumentNullException(nameof(method));
            Comparer = orderMethod;
        }

        public bool Equals(T x, T y)
            => EqualityComparer.Invoke(x, y);

        public int Compare(T x, T y)
            => Comparer?.Invoke(x, y) ?? throw new NotSupportedException("Ordering comparisons are not supported.");

        public int GetHashCode(T obj)
            => Comparer?.GetHashCode() ?? 0;


        public static IEqualityComparer<T> FromMethod(
            Func<T, T, bool> comparer,
            Func<T, T, int> orderComparer)
            => new CustomComparer<T>(comparer, orderComparer);
    }

    internal abstract class SimpleEqualityComparer
    {
        public static IEqualityComparer<TSource> FromMethod<TSource, TTarget>(Func<TSource, TTarget> selector) => new SimpleEqualityComparer<TSource,TTarget>(selector);
    }

    internal sealed class SimpleEqualityComparer<TSource, TTarget> : SimpleEqualityComparer, IEqualityComparer<TSource>
    {
        private readonly Func<TSource, TTarget> _selector;
        private readonly IEqualityComparer<TTarget> _comparer;
        public SimpleEqualityComparer(Func<TSource, TTarget> selector)
        {
            _selector = selector ?? throw new ArgumentNullException(nameof(selector));
            _comparer = EqualityComparer<TTarget>.Default;
        }

        public bool Equals(TSource x, TSource y) => _comparer.Equals(_selector(x), _selector(y));

        public int GetHashCode(TSource obj) => _comparer.GetHashCode(_selector(obj));
    }
}
