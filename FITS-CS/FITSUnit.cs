using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FITS_CS
{
    public class FITSUnit
    {
        public static readonly int UnitSizeInBytes = 2880;

        private byte[] array = new byte[FITSUnit.UnitSizeInBytes];

        public byte[] Data => array;

        public FITSUnit(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException($"{nameof(data)} is null");
            if (data.Length != UnitSizeInBytes)
                throw new ArgumentException($"{nameof(data)} has wrong length");

            Array.Copy(data, array, data.Length);
        }

        public char[][] IsKeywords
            => Enumerable.Range(0, UnitSizeInBytes / FITSKey.KeySize)
            .Select(i => FITSKey.IsFITSKey(Data, i * FITSKey.KeySize)).ToArray();
    }
}
