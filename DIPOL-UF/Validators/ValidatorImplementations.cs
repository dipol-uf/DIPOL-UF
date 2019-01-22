using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIPOL_UF.Validators
{
    internal static class Validator
    {
        public static IEnumerable CannotBeLessThan(int x, int comp)
        {
            return x >= comp 
                ? Enumerable.Empty<string>()
                : new[] {string.Format(Properties.Localization.Validation_ValueCannotBeLessThan, comp)};
        }

        public static IEnumerable CannotBeGreaterThan(int x, int comp)
        {
            return x <= comp
                ? Enumerable.Empty<string>() 
                : new [] {string.Format(Properties.Localization.Validation_ValueCannotBeGreaterThan, comp)};
        }

        public static IEnumerable ShouldFallWithinRange(int x, int lower, int upper)
        {
            return lower <= x && x <= upper
                ? Enumerable.Empty<string>()
                : new [] {string.Format(Properties.Localization.Validation_ValueShouldFallWithinRange, lower, upper)};
        }
    }
}
