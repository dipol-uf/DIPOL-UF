using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIPOL_UF.Validators
{
    internal static class Validate
    {
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
    }
}
