﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DIPOL_UF.Validators
{
    internal static class Validate
    {
        private static readonly Dictionary<string, Regex> _regexCache =
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
            if (!_regexCache.ContainsKey(pattern))
                _regexCache.Add(pattern, new Regex(pattern));

            return !_regexCache[pattern].IsMatch(input)
                ? string.Format(Properties.Localization.Validation_ValueMatchesRegex,
                    string.IsNullOrWhiteSpace(disallowed) ? "" : $" [{disallowed}]")
                : null;
        }

        public static string DoesNotThrow(Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                return e.Message;
            }

            return null;
        }

    }
}
