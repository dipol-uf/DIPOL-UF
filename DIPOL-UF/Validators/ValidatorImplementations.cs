using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIPOL_UF.Validators
{
    internal static class Validator
    {
        public static string CannotBeLessThan(int x, int comp) =>
            x >= comp ? null : string.Format(Properties.Localization.Validation_ValueCannotBeLessThan, comp);

        public static string CannotBeGreaterThan(int x, int comp) =>
            x <= comp ? null : string.Format(Properties.Localization.Validation_ValueCannotBeGreaterThan, comp);

        public static string ShouldFallWithinRange(int x, int lower, int upper) =>
            lower <= x && x <= upper
                ? null
                : string.Format(Properties.Localization.Validation_ValueShouldFallWithinRange, lower, upper);
    }
}
