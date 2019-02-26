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
using System.Text.RegularExpressions;

namespace DIPOL_UF.Validators
{
    internal static class Validate
    {
        private static readonly Dictionary<string, Regex> RegexCache =
            new Dictionary<string, Regex>();

        public static string CannotBeLessThan(int x, int comp)
        {
            return x < comp
                ? string.Format(Properties.Localization.Validation_ValueCannotBeLessThan, comp)
                : null;
        }

        public static string CannotBeGreaterThan(int x, int comp)
        {
            return x > comp
                ? string.Format(Properties.Localization.Validation_ValueCannotBeGreaterThan, comp)
                : null;
        }

        public static string ShouldFallWithinRange(int x, int lower, int upper)
        {
            return x < lower || x > upper
                ? string.Format(Properties.Localization.Validation_ValueShouldFallWithinRange, lower, upper)
                : null;
        }

        public static string ShouldFallWithinRange(float x, float lower, float upper)
        {
            return x < lower || x > upper
                ? string.Format(Properties.Localization.Validation_ValueShouldFallWithinRange, lower, upper)
                : null;
        }

        public static string MatchesRegex(string input, string pattern, string disallowed = null)
        {
            if (!RegexCache.ContainsKey(pattern))
                RegexCache.Add(pattern, new Regex(pattern));

            return !RegexCache[pattern].IsMatch(input)
                ? string.Format(Properties.Localization.Validation_ValueMatchesRegex,
                    string.IsNullOrWhiteSpace(disallowed) ? "" : $" [{disallowed}]")
                : null;
        }

        public static string DoesNotThrow<T>(Action<T> action, T param)
        {
            try
            {
                action(param);
            }
            catch (Exception e)
            {
                return e.Message;
            }

            return null;
        }

    }
}
