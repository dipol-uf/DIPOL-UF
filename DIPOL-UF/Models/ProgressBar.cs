using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIPOL_UF.Models
{
    class ProgressBar
    {
        private int minimum = 0;
        private int maximum = 100;
        private int value = 50;

        public int Minimum
        {
            get => minimum;
            set
            {
                if (value >= maximum)
                    minimum = maximum - 1;

                if (Value < minimum)
                    Value = minimum;
            }
        }

        public int Maximum
        {
            get => maximum;
            set
            {

                if (value <= minimum)
                    maximum = minimum + 1;
                if (Value > maximum)
                    Value = maximum;
            }
        }

        public int Value
        {
            get => value;
            set
            {
                if (value < minimum)
                    this.value = minimum;
                else if (value > maximum)
                    this.value = maximum;
                else
                    this.value = value;
            }
        }

        public bool TryIncrement()
            => ++Value <= Maximum;

        public bool Decrement()
            => --Value >= Minimum;
    }
}
